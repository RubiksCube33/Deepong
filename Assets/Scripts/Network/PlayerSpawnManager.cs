using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// 방장과 참가자를 구분하여 플레이어를 스폰하는 매니저
/// 방장: p1 위치에 로컬 플레이어, p2 위치에 원격 플레이어 스폰
/// 참가자: p2 위치에 로컬 플레이어, p1 위치에 원격 플레이어 스폰
/// </summary>
public class PlayerSpawnManager : MonoBehaviourPunCallbacks
{
    [Header("스폰 위치")]
    public Transform p1SpawnPoint; // Player 1 스폰 위치
    public Transform p2SpawnPoint; // Player 2 스폰 위치
    
    [Header("플레이어 프리팹")]
    private string localPlayerPrefabPath = "Player_Origin"; // 로컬 플레이어 프리팹 경로
    private string remotePlayerPrefabPath = "2P_Player_Origin"; // 원격 플레이어 프리팹 경로
    
    private GameObject localPlayerInstance; // 로컬 플레이어 인스턴스
    private bool isRoomMaster; // 방장 여부
    private bool hasSpawned = false; // 스폰 완료 여부
    
    void Start()
    {
        Debug.Log("=== PlayerSpawnManager 시작 ===");
        Debug.Log($"PhotonNetwork 연결 상태: {PhotonNetwork.IsConnected}");
        Debug.Log($"방에 입장 상태: {PhotonNetwork.InRoom}");
        Debug.Log($"현재 방 이름: {(PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : "없음")}");
        
        // 스폰 포인트 자동 찾기
        FindSpawnPoints();
        
        // 방장 여부 확인
        isRoomMaster = PhotonNetwork.IsMasterClient;
        
        Debug.Log($"PlayerSpawnManager 초기화 완료 - 방장 여부: {isRoomMaster}");
        if (p1SpawnPoint != null && p2SpawnPoint != null)
        {
            Debug.Log($"p1 위치: {p1SpawnPoint.position}, p2 위치: {p2SpawnPoint.position}");
        }
        
        // 이미 방에 있다면 즉시 스폰 시도 (씬 플로우 고려)
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("이미 방에 있으므로 즉시 스폰을 시도합니다. (씬 전환으로 인한 상황)");
            StartCoroutine(SpawnPlayersWithDelay(1f)); // 씬 로딩 완료를 위해 1초 대기
        }
        else
        {
            Debug.Log("방에 입장하지 않은 상태입니다. OnJoinedRoom을 기다립니다.");
        }
    }
    
    /// <summary>
    /// 스폰 포인트 찾기
    /// </summary>
    private void FindSpawnPoints()
    {
        if (p1SpawnPoint == null)
        {
            GameObject p1Object = GameObject.Find("p1");
            if (p1Object != null)
            {
                p1SpawnPoint = p1Object.transform;
                Debug.Log($"p1 스폰 포인트 찾음: {p1SpawnPoint.position}");
            }
        }
        
        if (p2SpawnPoint == null)
        {
            GameObject p2Object = GameObject.Find("p2");
            if (p2Object != null)
            {
                p2SpawnPoint = p2Object.transform;
                Debug.Log($"p2 스폰 포인트 찾음: {p2SpawnPoint.position}");
            }
        }
        
        // 스폰 포인트 확인
        if (p1SpawnPoint == null || p2SpawnPoint == null)
        {
            Debug.LogError("PlayerSpawnManager: p1 또는 p2 스폰 포인트를 찾을 수 없습니다!");
            
            // 기본 위치 설정
            if (p1SpawnPoint == null)
            {
                GameObject p1Default = new GameObject("p1_default");
                p1Default.transform.position = new Vector3(-1.31f, 1f, -5.81f);
                p1SpawnPoint = p1Default.transform;
                Debug.Log("p1 기본 위치 생성됨");
            }
            
            if (p2SpawnPoint == null)
            {
                GameObject p2Default = new GameObject("p2_default");
                p2Default.transform.position = new Vector3(-0.98f, 1f, 10.207f);
                p2SpawnPoint = p2Default.transform;
                Debug.Log("p2 기본 위치 생성됨");
            }
        }
    }
    
    public override void OnJoinedRoom()
    {
        Debug.Log("=== OnJoinedRoom 호출됨 ===");
        Debug.Log($"방 이름: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"플레이어 수: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
        Debug.Log($"방장 여부: {PhotonNetwork.IsMasterClient}");
        
        // 이미 스폰했다면 중복 실행 방지
        if (hasSpawned)
        {
            Debug.LogWarning("이미 플레이어가 스폰되었습니다.");
            return;
        }
        
        // 잠시 대기 후 스폰 (다른 플레이어들이 준비될 시간을 줌)
        StartCoroutine(SpawnPlayersWithDelay(1f));
    }
    
    private IEnumerator SpawnPlayersWithDelay(float delay)
    {
        Debug.Log($"{delay}초 후 스폰을 시작합니다...");
        yield return new WaitForSeconds(delay);
        
        // 네트워크 상태 재확인
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogError("네트워크에 연결되지 않았거나 방에 없습니다. 스폰을 중단합니다.");
            yield break;
        }
        
        // 스폰 포인트 재확인
        if (p1SpawnPoint == null || p2SpawnPoint == null)
        {
            FindSpawnPoints();
        }
        
        SpawnPlayers();
    }
    
    /// <summary>
    /// 플레이어들을 스폰합니다
    /// </summary>
    private void SpawnPlayers()
    {
        Debug.Log("=== SpawnPlayers 호출됨 ===");
        
        if (hasSpawned)
        {
            Debug.LogWarning("이미 플레이어가 스폰되었습니다.");
            return;
        }
        
        // 프리팹 존재 확인
        if (!CheckPrefabsExist())
        {
            Debug.LogError("필요한 프리팹이 존재하지 않습니다. 스폰을 중단합니다.");
            return;
        }
        
        // 방장 여부 재확인
        isRoomMaster = PhotonNetwork.IsMasterClient;
        Debug.Log($"스폰 시 방장 여부: {isRoomMaster}");
        
        if (isRoomMaster)
        {
            SpawnAsRoomMaster();
        }
        else
        {
            SpawnAsParticipant();
        }
        
        hasSpawned = true;
    }
    
    /// <summary>
    /// 프리팹 존재 확인
    /// </summary>
    private bool CheckPrefabsExist()
    {
        GameObject localPrefab = Resources.Load<GameObject>(localPlayerPrefabPath);
        
        if (localPrefab == null)
        {
            Debug.LogError($"로컬 플레이어 프리팹을 찾을 수 없습니다: Resources/{localPlayerPrefabPath}");
            Debug.LogError("경로를 확인하세요: Assets/Resources/Player_Origin.prefab");
            return false;
        }
        
        // PhotonView 확인
        PhotonView localPV = localPrefab.GetComponent<PhotonView>();
        
        if (localPV == null)
        {
            Debug.LogError($"로컬 플레이어 프리팹에 PhotonView가 없습니다: {localPlayerPrefabPath}");
            return false;
        }
        
        Debug.Log($"✅ 프리팹 확인 완료: {localPrefab.name} (PhotonView 있음)");
        return true;
    }
    
    /// <summary>
    /// 방장으로서 플레이어 스폰
    /// 방장: p1 위치에 로컬 플레이어
    /// </summary>
    private void SpawnAsRoomMaster()
    {
        Debug.Log("=== 방장으로서 플레이어를 스폰합니다 ===");
        
        // p1 위치에 로컬 플레이어 스폰
        SpawnLocalPlayer(p1SpawnPoint);
    }
    
    /// <summary>
    /// 참가자로서 플레이어 스폰
    /// 참가자: p2 위치에 로컬 플레이어
    /// </summary>
    private void SpawnAsParticipant()
    {
        Debug.Log("=== 참가자로서 플레이어를 스폰합니다 ===");
        
        // p2 위치에 로컬 플레이어 스폰
        SpawnLocalPlayer(p2SpawnPoint);
    }
    
    /// <summary>
    /// 로컬 플레이어를 스폰합니다 (Player_Origin.prefab)
    /// </summary>
    private void SpawnLocalPlayer(Transform spawnPoint)
    {
        Debug.Log($"=== SpawnLocalPlayer 호출됨 - 위치: {spawnPoint.name} ===");
        
        if (localPlayerInstance != null)
        {
            Debug.LogWarning("로컬 플레이어가 이미 스폰되어 있습니다.");
            return;
        }
        
        // 로컬 플레이어 프리팹 로드 확인
        GameObject localPrefab = Resources.Load<GameObject>(localPlayerPrefabPath);
        if (localPrefab == null)
        {
            Debug.LogError($"로컬 플레이어 프리팹을 찾을 수 없습니다: {localPlayerPrefabPath}");
            Debug.LogError("확인할 경로: Assets/Resources/Player_Origin.prefab");
            return;
        }
        
        Debug.Log($"프리팹 로드 성공: {localPrefab.name}");
        Debug.Log($"스폰 위치: {spawnPoint.position}");
        Debug.Log($"스폰 회전: {spawnPoint.rotation}");
        
        try
        {
            // 네트워크를 통해 로컬 플레이어 스폰
            localPlayerInstance = PhotonNetwork.Instantiate(
                localPlayerPrefabPath,
                spawnPoint.position,
                spawnPoint.rotation
            );
            
            Debug.Log($"✅ 로컬 플레이어 스폰 성공: {localPlayerInstance.name}");
            
            // 로컬 플레이어 설정
            SetupLocalPlayer(localPlayerInstance);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 플레이어 스폰 실패: {e.Message}");
            Debug.LogError($"스택 트레이스: {e.StackTrace}");
            
            // 일반적인 스폰 실패 원인들
            Debug.LogError("가능한 원인들:");
            Debug.LogError("1. 프리팹에 PhotonView가 없음");
            Debug.LogError("2. 프리팹이 Resources 폴더에 없음");
            Debug.LogError("3. 네트워크 연결 문제");
            Debug.LogError("4. 방에 입장하지 않은 상태");
        }
    }
    
    /// <summary>
    /// 로컬 플레이어 설정
    /// </summary>
    private void SetupLocalPlayer(GameObject player)
    {
        Debug.Log($"=== SetupLocalPlayer 호출됨: {player.name} ===");
        
        PhotonView pv = player.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine)
        {
            Debug.Log($"PhotonView 확인됨 - IsMine: {pv.IsMine}, ViewID: {pv.ViewID}");
            
            // PlayerSetup 컴포넌트 설정
            PlayerSetup playerSetup = player.GetComponent<PlayerSetup>();
            if (playerSetup != null)
            {
                playerSetup.SetAsLocalPlayer(true);
                Debug.Log("PlayerSetup 설정 완료");
            }
            else
            {
                Debug.LogWarning("PlayerSetup 컴포넌트를 찾을 수 없습니다.");
            }
            
            Debug.Log("✅ 로컬 플레이어 설정 완료");
        }
        else
        {
            Debug.LogError($"PhotonView 문제 - PV: {pv}, IsMine: {(pv != null ? pv.IsMine.ToString() : "null")}");
        }
    }
    
    /// <summary>
    /// 다른 플레이어가 방에 입장했을 때
    /// </summary>
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"=== 플레이어 입장 ===");
        Debug.Log($"플레이어: {newPlayer.NickName} (ID: {newPlayer.ActorNumber})");
        Debug.Log($"현재 방 인원: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
        
        // 원격 플레이어 프리팹은 자동으로 네트워크 동기화를 통해 생성됨
        // 별도의 스폰 로직이 필요하지 않음
    }
    
    /// <summary>
    /// 플레이어가 방을 나갔을 때
    /// </summary>
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"플레이어가 나갔습니다: {otherPlayer.NickName} (ID: {otherPlayer.ActorNumber})");
        
        // 필요시 정리 로직 구현
    }
    
    /// <summary>
    /// 방장이 바뀌었을 때
    /// </summary>
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"방장이 바뀌었습니다: {newMasterClient.NickName}");
        
        // 방장 여부 업데이트
        isRoomMaster = PhotonNetwork.IsMasterClient;
    }
    
    /// <summary>
    /// 방을 나갈 때 정리
    /// </summary>
    public override void OnLeftRoom()
    {
        Debug.Log("=== OnLeftRoom 호출됨 ===");
        
        // 스폰된 플레이어 인스턴스들 정리
        if (localPlayerInstance != null)
        {
            PhotonView pv = localPlayerInstance.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                PhotonNetwork.Destroy(localPlayerInstance);
                Debug.Log("로컬 플레이어 인스턴스 삭제됨");
            }
            localPlayerInstance = null;
        }
        
        // 스폰 상태 초기화
        hasSpawned = false;
        
        Debug.Log("방을 나가면서 플레이어 인스턴스들을 정리했습니다.");
    }
    
    /// <summary>
    /// 수동으로 플레이어 재스폰 (디버깅용)
    /// </summary>
    [ContextMenu("Respawn Players")]
    public void RespawnPlayers()
    {
        Debug.Log("=== 수동 재스폰 시작 ===");
        
        // 기존 플레이어 정리
        if (localPlayerInstance != null)
        {
            PhotonView pv = localPlayerInstance.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                PhotonNetwork.Destroy(localPlayerInstance);
            }
            localPlayerInstance = null;
        }
        
        // 스폰 상태 초기화
        hasSpawned = false;
        
        // 재스폰
        if (PhotonNetwork.InRoom)
        {
            StartCoroutine(SpawnPlayersWithDelay(0.5f));
        }
        else
        {
            Debug.LogError("방에 입장하지 않은 상태에서는 재스폰할 수 없습니다.");
        }
    }
    
    /// <summary>
    /// 디버깅용 - 현재 상태 출력
    /// </summary>
    [ContextMenu("Print Spawn Manager Status")]
    public void PrintStatus()
    {
        Debug.Log("=== PlayerSpawnManager 상태 ===");
        Debug.Log($"- PhotonNetwork 연결: {PhotonNetwork.IsConnected}");
        Debug.Log($"- 방 입장 상태: {PhotonNetwork.InRoom}");
        Debug.Log($"- 현재 방: {(PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : "없음")}");
        Debug.Log($"- 방장 여부: {isRoomMaster}");
        Debug.Log($"- 스폰 완료: {hasSpawned}");
        Debug.Log($"- 로컬 플레이어: {(localPlayerInstance != null ? localPlayerInstance.name : "없음")}");
        Debug.Log($"- p1 위치: {(p1SpawnPoint != null ? p1SpawnPoint.position.ToString() : "없음")}");
        Debug.Log($"- p2 위치: {(p2SpawnPoint != null ? p2SpawnPoint.position.ToString() : "없음")}");
        
        // 프리팹 경로 확인
        GameObject testPrefab = Resources.Load<GameObject>(localPlayerPrefabPath);
        Debug.Log($"- 프리팹 로드 테스트: {(testPrefab != null ? "성공" : "실패")}");
        if (testPrefab != null)
        {
            PhotonView pv = testPrefab.GetComponent<PhotonView>();
            Debug.Log($"- PhotonView 존재: {(pv != null ? "있음" : "없음")}");
        }
    }
} 