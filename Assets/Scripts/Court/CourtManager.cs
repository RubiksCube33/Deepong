using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class CourtManager : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Court_Testing 씬 설정")]
    [SerializeField] private Transform player1Position; // 코트 왼쪽 위치
    [SerializeField] private Transform player2Position; // 코트 오른쪽 위치
    
    [Header("자동 찾기")]
    [SerializeField] private bool autoFindPlayers = true;
    
    // 직접 할당용 변수
    [SerializeField] private GameObject player1;
    [SerializeField] private GameObject player2;
    
    [Header("XR Origin 네트워크 동기화")]
    [SerializeField] private Transform myXROrigin; // 내 XR Origin
    
    // 네트워크 수신 데이터 (다른 곳에서 사용할 수 있도록)
    private Vector3 enemyXROriginPosition;
    private Quaternion enemyXROriginRotation;
    private bool hasReceivedEnemyData = false;
    
    // 다른 스크립트에서 접근할 수 있는 프로퍼티
    public Vector3 EnemyXROriginPosition => enemyXROriginPosition;
    public Quaternion EnemyXROriginRotation => enemyXROriginRotation;
    public bool HasReceivedEnemyData => hasReceivedEnemyData;
    
    void Start()
    {
        // 포지션이 지정되지 않은 경우 기본 위치 생성
        if (player1Position == null || player2Position == null)
        {
            CreatePositionMarkers();
        }
        
        // 플레이어 오브젝트 찾기
        if (autoFindPlayers)
        {
            FindPlayerObjects();
        }
        
        // 내 XR Origin 찾기
        FindMyXROrigin();
        
        // 플레이어 초기 위치 설정
        PositionPlayers();
    }
    
    void FindMyXROrigin()
    {
        if (myXROrigin != null) return; // 이미 할당된 경우
        
        // PhotonView.IsMine인 플레이어 오브젝트 찾기
        PhotonView[] allPhotonViews = FindObjectsOfType<PhotonView>();
        foreach (PhotonView pv in allPhotonViews)
        {
            if (pv.IsMine && (pv.gameObject.name.Contains("Player") || pv.gameObject.name.Contains("Origin")))
            {
                myXROrigin = pv.transform;
                Debug.Log($"[CourtManager] 내 XR Origin 찾음: {myXROrigin.name}");
                break;
            }
        }
        
        if (myXROrigin == null)
        {
            Debug.LogWarning("[CourtManager] 내 XR Origin을 찾을 수 없습니다!");
        }
    }
    
    void CreatePositionMarkers()
    {
        // 코트 위치 마커 생성
        GameObject markersHolder = new GameObject("PositionMarkers");
        
        // 플레이어 1 포지션 마커 (코트 왼쪽)
        GameObject p1Marker = new GameObject("Player1Position");
        p1Marker.transform.parent = markersHolder.transform;
        p1Marker.transform.position = new Vector3(-9f, 1f, 0f); // 코트 왼쪽 끝
        player1Position = p1Marker.transform;
        
        // 플레이어 2 포지션 마커 (코트 오른쪽)
        GameObject p2Marker = new GameObject("Player2Position");
        p2Marker.transform.parent = markersHolder.transform;
        p2Marker.transform.position = new Vector3(9f, 1f, 0f); // 코트 오른쪽 끝
        player2Position = p2Marker.transform;
        
        Debug.Log("기본 위치 마커가 생성되었습니다.");
    }
    
    void FindPlayerObjects()
    {
        List<GameObject> foundPlayers = new List<GameObject>();
        
        // 1. Player 태그로 찾기
        GameObject[] taggedPlayers = GameObject.FindGameObjectsWithTag("Player");
        if (taggedPlayers.Length >= 2)
        {
            foundPlayers.AddRange(taggedPlayers);
            Debug.Log("Player 태그가 있는 오브젝트를 찾았습니다.");
        }
        
        // 2. player1, player2 태그로 찾기
        if (foundPlayers.Count < 2)
        {
            GameObject p1 = GameObject.FindGameObjectWithTag("player1");
            GameObject p2 = GameObject.FindGameObjectWithTag("player2");
            if (p1 != null && p2 != null)
            {
                foundPlayers.Clear();
                foundPlayers.Add(p1);
                foundPlayers.Add(p2);
                Debug.Log("player1, player2 태그 오브젝트를 찾았습니다.");
            }
        }
        
        // 3. Cylinder 형태로 찾기
        if (foundPlayers.Count < 2)
        {
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            List<GameObject> cylinders = new List<GameObject>();
            
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("Cylinder") || obj.name.Contains("Player") ||
                    (obj.GetComponent<MeshFilter>() != null && 
                     obj.GetComponent<MeshFilter>().sharedMesh != null && 
                     obj.GetComponent<MeshFilter>().sharedMesh.name.Contains("Cylinder")))
                {
                    cylinders.Add(obj);
                }
            }
            
            if (cylinders.Count >= 2)
            {
                foundPlayers.Clear();
                foundPlayers.Add(cylinders[0]);
                foundPlayers.Add(cylinders[1]);
                Debug.Log($"실린더 형태 오브젝트를 플레이어로 찾았습니다: {cylinders[0].name}, {cylinders[1].name}");
            }
        }
        
        // 4. 활성화된 게임 오브젝트 중에서 찾기 (마지막 수단)
        if (foundPlayers.Count < 2)
        {
            GameObject[] allActive = FindObjectsOfType<GameObject>();
            List<GameObject> candidates = new List<GameObject>();
            
            foreach (GameObject obj in allActive)
            {
                // 메시 렌더러가 있고 활성화된 오브젝트
                if (obj.activeInHierarchy && obj.GetComponent<MeshRenderer>() != null)
                {
                    candidates.Add(obj);
                }
            }
            
            if (candidates.Count >= 2)
            {
                foundPlayers.Clear();
                foundPlayers.Add(candidates[0]);
                foundPlayers.Add(candidates[1]);
                Debug.Log($"활성화된 오브젝트를 플레이어로 사용합니다: {candidates[0].name}, {candidates[1].name}");
            }
        }
        
        // 결과 할당
        if (foundPlayers.Count >= 2)
        {
            player1 = foundPlayers[0];
            player2 = foundPlayers[1];
            Debug.Log($"플레이어 할당 완료: Player1={player1.name}, Player2={player2.name}");
        }
        else
        {
            Debug.LogWarning($"충분한 플레이어 오브젝트를 찾을 수 없습니다! 찾은 개수: {foundPlayers.Count}");
        }
    }
    
    void PositionPlayers()
    {
        // 플레이어 오브젝트가 지정되어 있는지 확인
        if (player1 == null || player2 == null)
        {
            Debug.LogWarning("플레이어 오브젝트가 없습니다!");
            return;
        }
        
        // 네트워크 환경인 경우 마스터 클라이언트만 설정할 수 있도록 함
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
        {
            return;
        }
        
        // 플레이어 1을 왼쪽 위치로 이동
        player1.transform.position = player1Position.position;
        
        // 플레이어 2를 오른쪽 위치로 이동
        player2.transform.position = player2Position.position;
        
        Debug.Log("플레이어 위치가 코트 양 끝으로 설정되었습니다.");
        
        // 네트워크 동기화
        if (PhotonNetwork.IsConnected && photonView != null)
        {
            photonView.RPC("SyncPlayerPositions", RpcTarget.Others);
        }
    }
    
    [PunRPC]
    void SyncPlayerPositions()
    {
        // 다른 클라이언트에서 플레이어 위치 동기화
        if (player1 != null && player2 != null && player1Position != null && player2Position != null)
        {
            player1.transform.position = player1Position.position;
            player2.transform.position = player2Position.position;
        }
    }
    
    #region XR Origin 네트워크 동기화 (송수신만, Transform 조작 안함)
    
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 내 XR Origin 데이터 송신
            if (myXROrigin != null)
            {
                stream.SendNext(myXROrigin.position);
                stream.SendNext(myXROrigin.rotation);
            }
            else
            {
                stream.SendNext(Vector3.zero);
                stream.SendNext(Quaternion.identity);
            }
        }
        else
        {
            // 상대 XR Origin 데이터 수신 (변수에만 저장, Transform 조작 안함)
            enemyXROriginPosition = (Vector3)stream.ReceiveNext();
            enemyXROriginRotation = (Quaternion)stream.ReceiveNext();
            hasReceivedEnemyData = true;
            
            // 디버그 로그 (5초마다)
            if (Time.time % 5f < 0.1f)
            {
                Debug.Log($"[CourtManager] 상대 XR Origin 데이터 수신: Position={enemyXROriginPosition}, Rotation={enemyXROriginRotation.eulerAngles}");
            }
        }
    }
    
    #endregion
}