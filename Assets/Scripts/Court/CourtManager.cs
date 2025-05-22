using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class CourtManager : MonoBehaviourPunCallbacks
{
    [Header("Court_Testing 씬 설정")]
    [SerializeField] private Transform player1Position; // 코트 왼쪽 위치
    [SerializeField] private Transform player2Position; // 코트 오른쪽 위치
    
    [Header("자동 찾기")]
    [SerializeField] private bool autoFindPlayers = true;
    
    // 직접 할당용 변수
    [SerializeField] private GameObject player1;
    [SerializeField] private GameObject player2;
    
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
        
        // 플레이어 초기 위치 설정
        PositionPlayers();
    }
    
    void CreatePositionMarkers()
    {
        // 코트 위치 마커 생성
        GameObject markersHolder = new GameObject("PositionMarkers");
        
        // 플레이어 1 포지션 마커 (코트 왼쪽)
        GameObject p1Marker = new GameObject("Player1Position");
        p1Marker.transform.parent = markersHolder.transform;
        p1Marker.transform.position = new Vector3(-9f, 1f, 0f); // 코트 왼쪽 끝
        p1Marker.transform.rotation = Quaternion.Euler(0, 90, 0); // 오른쪽을 바라보도록 설정
        player1Position = p1Marker.transform;
        
        // 플레이어 2 포지션 마커 (코트 오른쪽)
        GameObject p2Marker = new GameObject("Player2Position");
        p2Marker.transform.parent = markersHolder.transform;
        p2Marker.transform.position = new Vector3(9f, 1f, 0f); // 코트 오른쪽 끝
        p2Marker.transform.rotation = Quaternion.Euler(0, -90, 0); // 왼쪽을 바라보도록 설정
        player2Position = p2Marker.transform;
        
        Debug.Log("기본 위치 마커가 생성되었습니다.");
    }
    
    void FindPlayerObjects()
    {
        // 씬에서 실린더 오브젝트 찾기
        GameObject[] cylinders = GameObject.FindGameObjectsWithTag("Player");
        
        // Player 태그가 없는 경우 Cylinder 형태로 찾기
        if (cylinders.Length < 2)
        {
            cylinders = GameObject.FindObjectsOfType<GameObject>();
            List<GameObject> foundCylinders = new List<GameObject>();
            
            foreach (GameObject obj in cylinders)
            {
                if (obj.name.Contains("Cylinder") || 
                    (obj.GetComponent<MeshFilter>() != null && 
                     obj.GetComponent<MeshFilter>().sharedMesh != null && 
                     obj.GetComponent<MeshFilter>().sharedMesh.name.Contains("Cylinder")))
                {
                    foundCylinders.Add(obj);
                }
            }
            
            if (foundCylinders.Count >= 2)
            {
                player1 = foundCylinders[0];
                player2 = foundCylinders[1];
                Debug.Log("실린더 오브젝트를 플레이어로 찾았습니다.");
            }
            else
            {
                Debug.LogWarning("씬에서 충분한 실린더 오브젝트를 찾을 수 없습니다!");
            }
        }
        else
        {
            player1 = cylinders[0];
            player2 = cylinders[1];
            Debug.Log("Player 태그가 있는 오브젝트를 찾았습니다.");
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
        
        // 플레이어 1을 왼쪽 위치로 이동 및 회전
        player1.transform.position = player1Position.position;
        // 마커의 회전값 사용
        player1.transform.rotation = player1Position.rotation;
        Debug.Log($"CourtManager: 플레이어 1 회전값 = {player1Position.rotation.eulerAngles}");
        
        // 플레이어 2를 오른쪽 위치로 이동 및 회전
        player2.transform.position = player2Position.position;
        // 마커의 회전값 사용
        player2.transform.rotation = player2Position.rotation;
        Debug.Log($"CourtManager: 플레이어 2 회전값 = {player2Position.rotation.eulerAngles}");
        
        Debug.Log("플레이어 위치와 방향이 코트 양 끝으로 설정되었습니다. (CourtManager)");
        
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
            player1.transform.rotation = player1Position.rotation;
            
            player2.transform.position = player2Position.position;
            player2.transform.rotation = player2Position.rotation;
        }
    }
}