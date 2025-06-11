using UnityEngine;
using Photon.Pun;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Player_Origin.prefab에 네트워크 동기화에 필요한 컴포넌트들을 자동으로 설정합니다.
/// 에디터에서 실행하여 프리팹을 네트워크 플레이어로 준비시킵니다.
/// </summary>
public class PlayerPrefabSetup : MonoBehaviour
{
    [Header("자동 설정")]
    [SerializeField] private bool autoSetupOnAwake = false; // Awake에서 자동 설정 여부
    
    [Header("컴포넌트 확인")]
    [SerializeField] private bool hasPhotonView = false;
    [SerializeField] private bool hasPlayerSetup = false;
    [SerializeField] private bool hasPlayerNetworkSync = false;
    [SerializeField] private bool hasPlayerAnimationSync = false;
    [SerializeField] private bool hasVRMovementController = false;
    
    void Awake()
    {
        if (autoSetupOnAwake)
        {
            SetupPlayerPrefab();
        }
        
        CheckComponents();
    }
    
    /// <summary>
    /// 플레이어 프리팹에 필요한 컴포넌트들을 설정합니다.
    /// </summary>
    [ContextMenu("Setup Player Prefab")]
    public void SetupPlayerPrefab()
    {
        // PhotonView 컴포넌트 추가/설정
        SetupPhotonView();
        
        // PlayerSetup 컴포넌트 추가/설정
        SetupPlayerSetup();
        
        // PlayerNetworkSync 컴포넌트 추가/설정
        SetupPlayerNetworkSync();
        
        // PlayerAnimationSync 컴포넌트 추가/설정
        SetupPlayerAnimationSync();
        
        // VRMovementController 컴포넌트 추가/설정
        SetupVRMovementController();
        
        // 컴포넌트 상태 업데이트
        CheckComponents();
        
        Debug.Log($"플레이어 프리팹 설정 완료: {gameObject.name}");
    }
    
    /// <summary>
    /// PhotonView 컴포넌트 설정
    /// </summary>
    void SetupPhotonView()
    {
        PhotonView photonView = GetComponent<PhotonView>();
        if (photonView == null)
        {
            photonView = gameObject.AddComponent<PhotonView>();
            Debug.Log("PhotonView 컴포넌트 추가됨");
        }
        
        // PhotonView 설정
        photonView.Synchronization = ViewSynchronization.UnreliableOnChange;
        photonView.OwnershipTransfer = OwnershipOption.Fixed;
        
        // 관찰할 컴포넌트들 설정
        var observedComponents = new System.Collections.Generic.List<Component>();
        
        // PlayerNetworkSync 추가
        PlayerNetworkSync networkSync = GetComponent<PlayerNetworkSync>();
        if (networkSync != null)
        {
            observedComponents.Add(networkSync);
        }
        
        // PlayerAnimationSync 추가
        PlayerAnimationSync animSync = GetComponent<PlayerAnimationSync>();
        if (animSync != null)
        {
            observedComponents.Add(animSync);
        }
        
        // ObservedComponents 설정
        photonView.ObservedComponents.Clear();
        photonView.ObservedComponents.AddRange(observedComponents);
        
        Debug.Log($"PhotonView 설정 완료 - 관찰 컴포넌트 수: {observedComponents.Count}");
    }
    
    /// <summary>
    /// PlayerSetup 컴포넌트 설정
    /// </summary>
    void SetupPlayerSetup()
    {
        PlayerSetup playerSetup = GetComponent<PlayerSetup>();
        if (playerSetup == null)
        {
            playerSetup = gameObject.AddComponent<PlayerSetup>();
            Debug.Log("PlayerSetup 컴포넌트 추가됨");
        }
        
        // 기본 설정값들은 PlayerSetup 스크립트에서 처리
    }
    
    /// <summary>
    /// PlayerNetworkSync 컴포넌트 설정
    /// </summary>
    void SetupPlayerNetworkSync()
    {
        PlayerNetworkSync networkSync = GetComponent<PlayerNetworkSync>();
        if (networkSync == null)
        {
            networkSync = gameObject.AddComponent<PlayerNetworkSync>();
            Debug.Log("PlayerNetworkSync 컴포넌트 추가됨");
        }
        
        // VR 컨트롤러 참조들은 런타임에 자동으로 찾도록 설정되어 있음
    }
    
