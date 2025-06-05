using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("플레이어 설정")]
    public Transform player1SpawnPoint; // 플레이어 1 시작 위치
    public Transform player2SpawnPoint; // 플레이어 2 시작 위치
    public GameObject playerPrefab; // 플레이어 프리팹 (실린더)

    [Header("현재 씬의 플레이어 오브젝트")]
    public GameObject player1Object; // 플레이어 1 오브젝트 (실린더)
    public GameObject player2Object; // 플레이어 2 오브젝트 (실린더)

    void Start()
    {
        Debug.Log("GameManager 시작됨");
        
        // 스폰 포인트가 지정되지 않았을 경우 기본값 생성
        if (player1SpawnPoint == null || player2SpawnPoint == null)
        {
            Debug.Log("스폰 포인트가 설정되지 않아 기본값을 생성합니다.");
            CreateDefaultSpawnPoints();
        }

        // Inspector에서 플레이어 오브젝트가 제대로 할당되었는지 먼저 확인
        Debug.Log($"Inspector 할당 상태 - Player1: {(player1Object != null ? player1Object.name : "null")}, Player2: {(player2Object != null ? player2Object.name : "null")}");
        
        // 플레이어 오브젝트 찾기
        FindPlayerObjects();
        
        // 한 번 더 확인 후 위치 설정
        if (player1Object != null && player2Object != null)
        {
            // 잘못된 오브젝트가 할당되었는지 검증
            if (player1Object.name.ToLower().Contains("eye") || player2Object.name.ToLower().Contains("eye"))
            {
                Debug.LogError("잘못된 오브젝트가 할당되었습니다! Inspector에서 올바른 플레이어 오브젝트를 할당해주세요.");
                player1Object = null;
                player2Object = null;
                return;
            }
            
            // 플레이어 초기 위치 설정
            PositionPlayers();
        }
        else
        {
            Debug.LogError("플레이어 오브젝트를 찾을 수 없습니다. Inspector에서 직접 할당하거나 씬에서 올바른 이름으로 오브젝트를 설정해주세요.");
        }
    }

    void CreateDefaultSpawnPoints()
    {
        // 기본 스폰 포인트 생성 - 이미지에 보여진 정확한 위치로 설정
        GameObject spawnPointsHolder = new GameObject("SpawnPoints");
        
        // 플레이어 1 스폰 포인트 
        GameObject p1Spawn = new GameObject("Player1SpawnPoint");
        p1Spawn.transform.parent = spawnPointsHolder.transform;
        p1Spawn.transform.position = new Vector3(-1.31f, 1f, -5.81f); // 이미지에 보여진 player1 위치
        player1SpawnPoint = p1Spawn.transform;
        
        // 플레이어 2 스폰 포인트
        GameObject p2Spawn = new GameObject("Player2SpawnPoint");
        p2Spawn.transform.parent = spawnPointsHolder.transform;
        p2Spawn.transform.position = new Vector3(-0.98f, 1f, 10.207f); // 이미지에 보여진 player2 위치
        player2SpawnPoint = p2Spawn.transform;
        
        Debug.Log("플레이어 스폰 포인트가 생성되었습니다.");
    }
    
    void FindPlayerObjects()
    {
        // Inspector에서 직접 할당된 경우 그것을 우선 사용
        if (player1Object != null && player2Object != null)
        {
            Debug.Log("Inspector에서 할당된 플레이어 오브젝트를 사용합니다.");
            return;
        }

        // 정확한 이름으로 먼저 찾기 시도
        player1Object = GameObject.Find("player1");  // 실제 오브젝트 이름에 맞게 수정
        player2Object = GameObject.Find("Player2");  // 실제 오브젝트 이름에 맞게 수정
        
        if (player1Object != null && player2Object != null)
        {
            Debug.Log($"정확한 이름으로 플레이어 오브젝트를 찾았습니다: {player1Object.name}, {player2Object.name}");
            return;
        }
        
        // Player 태그로 시도 (eye 오브젝트 제외)
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag("Player");
        List<GameObject> validPlayers = new List<GameObject>();
        
        foreach (GameObject obj in taggedObjects)
        {
            // eye 오브젝트는 제외
            if (!obj.name.ToLower().Contains("eye"))
            {
                validPlayers.Add(obj);
            }
        }
        
        if (validPlayers.Count >= 2)
        {
            player1Object = validPlayers[0];
            player2Object = validPlayers[1];
            Debug.Log($"Player 태그로 플레이어 오브젝트를 찾았습니다: {player1Object.name}, {player2Object.name}");
            return;
        }
        
        // 모든 게임오브젝트에서 적합한 플레이어 오브젝트 찾기
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        List<GameObject> potentialPlayers = new List<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            // 씬에 활성화된 오브젝트만 확인
            if (obj.scene.IsValid() && obj.activeInHierarchy)
            {
                // eye가 포함된 이름은 제외하고, 실제 플레이어 오브젝트만 찾기
                string objName = obj.name.ToLower();
                if ((objName.Contains("player") || objName.Contains("robot") || objName.Contains("cylinder")) && 
                    !objName.Contains("eye") && !objName.Contains("camera") && !objName.Contains("ui"))
                {
                    potentialPlayers.Add(obj);
                    Debug.Log($"잠재적 플레이어 오브젝트 발견: {obj.name}");
                }
            }
        }
        
        if (potentialPlayers.Count >= 2)
        {
            player1Object = potentialPlayers[0];
            player2Object = potentialPlayers[1];
            Debug.Log($"검색으로 플레이어 오브젝트를 찾았습니다: {player1Object.name}, {player2Object.name}");
        }
        else if (potentialPlayers.Count == 1)
        {
            Debug.LogWarning($"플레이어 오브젝트를 하나만 찾았습니다: {potentialPlayers[0].name}");
            player1Object = potentialPlayers[0];
        }
        else
        {
            Debug.LogError("씬에서 적절한 플레이어 오브젝트를 찾을 수 없습니다! Inspector에서 직접 할당해주세요.");
        }
    }
    
    void PositionPlayers()
    {
        if (player1Object != null && player2Object != null)
        {
            Debug.Log($"플레이어 위치 설정 시작 - Player1: {player1Object.name}, Player2: {player2Object.name}");
            Debug.Log($"현재 Player1 위치: {player1Object.transform.position}");
            Debug.Log($"현재 Player2 위치: {player2Object.transform.position}");
            Debug.Log($"목표 Player1 위치: {player1SpawnPoint.position}");
            Debug.Log($"목표 Player2 위치: {player2SpawnPoint.position}");
            
            // 플레이어 1과 2를 각각의 스폰 포인트로 이동
            player1Object.transform.position = player1SpawnPoint.position;
            player2Object.transform.position = player2SpawnPoint.position;
            
            Debug.Log($"플레이어 위치 설정 완료 - Player1: {player1Object.transform.position}, Player2: {player2Object.transform.position}");
        }
        else
        {
            Debug.LogError($"플레이어 오브젝트가 설정되지 않았습니다! Player1: {(player1Object != null ? player1Object.name : "null")}, Player2: {(player2Object != null ? player2Object.name : "null")}");
        }
    }
    
    // 네트워크 환경에서의 플레이어 스폰 방법 (향후 확장용)
    void SpawnNetworkPlayers()
    {
        if (PhotonNetwork.IsConnected)
        {
            // 방장인 경우에만 플레이어 위치 설정 권한 부여
            if (PhotonNetwork.IsMasterClient)
            {
                // 네트워크 이벤트를 통해 모든 클라이언트에게 위치 설정 명령 전송
                photonView.RPC("RPC_SetPlayerPositions", RpcTarget.All);
            }
        }
        else
        {
            // 비 네트워크 환경일 경우 바로 위치 설정
            PositionPlayers();
        }
    }
    
    [PunRPC]
    void RPC_SetPlayerPositions()
    {
        PositionPlayers();
    }
    
    // 에디터에서 플레이어 오브젝트를 쉽게 설정할 수 있도록 도와주는 메서드
    [ContextMenu("Find And Set Player Objects")]
    void FindAndSetPlayerObjects()
    {
        FindPlayerObjects();
        Debug.Log($"플레이어 오브젝트 검색 완료 - Player1: {(player1Object != null ? player1Object.name : "찾을 수 없음")}, Player2: {(player2Object != null ? player2Object.name : "찾을 수 없음")}");
    }
    
    [ContextMenu("Reset Player Positions")]
    void ResetPlayerPositions()
    {
        PositionPlayers();
    }
    
    // Inspector에서 현재 설정된 플레이어 오브젝트 정보를 확인
    void OnValidate()
    {
        if (Application.isPlaying) return;
        
        if (player1Object != null && player1Object.name.ToLower().Contains("eye"))
        {
            Debug.LogWarning($"Player 1 Object로 '{player1Object.name}'이 할당되어 있습니다. 이것은 올바른 플레이어 오브젝트가 아닐 수 있습니다.");
        }
        
        if (player2Object != null && player2Object.name.ToLower().Contains("eye"))
        {
            Debug.LogWarning($"Player 2 Object로 '{player2Object.name}'이 할당되어 있습니다. 이것은 올바른 플레이어 오브젝트가 아닐 수 있습니다.");
        }
    }
}