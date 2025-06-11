using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// CourtScene에서 Player_Origin.prefab을 사용하여 네트워크 플레이어를 스폰하고 관리합니다.
/// VR 컨트롤러 움직임을 네트워크로 동기화하여 서로에게 보여줍니다.
/// </summary>
public class NetworkPlayerSpawner : MonoBehaviourPunCallbacks
{
    [Header("플레이어 프리팹 설정")]
    [SerializeField] private string playerPrefabName = "Player_Origin"; // Resources 폴더 내 프리팹 이름
    
    [Header("스폰 포인트")]
    [SerializeField] private Transform player1SpawnPoint; // Player 1 스폰 위치
    [SerializeField] private Transform player2SpawnPoint; // Player 2 스폰 위치
    
    [Header("자동 찾기")]
    [SerializeField] private bool autoFindSpawnPoints = true; // p1, p2 오브젝트 자동 찾기
    
    [Header("디버그")]
    [SerializeField] private bool showDebugInfo = true;
    
    private GameObject localPlayerInstance; // 로컬 플레이어 인스턴스
    private bool hasSpawned = false; // 스폰 완료 여부
    
    void Start()
    {
        // 네트워크 연결 확인
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("NetworkPlayerSpawner: Photon에 연결되지 않았습니다!");
            return;
        }
        
        // 스폰 포인트 자동 찾기
        if (autoFindSpawnPoints)
        {
            FindSpawnPoints();
        }
        