    /// <summary>
    /// PlayerAnimationSync 컴포넌트 설정
    /// </summary>
    void SetupPlayerAnimationSync()
    {
        // Animator가 있는 경우에만 추가
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            PlayerAnimationSync animSync = GetComponent<PlayerAnimationSync>();
            if (animSync == null)
            {
                animSync = gameObject.AddComponent<PlayerAnimationSync>();
                Debug.Log("PlayerAnimationSync 컴포넌트 추가됨");
            }
        }
        else
        {
            Debug.LogWarning("Animator가 없어서 PlayerAnimationSync를 추가하지 않았습니다.");
        }
    }
    
    /// <summary>
    /// VRMovementController 컴포넌트 설정
    /// </summary>
    void SetupVRMovementController()
    {
        VRMovementController vRMovementController = GetComponent<VRMovementController>();
        if (vRMovementController == null)
        {
            vRMovementController = gameObject.AddComponent<VRMovementController>();
            Debug.Log("VRMovementController 컴포넌트 추가됨");
        }
    }
    
    /// <summary>
    /// 현재 컴포넌트 상태 확인
    /// </summary>
    [ContextMenu("Check Components")]
    public void CheckComponents()
    {
        hasPhotonView = GetComponent<PhotonView>() != null;
        hasPlayerSetup = GetComponent<PlayerSetup>() != null;
        hasPlayerNetworkSync = GetComponent<PlayerNetworkSync>() != null;
        hasPlayerAnimationSync = GetComponent<PlayerAnimationSync>() != null;
        hasVRMovementController = GetComponent<VRMovementController>() != null;
        
        Debug.Log($"컴포넌트 상태 - PhotonView: {hasPhotonView}, PlayerSetup: {hasPlayerSetup}, " +
                 $"NetworkSync: {hasPlayerNetworkSync}, AnimationSync: {hasPlayerAnimationSync}, VRMovementController: {hasVRMovementController}");
    }
    
    /// <summary>
    /// 네트워크 준비 상태 확인
    /// </summary>
    public bool IsNetworkReady()
    {
        return hasPhotonView && hasPlayerSetup && hasPlayerNetworkSync && hasVRMovementController;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 Resources 폴더의 모든 Player_Origin 프리팹을 설정
    /// </summary>
    [MenuItem("Tools/Setup All Player Prefabs")]
    public static void SetupAllPlayerPrefabs()
    {
        string[] prefabPaths = {
            "Assets/Resources/Player_Origin.prefab",
            "Assets/Resources/2P_Player_Origin.prefab"
        };
        
        foreach (string path in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                // 프리팹 인스턴스 생성
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                
                // PlayerPrefabSetup 컴포넌트 추가 (임시)
                PlayerPrefabSetup setup = instance.GetComponent<PlayerPrefabSetup>();
                if (setup == null)
                {
                    setup = instance.AddComponent<PlayerPrefabSetup>();
                }
                
                // 설정 실행
                setup.SetupPlayerPrefab();
                
                // 프리팹에 변경사항 적용
                PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
                
                // 임시 인스턴스 삭제
                DestroyImmediate(instance);
                
                Debug.Log($"프리팹 설정 완료: {path}");
            }
            else
            {
                Debug.LogWarning($"프리팹을 찾을 수 없습니다: {path}");
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("모든 플레이어 프리팹 설정 완료!");
    }
    
    /// <summary>
    /// 현재 선택된 프리팹 설정
    /// </summary>
    [MenuItem("Tools/Setup Selected Player Prefab")]
    public static void SetupSelectedPlayerPrefab()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("프리팹을 선택해주세요.");
            return;
        }
        
        PlayerPrefabSetup setup = selected.GetComponent<PlayerPrefabSetup>();
        if (setup == null)
        {
            setup = selected.AddComponent<PlayerPrefabSetup>();
        }
        
        setup.SetupPlayerPrefab();
        
        Debug.Log($"선택된 프리팹 설정 완료: {selected.name}");
    }
#endif
} 