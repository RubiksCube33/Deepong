using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// CourtScene에서 플레이어를 동적으로 스폰하는 매니저
/// PhotonNetwork.Instantiate를 사용하여 각 플레이어가 자신만의 Player_Origin 프리팹을 생성
/// </summary>
public class NetworkPlayerSpawner : MonoBehaviourPunCallbacks
{
    [Header("플레이어 스폰 설정")]
    [SerializeField] private GameObject playerPrefab; // Player_Origin 프리팹
    [SerializeField] private Transform[] spawnPoints; // 스폰 위치들
    [SerializeField] private bool autoSpawnOnStart = true; // 시작 시 자동 스폰
    [SerializeField] private float spawnDelay = 1f; // 스폰 지연 시간
    
    [Header("스폰 위치 설정")]
    [SerializeField] private Vector3 player1SpawnPosition = new Vector3(-1.31f, 1f, -5.81f);
    [SerializeField] private Vector3 player2SpawnPosition = new Vector3(-0.98f, 1f, 10.207f);
    [SerializeField] private Vector3 player1SpawnRotation = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 player2SpawnRotation = new Vector3(0f, 180f, 0f);
    
    [Header("디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 현재 스폰된 플레이어 인스턴스
    private GameObject localPlayerInstance;
    private bool hasSpawned = false;
    
    void Start()
    {
        if (showDebugLogs)
        {
            Debug.Log($"[NetworkPlayerSpawner] 시작됨 - 네트워크 연결: {PhotonNetwork.IsConnected}, 방 입장: {PhotonNetwork.InRoom}");
            Debug.Log($"[NetworkPlayerSpawner] 액터 번호: {PhotonNetwork.LocalPlayer?.ActorNumber}, 닉네임: {PhotonNetwork.LocalPlayer?.NickName}");
            Debug.Log($"[NetworkPlayerSpawner] 방 이름: {PhotonNetwork.CurrentRoom?.Name}, 방 인원: {PhotonNetwork.CurrentRoom?.PlayerCount}/{PhotonNetwork.CurrentRoom?.MaxPlayers}");
        }
            
        // 프리팹이 설정되지 않은 경우 Resources에서 로드
        if (playerPrefab == null)
        {
            LoadPlayerPrefab();
        }
        
        // 씬의 p1, p2 오브젝트를 우선적으로 찾기
        FindSceneSpawnPoints();
        
        // 스폰 포인트가 여전히 설정되지 않은 경우 기본 위치 생성
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            CreateDefaultSpawnPoints();
        }
        
        // 자동 스폰이 활성화된 경우
        if (autoSpawnOnStart)
        {
            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                if (showDebugLogs)
                    Debug.Log($"[NetworkPlayerSpawner] 자동 스폰 시작 - {spawnDelay}초 후 실행");
                StartCoroutine(DelayedSpawn());
            }
            else
            {
                Debug.LogWarning($"[NetworkPlayerSpawner] 네트워크 상태 불량 - Connected: {PhotonNetwork.IsConnected}, InRoom: {PhotonNetwork.InRoom}");
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.Log("[NetworkPlayerSpawner] 자동 스폰이 비활성화되어 있습니다.");
        }
    }
    
    /// <summary>
    /// Resources 폴더에서 플레이어 프리팹 로드
    /// </summary>
    void LoadPlayerPrefab()
    {
        // Resources 폴더에서 Player_Origin 프리팹 찾기
        playerPrefab = Resources.Load<GameObject>("Player_Origin");
        
        if (playerPrefab == null)
        {
            // 다른 이름들도 시도
            string[] possibleNames = { "PlayerPrefab", "Player", "VRPlayer", "NetworkPlayer" };
            
            foreach (string name in possibleNames)
            {
                playerPrefab = Resources.Load<GameObject>(name);
                if (playerPrefab != null)
                {
                    if (showDebugLogs)
                        Debug.Log($"[NetworkPlayerSpawner] 플레이어 프리팹 로드됨: {name}");
                    break;
                }
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.Log("[NetworkPlayerSpawner] Player_Origin 프리팹 로드됨");
        }
        
        if (playerPrefab == null)
        {
            Debug.LogError("[NetworkPlayerSpawner] 플레이어 프리팹을 찾을 수 없습니다! Resources 폴더에 Player_Origin.prefab이 있는지 확인하세요.");
        }
    }
    
    /// <summary>
    /// 씬에서 p1, p2 오브젝트를 찾아서 스폰 포인트로 설정
    /// </summary>
    void FindSceneSpawnPoints()
    {
        GameObject p1Object = GameObject.Find("p1");
        GameObject p2Object = GameObject.Find("p2");
        
        if (p1Object != null && p2Object != null)
        {
            // 씬에 p1, p2가 있으면 그 위치를 사용
            player1SpawnPosition = p1Object.transform.position;
            player2SpawnPosition = p2Object.transform.position;
            player1SpawnRotation = p1Object.transform.rotation.eulerAngles;
            player2SpawnRotation = p2Object.transform.rotation.eulerAngles;
            
            // 스폰 포인트 배열도 설정
            spawnPoints = new Transform[] { p1Object.transform, p2Object.transform };
            
            if (showDebugLogs)
            {
                Debug.Log($"[NetworkPlayerSpawner] 씬의 p1, p2 오브젝트를 스폰 포인트로 사용");
                Debug.Log($"[NetworkPlayerSpawner] p1 위치: {player1SpawnPosition}, 회전: {player1SpawnRotation}");
                Debug.Log($"[NetworkPlayerSpawner] p2 위치: {player2SpawnPosition}, 회전: {player2SpawnRotation}");
            }
        }
        else
        {
            if (showDebugLogs)
            {
                string missingObjects = "";
                if (p1Object == null) missingObjects += "p1 ";
                if (p2Object == null) missingObjects += "p2 ";
                Debug.Log($"[NetworkPlayerSpawner] 씬에서 오브젝트를 찾지 못함: {missingObjects}");
            }
        }
    }
    
    /// <summary>
    /// 기본 스폰 포인트 생성 (p1, p2가 없을 때만)
    /// </summary>
    void CreateDefaultSpawnPoints()
    {
        if (showDebugLogs)
            Debug.Log("[NetworkPlayerSpawner] 기본 스폰 포인트를 생성합니다.");
            
        GameObject spawnPointHolder = new GameObject("SpawnPoints");
        spawnPointHolder.transform.SetParent(transform);
        
        // Player 1 스폰 포인트
        GameObject sp1 = new GameObject("Player1SpawnPoint");
        sp1.transform.SetParent(spawnPointHolder.transform);
        sp1.transform.position = player1SpawnPosition;
        sp1.transform.rotation = Quaternion.Euler(player1SpawnRotation);
        
        // Player 2 스폰 포인트
        GameObject sp2 = new GameObject("Player2SpawnPoint");
        sp2.transform.SetParent(spawnPointHolder.transform);
        sp2.transform.position = player2SpawnPosition;
        sp2.transform.rotation = Quaternion.Euler(player2SpawnRotation);
        
        spawnPoints = new Transform[] { sp1.transform, sp2.transform };
        
        if (showDebugLogs)
            Debug.Log("[NetworkPlayerSpawner] 기본 스폰 포인트 생성됨");
    }
    
    /// <summary>
    /// 지연된 스폰 (네트워크 안정화 대기)
    /// </summary>
    IEnumerator DelayedSpawn()
    {
        yield return new WaitForSeconds(spawnDelay);
        SpawnLocalPlayer();
    }
    
    /// <summary>
    /// 로컬 플레이어 스폰
    /// </summary>
    public void SpawnLocalPlayer()
    {
        // 이미 스폰되었거나 프리팹이 없는 경우 중단
        if (hasSpawned || playerPrefab == null) return;
        
        // 네트워크 상태 확인
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[NetworkPlayerSpawner] 네트워크에 연결되지 않았거나 방에 입장하지 않았습니다.");
            return;
        }
        
        // 스폰 위치 결정
        Transform spawnPoint = GetSpawnPoint();
        Vector3 spawnPosition = spawnPoint.position;
        Quaternion spawnRotation = spawnPoint.rotation;
        
        if (showDebugLogs)
        {
            Debug.Log($"[NetworkPlayerSpawner] 플레이어 스폰 중... 위치: {spawnPosition}, 회전: {spawnRotation.eulerAngles}");
            Debug.Log($"[NetworkPlayerSpawner] 액터 번호: {PhotonNetwork.LocalPlayer.ActorNumber}, 닉네임: {PhotonNetwork.LocalPlayer.NickName}");
        }
        
        try
        {
            // PhotonNetwork.Instantiate로 네트워크 플레이어 생성
            localPlayerInstance = PhotonNetwork.Instantiate(
                playerPrefab.name,
                spawnPosition,
                spawnRotation
            );
            
            hasSpawned = true;
            
            if (showDebugLogs)
                Debug.Log($"[NetworkPlayerSpawner] 플레이어 스폰 완료: {localPlayerInstance.name}");
                
            // 스폰 후 추가 설정
            SetupSpawnedPlayer(localPlayerInstance);
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NetworkPlayerSpawner] 플레이어 스폰 실패: {e.Message}");
        }
    }
    
