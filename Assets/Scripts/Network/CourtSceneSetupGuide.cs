using UnityEngine;
using Photon.Pun;

/// <summary>
/// CourtScene에서 네트워크 플레이어 동기화 설정 가이드
/// 이 스크립트를 GameManager에 추가하여 자동으로 네트워크 플레이어 스폰을 설정할 수 있습니다.
/// </summary>
public class CourtSceneSetupGuide : MonoBehaviourPunCallbacks
{
    [Header("설정 상태")]
    [SerializeField] private bool isSetupComplete = false;
    [SerializeField] private bool hasNetworkSpawner = false;
    [SerializeField] private bool hasSpawnPoints = false;
    [SerializeField] private bool hasPrefabReady = false;
    
    [Header("자동 설정")]
    [SerializeField] private bool autoSetupOnStart = true;
    
    void Start()
    {
        if (autoSetupOnStart)
        {
            CheckAndSetupCourtScene();
        }
    }
    
    /// <summary>
    /// CourtScene 설정 확인 및 자동 설정
    /// </summary>
    [ContextMenu("Check and Setup Court Scene")]
    public void CheckAndSetupCourtScene()
    {
        Debug.Log("=== CourtScene 네트워크 설정 확인 시작 ===");
        
        // 1. 네트워크 연결 확인
        CheckNetworkConnection();
        
        // 2. NetworkPlayerSpawner 확인/추가
        CheckNetworkSpawner();
        
        // 3. 스폰 포인트 확인
        CheckSpawnPoints();
        
        // 4. 프리팹 준비 상태 확인
        CheckPrefabReady();
        
        // 5. 전체 설정 상태 업데이트
        UpdateSetupStatus();
        
        Debug.Log("=== CourtScene 네트워크 설정 확인 완료 ===");
    }
    
