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
        // 스폰 포인트가 지정되지 않았을 경우 기본값 생성
        if (player1SpawnPoint == null || player2SpawnPoint == null)
        {
            CreateDefaultSpawnPoints();
        }

        // 플레이어 오브젝트 찾기
        FindPlayerObjects();
        
        // 플레이어 초기 위치 설정
        PositionPlayers();
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
        // 씬에서 player1과 player2라는 이름의 오브젝트 찾기
        player1Object = GameObject.Find("player1");
        player2Object = GameObject.Find("player2");
        
        if (player1Object != null && player2Object != null)
        {
            Debug.Log("player1과 player2 오브젝트를 찾았습니다.");
            return;
        }
        
        // 이름으로 찾기 실패한 경우 태그로 시도
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        
        if (playerObjects.Length >= 2)
        {
            player1Object = playerObjects[0];
            player2Object = playerObjects[1];
            Debug.Log("Player 태그로 플레이어 오브젝트를 찾았습니다.");
            return;
        }
        
        // 위의 방법으로 찾지 못한 경우, 실린더 형태를 가진 오브젝트 찾기
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        List<GameObject> potentialPlayers = new List<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            // 이름에 Player나 Cylinder가 포함된 오브젝트 찾기
            if (obj.name.Contains("Player") || obj.name.Contains("Cylinder") || 
                obj.name.Contains("player"))
            {
                potentialPlayers.Add(obj);
            }
            // 또는 실린더 메시를 가진 오브젝트 찾기
            else if (obj.GetComponent<MeshFilter>() != null && 
                     obj.GetComponent<MeshFilter>().sharedMesh != null && 
                     obj.GetComponent<MeshFilter>().sharedMesh.name.Contains("Cylinder"))
            {
                potentialPlayers.Add(obj);
            }
        }
        
        if (potentialPlayers.Count >= 2)
        {
            player1Object = potentialPlayers[0];
            player2Object = potentialPlayers[1];
            Debug.Log("이름 또는 메시 형태로 플레이어 오브젝트를 찾았습니다.");
        }
        else
        {
            Debug.LogError("씬에서 충분한 수의 플레이어 오브젝트를 찾을 수 없습니다!");
        }
    }
    
    void PositionPlayers()
    {
        if (player1Object != null && player2Object != null)
        {
            // 플레이어 1과 2를 각각의 스폰 포인트로 이동
            player1Object.transform.position = player1SpawnPoint.position;
            player2Object.transform.position = player2SpawnPoint.position;
            
            Debug.Log("플레이어 위치가 설정되었습니다.");
        }
        else
        {
            Debug.LogError("플레이어 오브젝트가 설정되지 않았습니다!");
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
}