    /// <summary>
    /// 스폰된 플레이어 추가 설정
    /// </summary>
    void SetupSpawnedPlayer(GameObject playerInstance)
    {
        if (playerInstance == null) return;
        
        // PlayerSetup 컴포넌트 확인 및 설정
        PlayerSetup playerSetup = playerInstance.GetComponent<PlayerSetup>();
        if (playerSetup != null)
        {
            // 액터 번호에 따라 플레이어 구분
            bool isPlayerOne = PhotonNetwork.LocalPlayer.ActorNumber == 1;
            
            if (showDebugLogs)
                Debug.Log($"[NetworkPlayerSpawner] 플레이어 설정 - Player1: {isPlayerOne}");
        }
        
        // 추가적인 초기화가 필요한 경우 여기서 수행
        InitializePlayerComponents(playerInstance);
    }
    
    /// <summary>
    /// 플레이어 컴포넌트 초기화
    /// </summary>
    void InitializePlayerComponents(GameObject playerInstance)
    {
        // VRHumanoidController 확인
        VRHumanoidController vrController = playerInstance.GetComponent<VRHumanoidController>();
        if (vrController != null && showDebugLogs)
        {
            Debug.Log($"[NetworkPlayerSpawner] VRHumanoidController 발견 - Robot 모드: {vrController.IsRobotMode}");
        }
        
        // PlayerNetworkSync 확인
        PlayerNetworkSync networkSync = playerInstance.GetComponent<PlayerNetworkSync>();
        if (networkSync != null && showDebugLogs)
        {
            Debug.Log("[NetworkPlayerSpawner] PlayerNetworkSync 컴포넌트 확인됨");
        }
        
        // PhotonView 확인
        PhotonView photonView = playerInstance.GetComponent<PhotonView>();
        if (photonView != null && showDebugLogs)
        {
            Debug.Log($"[NetworkPlayerSpawner] PhotonView 확인됨 - ViewID: {photonView.ViewID}, IsMine: {photonView.IsMine}");
        }
    }
    