        // 플레이어 스폰
        StartCoroutine(SpawnPlayerWithDelay());
    }
    
    /// <summary>
    /// 씬에서 p1, p2 오브젝트를 찾아서 스폰 포인트로 설정
    /// </summary>
    void FindSpawnPoints()
    {
        if (player1SpawnPoint == null)
        {
            GameObject p1Object = GameObject.Find("p1");
            if (p1Object != null)
            {
                player1SpawnPoint = p1Object.transform;
                if (showDebugInfo) Debug.Log("Player 1 스폰 포인트 자동 찾기 완료: " + p1Object.name);
            }
        }
        
        if (player2SpawnPoint == null)
        {
            GameObject p2Object = GameObject.Find("p2");
            if (p2Object != null)
            {
                player2SpawnPoint = p2Object.transform;
                if (showDebugInfo) Debug.Log("Player 2 스폰 포인트 자동 찾기 완료: " + p2Object.name);
            }
        }
        
        // 스폰 포인트가 없으면 기본값 생성
        if (player1SpawnPoint == null || player2SpawnPoint == null)
        {
            CreateDefaultSpawnPoints();
        }
    }
    
    /// <summary>
    /// 기본 스폰 포인트 생성
    /// </summary>
    void CreateDefaultSpawnPoints()
    {
        GameObject spawnPointsHolder = new GameObject("SpawnPoints");
        
        if (player1SpawnPoint == null)
        {
            GameObject p1Spawn = new GameObject("Player1SpawnPoint");
            p1Spawn.transform.parent = spawnPointsHolder.transform;
            p1Spawn.transform.position = new Vector3(-1.31f, 1f, -5.81f);
            player1SpawnPoint = p1Spawn.transform;
            if (showDebugInfo) Debug.Log("기본 Player 1 스폰 포인트 생성됨");
        }
        
        if (player2SpawnPoint == null)
        {
            GameObject p2Spawn = new GameObject("Player2SpawnPoint");
            p2Spawn.transform.parent = spawnPointsHolder.transform;
            p2Spawn.transform.position = new Vector3(-0.98f, 1f, 10.207f);
            player2SpawnPoint = p2Spawn.transform;
            if (showDebugInfo) Debug.Log("기본 Player 2 스폰 포인트 생성됨");
        }
    }
    
    /// <summary>
    /// 약간의 지연 후 플레이어 스폰 (네트워크 안정화 대기)
    /// </summary>
    IEnumerator SpawnPlayerWithDelay()
    {
        // 네트워크 안정화 대기
        yield return new WaitForSeconds(0.5f);
        
        // 이미 스폰했으면 중복 방지
        if (hasSpawned) yield break;
        
        SpawnLocalPlayer();
    }
    
    /// <summary>
    /// 로컬 플레이어 스폰
    /// </summary>
    void SpawnLocalPlayer()
    {
        if (hasSpawned)
        {
            if (showDebugInfo) Debug.Log("이미 플레이어가 스폰되었습니다.");
            return;
        }
        
        // 스폰 위치 결정 (액터 번호에 따라)
        Transform spawnPoint = GetSpawnPointForPlayer();
        
        if (spawnPoint == null)
        {
            Debug.LogError("스폰 포인트를 찾을 수 없습니다!");
            return;
        }
        
        // 프리팹 로드 확인
        GameObject prefab = Resources.Load<GameObject>(playerPrefabName);
        if (prefab == null)
        {
            Debug.LogError($"플레이어 프리팹을 찾을 수 없습니다: {playerPrefabName}");
            return;
        }
        
        // PhotonView 확인
        PhotonView prefabPhotonView = prefab.GetComponent<PhotonView>();
        if (prefabPhotonView == null)
        {
            Debug.LogError($"플레이어 프리팹에 PhotonView가 없습니다: {playerPrefabName}");
            return;
        }
        
        // 네트워크 플레이어 인스턴스 생성
        localPlayerInstance = PhotonNetwork.Instantiate(
            playerPrefabName,
            spawnPoint.position,
            spawnPoint.rotation
        );
        
        hasSpawned = true;
        
        if (showDebugInfo)
        {
            Debug.Log($"플레이어 스폰 완료: {localPlayerInstance.name} " +
                     $"(Actor: {PhotonNetwork.LocalPlayer.ActorNumber}) " +
                     $"위치: {spawnPoint.position}");
        }
        
        // 스폰 완료 후 추가 설정
        StartCoroutine(ConfigurePlayerAfterSpawn());
    }
    
    /// <summary>
    /// 플레이어 액터 번호에 따른 스폰 포인트 결정
    /// </summary>
    Transform GetSpawnPointForPlayer()
    {
        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        
        // 액터 번호 1 = Player 1 위치, 액터 번호 2 = Player 2 위치
        if (actorNumber == 1)
        {
            return player1SpawnPoint;
        }
        else if (actorNumber == 2)
        {
            return player2SpawnPoint;
        }
        else
        {
            // 2명 이상인 경우 순환 배치
            return (actorNumber % 2 == 1) ? player1SpawnPoint : player2SpawnPoint;
        }
    }
    
    /// <summary>
    /// 스폰 후 플레이어 추가 설정
    /// </summary>
    IEnumerator ConfigurePlayerAfterSpawn()
    {
        // PhotonView 소유권이 확실히 설정될 때까지 대기
        yield return new WaitForSeconds(0.3f);
        
        if (localPlayerInstance == null) yield break;
        
        // PhotonView 확인
        PhotonView photonView = localPlayerInstance.GetComponent<PhotonView>();
        if (photonView == null)
        {
            Debug.LogError("스폰된 플레이어에 PhotonView가 없습니다!");
            yield break;
        }
        
        // 소유권 확인
        if (photonView.Owner == null)
        {
            Debug.LogError("PhotonView 소유권이 설정되지 않았습니다!");
            yield break;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"=== 플레이어 스폰 후 설정 ===");
            Debug.Log($"GameObject: {localPlayerInstance.name}");
            Debug.Log($"PhotonView Owner: {photonView.Owner.NickName} (Actor: {photonView.Owner.ActorNumber})");
            Debug.Log($"Local Player: {PhotonNetwork.LocalPlayer.NickName} (Actor: {PhotonNetwork.LocalPlayer.ActorNumber})");
            Debug.Log($"IsMine: {photonView.IsMine}");
            Debug.Log($"============================");
        }
        
        // PlayerSetup 컴포넌트 확인 (자동으로 설정됨)
        PlayerSetup playerSetup = localPlayerInstance.GetComponent<PlayerSetup>();
        if (playerSetup != null)
        {
            if (showDebugInfo)
            {
                Debug.Log($"PlayerSetup 컴포넌트 확인 완료 - 자동 설정 진행 중");
            }
        }
        else
        {
            Debug.LogWarning("PlayerSetup 컴포넌트가 없습니다!");
        }
        
        // PlayerNetworkSync 컴포넌트 확인
        PlayerNetworkSync networkSync = localPlayerInstance.GetComponent<PlayerNetworkSync>();
        if (networkSync == null)
        {
            Debug.LogWarning("PlayerNetworkSync 컴포넌트가 없습니다. VR 컨트롤러 동기화가 제한됩니다.");
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.Log("PlayerNetworkSync 컴포넌트 확인 완료 - VR 컨트롤러 동기화 준비됨");
            }
        }
        
        // PlayerAnimationSync 컴포넌트 확인
        PlayerAnimationSync animSync = localPlayerInstance.GetComponent<PlayerAnimationSync>();
        if (animSync == null)
        {
            Debug.LogWarning("PlayerAnimationSync 컴포넌트가 없습니다. 애니메이션 동기화가 제한됩니다.");
        }
    }
    
    /// <summary>
    /// 플레이어 재스폰 (디버그용)
    /// </summary>
    [ContextMenu("Respawn Player")]
    public void RespawnPlayer()
    {
        if (localPlayerInstance != null)
        {
            PhotonNetwork.Destroy(localPlayerInstance);
            localPlayerInstance = null;
        }
        
        hasSpawned = false;
        StartCoroutine(SpawnPlayerWithDelay());
    }
    
    /// <summary>
    /// 현재 스폰된 플레이어 정보 출력 (디버그용)
    /// </summary>
    [ContextMenu("Print Player Info")]
    public void PrintPlayerInfo()
    {
        if (localPlayerInstance == null)
        {
            Debug.Log("스폰된 플레이어가 없습니다.");
            return;
        }
        
        PlayerSetup playerSetup = localPlayerInstance.GetComponent<PlayerSetup>();
        if (playerSetup != null)
        {
            Debug.Log(playerSetup.GetPlayerInfo());
        }
        else
        {
            Debug.Log($"플레이어 인스턴스: {localPlayerInstance.name}, 위치: {localPlayerInstance.transform.position}");
        }
    }
    
    // Photon 콜백들
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (showDebugInfo)
        {
            Debug.Log($"플레이어가 방에 입장했습니다: {newPlayer.NickName} (Actor: {newPlayer.ActorNumber})");
        }
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (showDebugInfo)
        {
            Debug.Log($"플레이어가 방을 떠났습니다: {otherPlayer.NickName} (Actor: {otherPlayer.ActorNumber})");
        }
    }
    
    public override void OnLeftRoom()
    {
        // 방을 떠날 때 정리
        if (localPlayerInstance != null)
        {
            Destroy(localPlayerInstance);
            localPlayerInstance = null;
        }
        hasSpawned = false;
    }
} 