    /// <summary>
    /// 네트워크 연결 상태 확인
    /// </summary>
    void CheckNetworkConnection()
    {
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log($"✓ 네트워크 연결됨 - 방: {PhotonNetwork.CurrentRoom?.Name}, 플레이어 수: {PhotonNetwork.CurrentRoom?.PlayerCount}");
        }
        else
        {
            Debug.LogWarning("✗ 네트워크에 연결되지 않았습니다!");
        }
    }
    
    /// <summary>
    /// NetworkPlayerSpawner 확인 및 추가
    /// </summary>
    void CheckNetworkSpawner()
    {
        NetworkPlayerSpawner spawner = FindObjectOfType<NetworkPlayerSpawner>();
        
        if (spawner == null)
        {
            // GameManager에 NetworkPlayerSpawner 추가
            GameObject gameManager = GameObject.Find("GameManager");
            if (gameManager != null)
            {
                spawner = gameManager.AddComponent<NetworkPlayerSpawner>();
                Debug.Log("✓ NetworkPlayerSpawner가 GameManager에 추가되었습니다.");
            }
            else
            {
                // 새로운 GameObject 생성
                GameObject spawnerObj = new GameObject("NetworkPlayerSpawner");
                spawner = spawnerObj.AddComponent<NetworkPlayerSpawner>();
                Debug.Log("✓ NetworkPlayerSpawner 오브젝트가 생성되었습니다.");
            }
            hasNetworkSpawner = true;
        }
        else
        {
            Debug.Log("✓ NetworkPlayerSpawner가 이미 존재합니다.");
            hasNetworkSpawner = true;
        }
    }
    
    /// <summary>
    /// 스폰 포인트 확인
    /// </summary>
    void CheckSpawnPoints()
    {
        GameObject p1 = GameObject.Find("p1");
        GameObject p2 = GameObject.Find("p2");
        
        if (p1 != null && p2 != null)
        {
            Debug.Log($"✓ 스폰 포인트 확인됨 - p1: {p1.transform.position}, p2: {p2.transform.position}");
            hasSpawnPoints = true;
        }
        else
        {
            Debug.LogWarning("✗ 스폰 포인트(p1, p2)를 찾을 수 없습니다. NetworkPlayerSpawner가 기본 위치를 생성합니다.");
            hasSpawnPoints = false;
        }
    }
    
    /// <summary>
    /// 프리팹 준비 상태 확인
    /// </summary>
    void CheckPrefabReady()
    {
        GameObject prefab = Resources.Load<GameObject>("Player_Origin");
        
        if (prefab != null)
        {
            PhotonView photonView = prefab.GetComponent<PhotonView>();
            PlayerNetworkSync networkSync = prefab.GetComponent<PlayerNetworkSync>();
            PlayerSetup playerSetup = prefab.GetComponent<PlayerSetup>();
            
            if (photonView != null && networkSync != null && playerSetup != null)
            {
                Debug.Log("✓ Player_Origin.prefab이 네트워크 동기화 준비 완료되었습니다.");
                hasPrefabReady = true;
            }
            else
            {
                Debug.LogWarning("✗ Player_Origin.prefab에 필요한 컴포넌트가 없습니다. PlayerPrefabSetup을 실행하세요.");
                Debug.LogWarning($"  - PhotonView: {(photonView != null ? "✓" : "✗")}");
                Debug.LogWarning($"  - PlayerNetworkSync: {(networkSync != null ? "✓" : "✗")}");
                Debug.LogWarning($"  - PlayerSetup: {(playerSetup != null ? "✓" : "✗")}");
                hasPrefabReady = false;
            }
        }
        else
        {
            Debug.LogError("✗ Player_Origin.prefab을 Resources 폴더에서 찾을 수 없습니다!");
            hasPrefabReady = false;
        }
    }
    
    /// <summary>
    /// 전체 설정 상태 업데이트
    /// </summary>
    void UpdateSetupStatus()
    {
        isSetupComplete = PhotonNetwork.IsConnected && hasNetworkSpawner && hasPrefabReady;
        
        if (isSetupComplete)
        {
            Debug.Log("🎉 CourtScene 네트워크 설정이 완료되었습니다! 게임을 시작할 수 있습니다.");
        }
        else
        {
            Debug.LogWarning("⚠️ CourtScene 설정이 완료되지 않았습니다. 위의 문제들을 해결해주세요.");
        }
    }
    
    /// <summary>
    /// 설정 가이드 출력
    /// </summary>
    [ContextMenu("Show Setup Guide")]
    public void ShowSetupGuide()
    {
        Debug.Log("=== CourtScene 네트워크 설정 가이드 ===");
        Debug.Log("1. Player_Origin.prefab 설정:");
        Debug.Log("   - Unity 에디터에서 Tools > Setup All Player Prefabs 실행");
        Debug.Log("   - 또는 Player_Origin.prefab을 선택하고 Tools > Setup Selected Player Prefab 실행");
        Debug.Log("");
        Debug.Log("2. CourtScene 설정:");
        Debug.Log("   - GameManager에 CourtSceneSetupGuide 컴포넌트 추가");
        Debug.Log("   - 'Check and Setup Court Scene' 버튼 클릭");
        Debug.Log("");
        Debug.Log("3. 게임 실행:");
        Debug.Log("   - 멀티플레이어 방에 입장");
        Debug.Log("   - CourtScene 로드");
        Debug.Log("   - 자동으로 플레이어 스폰 및 VR 컨트롤러 동기화 시작");
        Debug.Log("=====================================");
    }
    
    /// <summary>
    /// 현재 상태 요약 출력
    /// </summary>
    [ContextMenu("Show Status Summary")]
    public void ShowStatusSummary()
    {
        Debug.Log("=== CourtScene 설정 상태 요약 ===");
        Debug.Log($"네트워크 연결: {(PhotonNetwork.IsConnected ? "✓" : "✗")}");
        Debug.Log($"NetworkPlayerSpawner: {(hasNetworkSpawner ? "✓" : "✗")}");
        Debug.Log($"스폰 포인트: {(hasSpawnPoints ? "✓" : "✗")}");
        Debug.Log($"프리팹 준비: {(hasPrefabReady ? "✓" : "✗")}");
        Debug.Log($"전체 설정 완료: {(isSetupComplete ? "✓" : "✗")}");
        Debug.Log("===============================");
    }
} 