    /// <summary>
    /// 스폰 포인트 결정
    /// </summary>
    Transform GetSpawnPoint()
    {
        // 스폰 포인트가 설정되어 있다면 사용
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            int spawnIndex = (actorNumber - 1) % spawnPoints.Length; // 액터 번호를 인덱스로 변환
            return spawnPoints[spawnIndex];
        }
        
        // 스폰 포인트가 없다면 null 반환
        return null;
    }
    
    /// <summary>
    /// 플레이어 액터 번호에 따른 스폰 위치와 회전 반환
    /// </summary>
    bool GetSpawnPositionForPlayer(int actorNumber, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        
        if (showDebugLogs)
            Debug.Log($"[NetworkPlayerSpawner] GetSpawnPositionForPlayer 호출됨 - ActorNumber: {actorNumber}");
        
        // 먼저 spawnPoints 배열 사용 시도
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int spawnIndex = (actorNumber - 1) % spawnPoints.Length;
            if (spawnIndex >= 0 && spawnIndex < spawnPoints.Length && spawnPoints[spawnIndex] != null)
            {
                position = spawnPoints[spawnIndex].position;
                rotation = spawnPoints[spawnIndex].rotation;
                
                if (showDebugLogs)
                    Debug.Log($"[NetworkPlayerSpawner] 스폰 포인트 배열 사용 - Index: {spawnIndex}, Position: {position}");
                return true;
            }
        }
        
        // spawnPoints가 없거나 부족하면 개별 위치 사용
        if (actorNumber == 1)
        {
            position = player1SpawnPosition;
            rotation = Quaternion.Euler(player1SpawnRotation);
            
            if (showDebugLogs)
                Debug.Log($"[NetworkPlayerSpawner] Player1 스폰 위치 사용 - Position: {position}");
            return true;
        }
        else if (actorNumber == 2)
        {
            position = player2SpawnPosition;
            rotation = Quaternion.Euler(player2SpawnRotation);
            
            if (showDebugLogs)
                Debug.Log($"[NetworkPlayerSpawner] Player2 스폰 위치 사용 - Position: {position}");
            return true;
        }
        else
        {
            // 3명 이상의 플레이어를 위한 기본 처리 (원형 배치)
            float angle = (actorNumber - 1) * (360f / 8f); // 최대 8명까지 원형 배치
            float radius = 5f;
            
            position = new Vector3(
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius,
                1f,
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius
            );
            rotation = Quaternion.LookRotation(-position.normalized);
            
            if (showDebugLogs)
                Debug.Log($"[NetworkPlayerSpawner] 추가 플레이어 원형 배치 - ActorNumber: {actorNumber}, Position: {position}");
            return true;
        }
    }
    
    /// <summary>
    /// 수동 스폰 (버튼 등에서 호출)
    /// </summary>
    public void ManualSpawn()
    {
        if (showDebugLogs)
            Debug.Log("[NetworkPlayerSpawner] 수동 스폰 요청됨");
        
        if (localPlayerInstance != null)
        {
            Debug.LogWarning("[NetworkPlayerSpawner] 이미 스폰된 플레이어가 있습니다.");
            return;
        }
        
        // 더 확실한 재시도 로직 사용
        StartCoroutine(RetrySpawnWithDelay());
    }
    
    /// <summary>
    /// 플레이어 제거
    /// </summary>
    public void DestroyLocalPlayer()
    {
        if (localPlayerInstance != null)
        {
            if (showDebugLogs)
                Debug.Log("[NetworkPlayerSpawner] 로컬 플레이어 제거 중...");
                
            PhotonNetwork.Destroy(localPlayerInstance);
            localPlayerInstance = null;
            hasSpawned = false;
        }
    }
    
    // Photon 콜백들
    public override void OnJoinedRoom()
    {
        if (showDebugLogs)
        {
            Debug.Log($"[NetworkPlayerSpawner] 방 입장됨: {PhotonNetwork.CurrentRoom.Name}");
            Debug.Log($"[NetworkPlayerSpawner] 내 액터 번호: {PhotonNetwork.LocalPlayer.ActorNumber}");
            Debug.Log($"[NetworkPlayerSpawner] 현재 방 인원: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
            
            // 방에 있는 모든 플레이어 출력
            foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                Debug.Log($"[NetworkPlayerSpawner] 방 내 플레이어: ActorNumber={player.ActorNumber}, NickName={player.NickName}, IsMasterClient={player.IsMasterClient}");
            }
        }
        
        // 방에 입장한 후 자동 스폰 시도 (아직 스폰되지 않은 경우)
        if (autoSpawnOnStart && !hasSpawned)
        {
            if (showDebugLogs)
                Debug.Log($"[NetworkPlayerSpawner] 방 입장 후 자동 스폰 시작 - {spawnDelay}초 지연");
            StartCoroutine(DelayedSpawn());
        }
    }
    
    public override void OnLeftRoom()
    {
        if (showDebugLogs)
            Debug.Log("[NetworkPlayerSpawner] 방에서 나감");
            
        // 로컬 플레이어 인스턴스 정리
        if (localPlayerInstance != null)
        {
            DestroyLocalPlayer();
        }
        
        hasSpawned = false;
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (showDebugLogs)
            Debug.Log($"[NetworkPlayerSpawner] 새 플레이어 입장: {newPlayer.NickName} (ActorNumber: {newPlayer.ActorNumber})");
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (showDebugLogs)
            Debug.Log($"[NetworkPlayerSpawner] 플레이어 퇴장: {otherPlayer.NickName} (ActorNumber: {otherPlayer.ActorNumber})");
    }
    
    void OnDestroy()
    {
        // 정리 작업
        if (localPlayerInstance != null && PhotonNetwork.IsConnected)
        {
            try
            {
                PhotonNetwork.Destroy(localPlayerInstance);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[NetworkPlayerSpawner] 플레이어 제거 중 오류: {e.Message}");
            }
        }
    }
    
    void OnEnable()
    {
        // 씬 변경 이벤트 구독
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
    }
    
    void OnDisable()
    {
        // 씬 변경 이벤트 구독 해제
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }
    
    /// <summary>
    /// 새 씬이 로드될 때 호출
    /// </summary>
    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (showDebugLogs)
            Debug.Log($"[NetworkPlayerSpawner] 씬 로드됨: {scene.name}");
        
        // 새 씬에서 자동 스폰이 필요한 경우
        if (autoSpawnOnStart && !hasSpawned && PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            StartCoroutine(DelayedSpawn());
        }
    }
    
    /// <summary>
    /// 씬이 언로드될 때 호출
    /// </summary>
    void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
    {
        if (showDebugLogs)
            Debug.Log($"[NetworkPlayerSpawner] 씬 언로드됨: {scene.name}");
    }
    
    // 디버그용 메서드들
    [ContextMenu("Manual Spawn Player")]
    void DebugSpawnPlayer()
    {
        ManualSpawn();
    }
    
    [ContextMenu("Destroy Local Player")]  
    void DebugDestroyPlayer()
    {
        DestroyLocalPlayer();
    }
    
    [ContextMenu("Print Network Status")]
    void DebugPrintNetworkStatus()
    {
        Debug.Log($"Connected: {PhotonNetwork.IsConnected}, InRoom: {PhotonNetwork.InRoom}, ActorNumber: {PhotonNetwork.LocalPlayer?.ActorNumber}, RoomName: {PhotonNetwork.CurrentRoom?.Name}");
    }
    
    // 공개 설정 메서드들
    /// <summary>
    /// 스폰 위치를 외부에서 설정
    /// </summary>
    public void SetSpawnPositions(Vector3 player1Pos, Vector3 player2Pos)
    {
        player1SpawnPosition = player1Pos;
        player2SpawnPosition = player2Pos;
        
        if (showDebugLogs)
            Debug.Log($"[NetworkPlayerSpawner] 스폰 위치 업데이트 - P1: {player1Pos}, P2: {player2Pos}");
    }
    
    /// <summary>
    /// 스폰 회전을 외부에서 설정
    /// </summary>
    public void SetSpawnRotations(Vector3 player1Rot, Vector3 player2Rot)
    {
        player1SpawnRotation = player1Rot;
        player2SpawnRotation = player2Rot;
        
        if (showDebugLogs)
            Debug.Log($"[NetworkPlayerSpawner] 스폰 회전 업데이트 - P1: {player1Rot}, P2: {player2Rot}");
    }
    
    /// <summary>
    /// 스폰 포인트 Transform 배열을 설정
    /// </summary>
    public void SetSpawnPoints(Transform[] newSpawnPoints)
    {
        spawnPoints = newSpawnPoints;
        
        if (showDebugLogs)
            Debug.Log($"[NetworkPlayerSpawner] 스폰 포인트 배열 업데이트 - 개수: {newSpawnPoints?.Length}");
    }
    
    /// <summary>
    /// 플레이어 프리팹을 외부에서 설정
    /// </summary>
    public void SetPlayerPrefab(GameObject prefab)
    {
        playerPrefab = prefab;
        
        if (showDebugLogs)
            Debug.Log($"[NetworkPlayerSpawner] 플레이어 프리팹 설정: {prefab?.name}");
    }
    
    /// <summary>
    /// 현재 스폰 상태 반환
    /// </summary>
    public bool IsPlayerSpawned()
    {
        return hasSpawned && localPlayerInstance != null;
    }
    
    /// <summary>
    /// 스폰된 플레이어 인스턴스 반환
    /// </summary>
    public GameObject GetSpawnedPlayer()
    {
        return localPlayerInstance;
    }

    public bool SpawnPlayer()
    {
        if (showDebugLogs)
        {
            Debug.Log($"[NetworkPlayerSpawner] SpawnPlayer 호출됨");
            Debug.Log($"[NetworkPlayerSpawner] 네트워크 상태 - Connected: {PhotonNetwork.IsConnected}, InRoom: {PhotonNetwork.InRoom}");
            Debug.Log($"[NetworkPlayerSpawner] 현재 플레이어 - ActorNumber: {PhotonNetwork.LocalPlayer?.ActorNumber}, NickName: {PhotonNetwork.LocalPlayer?.NickName}");
            
            if (PhotonNetwork.CurrentRoom != null)
            {
                Debug.Log($"[NetworkPlayerSpawner] 방 정보 - Name: {PhotonNetwork.CurrentRoom.Name}, PlayerCount: {PhotonNetwork.CurrentRoom.PlayerCount}");
                foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
                {
                    Debug.Log($"[NetworkPlayerSpawner] 방 내 플레이어: ActorNumber={player.ActorNumber}, NickName={player.NickName}");
                }
            }
        }

        // 네트워크 연결 확인
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogError($"[NetworkPlayerSpawner] 네트워크 연결 실패 - Connected: {PhotonNetwork.IsConnected}, InRoom: {PhotonNetwork.InRoom}");
            return false;
        }

        // 이미 스폰된 플레이어가 있는지 확인
        if (localPlayerInstance != null)
        {
            if (showDebugLogs)
                Debug.Log($"[NetworkPlayerSpawner] 이미 스폰된 플레이어가 있습니다: {localPlayerInstance.name}");
            return false;
        }

        // 프리팹 확인
        if (playerPrefab == null)
        {
            Debug.LogError("[NetworkPlayerSpawner] 플레이어 프리팹이 설정되지 않았습니다!");
            return false;
        }

        // 스폰 위치 결정
        Vector3 spawnPosition;
        Quaternion spawnRotation;
        
        if (!GetSpawnPositionForPlayer(PhotonNetwork.LocalPlayer.ActorNumber, out spawnPosition, out spawnRotation))
        {
            Debug.LogError($"[NetworkPlayerSpawner] 플레이어 {PhotonNetwork.LocalPlayer.ActorNumber}의 스폰 위치를 결정할 수 없습니다!");
            return false;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[NetworkPlayerSpawner] 스폰 위치 결정됨 - Position: {spawnPosition}, Rotation: {spawnRotation.eulerAngles}");
        }

        try
        {
            // 네트워크 플레이어 인스턴스 생성
            GameObject playerInstance = PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, spawnRotation);
            
            if (playerInstance == null)
            {
                Debug.LogError("[NetworkPlayerSpawner] PhotonNetwork.Instantiate가 null을 반환했습니다!");
                return false;
            }

            localPlayerInstance = playerInstance;
            
            if (showDebugLogs)
            {
                Debug.Log($"[NetworkPlayerSpawner] 플레이어 스폰 성공!");
                Debug.Log($"[NetworkPlayerSpawner] - Instance: {playerInstance.name}");
                Debug.Log($"[NetworkPlayerSpawner] - Position: {playerInstance.transform.position}");
                Debug.Log($"[NetworkPlayerSpawner] - Actor: {PhotonNetwork.LocalPlayer.ActorNumber}");
                
                // PhotonView 확인
                PhotonView pv = playerInstance.GetComponent<PhotonView>();
                if (pv != null)
                {
                    Debug.Log($"[NetworkPlayerSpawner] - PhotonView ViewID: {pv.ViewID}, IsMine: {pv.IsMine}");
                }
                else
                {
                    Debug.LogWarning("[NetworkPlayerSpawner] - PhotonView가 없습니다!");
                }
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NetworkPlayerSpawner] 플레이어 스폰 중 오류 발생: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// 강제 스폰 - 디버깅용
    /// </summary>
    [ContextMenu("Force Spawn Now")]
    public void ForceSpawnNow()
    {
        if (showDebugLogs)
            Debug.Log("[NetworkPlayerSpawner] 강제 스폰 실행됨");
            
        bool result = SpawnPlayer();
        Debug.Log($"[NetworkPlayerSpawner] 강제 스폰 결과: {result}");
    }
    
    /// <summary>
    /// 모든 연결된 플레이어 수동 스폰 - 마스터 클라이언트에서만 실행
    /// </summary>
    [ContextMenu("Force Spawn All Players")]
    public void ForceSpawnAllPlayers()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[NetworkPlayerSpawner] 마스터 클라이언트만 모든 플레이어 스폰을 실행할 수 있습니다.");
            return;
        }
        
        // 모든 NetworkPlayerSpawner에게 스폰 요청
        NetworkPlayerSpawner[] allSpawners = FindObjectsOfType<NetworkPlayerSpawner>();
        foreach (var spawner in allSpawners)
        {
            if (spawner.localPlayerInstance == null)
            {
                spawner.ForceSpawnNow();
            }
        }
    }
    
    [PunRPC]
    void RPC_ForceSpawn()
    {
        // PhotonView가 없으면 RPC는 작동하지 않으므로 제거
        // if (showDebugLogs)
        //     Debug.Log("[NetworkPlayerSpawner] RPC 강제 스폰 받음");
            
        // if (localPlayerInstance == null)
        // {
        //     StartCoroutine(DelayedSpawn());
        // }
        // else
        // {
        //     Debug.Log("[NetworkPlayerSpawner] 이미 스폰된 플레이어가 있어 스킵됨");
        // }
    }
    
    /// <summary>
    /// 지연된 스폰을 재시도하는 메서드
    /// </summary>
    public void RetrySpawn()
    {
        if (localPlayerInstance == null && PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            if (showDebugLogs)
                Debug.Log("[NetworkPlayerSpawner] 스폰 재시도 중...");
            StartCoroutine(DelayedSpawn());
        }
    }
    
    /// <summary>
    /// 더 확실한 지연된 스폰 (여러 번 시도)
    /// </summary>
    IEnumerator RetrySpawnWithDelay(int maxRetries = 5)
    {
        int attempts = 0;
        
        while (attempts < maxRetries && localPlayerInstance == null && PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            if (showDebugLogs)
                Debug.Log($"[NetworkPlayerSpawner] 스폰 시도 {attempts + 1}/{maxRetries}");
                
            bool spawnResult = SpawnPlayer();
            
            if (spawnResult)
            {
                if (showDebugLogs)
                    Debug.Log($"[NetworkPlayerSpawner] 스폰 성공! (시도 {attempts + 1}회)");
                yield break;
            }
            
            attempts++;
            yield return new WaitForSeconds(1f); // 1초씩 간격을 두고 재시도
        }
        
        if (localPlayerInstance == null)
        {
            Debug.LogError($"[NetworkPlayerSpawner] {maxRetries}회 시도 후에도 스폰 실패!");
        }
    }
    
    /// <summary>
    /// 네트워크 상태 체크 및 리포트
    /// </summary>
    [ContextMenu("Check Network Status")]
    public void CheckNetworkStatus()
    {
        Debug.Log("=== NetworkPlayerSpawner 상태 리포트 ===");
        Debug.Log($"IsConnected: {PhotonNetwork.IsConnected}");
        Debug.Log($"InRoom: {PhotonNetwork.InRoom}");
        Debug.Log($"RoomName: {PhotonNetwork.CurrentRoom?.Name}");
        Debug.Log($"PlayerCount: {PhotonNetwork.CurrentRoom?.PlayerCount}");
        Debug.Log($"LocalPlayer ActorNumber: {PhotonNetwork.LocalPlayer?.ActorNumber}");
        Debug.Log($"LocalPlayer NickName: {PhotonNetwork.LocalPlayer?.NickName}");
        Debug.Log($"HasSpawned: {hasSpawned}");
        Debug.Log($"LocalPlayerInstance: {(localPlayerInstance != null ? localPlayerInstance.name : "null")}");
        Debug.Log($"PlayerPrefab: {(playerPrefab != null ? playerPrefab.name : "null")}");
        
        if (PhotonNetwork.CurrentRoom != null)
        {
            Debug.Log("=== 방 내 모든 플레이어 ===");
            foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                Debug.Log($"- ActorNumber: {player.ActorNumber}, NickName: {player.NickName}, IsMasterClient: {player.IsMasterClient}");
            }
        }
        
        // 스폰 포인트 정보
        Debug.Log("=== 스폰 포인트 정보 ===");
        Debug.Log($"Player1 Position: {player1SpawnPosition}");
        Debug.Log($"Player2 Position: {player2SpawnPosition}");
        Debug.Log($"SpawnPoints Array Length: {(spawnPoints?.Length ?? 0)}");
        
        Debug.Log("=== 상태 리포트 끝 ===");
    }
} 