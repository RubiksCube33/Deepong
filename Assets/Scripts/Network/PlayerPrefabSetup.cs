using UnityEngine;
using Photon.Pun;
using DeepongVR.Court;

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
    [SerializeField] private bool hasPaddleChangeController = false;
    
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
        
        // PaddleChangeController 설정 확인
        SetupPaddleChangeController();
        
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
        
        // VRControllerNetworkSync 추가 (VR 폴더에서 찾기)
        var vrControllerSync = GetComponent<DeepongVR.Network.VRControllerNetworkSync>();
        if (vrControllerSync != null)
        {
            observedComponents.Add(vrControllerSync);
        }
        
        // PaddleChangeController는 RPC로 동기화하므로 ObservedComponents에 추가하지 않음
        PaddleChangeController paddleController = GetComponentInChildren<PaddleChangeController>();
        if (paddleController != null)
        {
            Debug.Log($"PaddleChangeController 발견: {paddleController.gameObject.name} (RPC 방식으로 동기화)");
        }
        else
        {
            Debug.LogWarning("PaddleChangeController를 찾을 수 없습니다. 패들 변경 동기화가 작동하지 않을 수 있습니다.");
        }
        
        // ObservedComponents 설정
        photonView.ObservedComponents.Clear();
        photonView.ObservedComponents.AddRange(observedComponents);
        
        Debug.Log($"PhotonView 설정 완료 - 관찰 컴포넌트 수: {observedComponents.Count}");
        
        // 각 컴포넌트 이름 출력
        for (int i = 0; i < observedComponents.Count; i++)
        {
            Debug.Log($"  {i + 1}. {observedComponents[i].GetType().Name} ({observedComponents[i].gameObject.name})");
        }
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
        
        // PlayerNetworkSync의 기본 설정값들은 스크립트에서 자동으로 처리
    }
    
    /// <summary>
    /// PlayerAnimationSync 컴포넌트 설정
    /// </summary>
    void SetupPlayerAnimationSync()
    {
        PlayerAnimationSync animSync = GetComponent<PlayerAnimationSync>();
        if (animSync == null)
        {
            animSync = gameObject.AddComponent<PlayerAnimationSync>();
            Debug.Log("PlayerAnimationSync 컴포넌트 추가됨");
        }
        
        // 기본 애니메이션 파라미터 설정은 스크립트에서 처리
    }
    
    /// <summary>
    /// VRMovementController 컴포넌트 설정
    /// </summary>
    void SetupVRMovementController()
    {
        VRMovementController vrMovement = GetComponent<VRMovementController>();
        if (vrMovement == null)
        {
            vrMovement = gameObject.AddComponent<VRMovementController>();
            Debug.Log("VRMovementController 컴포넌트 추가됨");
        }
        
        // VRMovementController의 기본 설정값들은 스크립트에서 처리
    }
    
    /// <summary>
    /// PaddleChangeController 설정 확인 및 네트워크 동기화 활성화
    /// </summary>
    void SetupPaddleChangeController()
    {
        // 자식 오브젝트들에서 PaddleChangeController 찾기
        PaddleChangeController[] paddleControllers = GetComponentsInChildren<PaddleChangeController>();
        
        if (paddleControllers.Length > 0)
        {
            Debug.Log($"PaddleChangeController {paddleControllers.Length}개 발견");
            
            foreach (var controller in paddleControllers)
            {
                // 네트워크 동기화 활성화 (Inspector에서 설정된 값 확인)
                string controllerType = controller.name.Contains("Right") ? "우측" : "좌측";
                Debug.Log($"{controllerType} PaddleChangeController 확인: {controller.gameObject.name}");
                
                // 네트워크 동기화가 비활성화되어 있다면 경고
                var enableNetworkSyncField = controller.GetType().GetField("enableNetworkSync", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (enableNetworkSyncField != null)
                {
                    bool isNetworkSyncEnabled = (bool)enableNetworkSyncField.GetValue(controller);
                    if (!isNetworkSyncEnabled)
                    {
                        Debug.LogWarning($"{controllerType} PaddleChangeController의 네트워크 동기화가 비활성화되어 있습니다.");
                    }
                    else
                    {
                        Debug.Log($"{controllerType} PaddleChangeController 네트워크 동기화 활성화 확인됨");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("PaddleChangeController를 찾을 수 없습니다. 패들 변경 동기화가 작동하지 않을 수 있습니다.");
        }
    }
    
    /// <summary>
    /// 컴포넌트 상태 확인
    /// </summary>
    void CheckComponents()
    {
        hasPhotonView = GetComponent<PhotonView>() != null;
        hasPlayerSetup = GetComponent<PlayerSetup>() != null;
        hasPlayerNetworkSync = GetComponent<PlayerNetworkSync>() != null;
        hasPlayerAnimationSync = GetComponent<PlayerAnimationSync>() != null;
        hasVRMovementController = GetComponent<VRMovementController>() != null;
        hasPaddleChangeController = GetComponentInChildren<PaddleChangeController>() != null;
        
        Debug.Log($"컴포넌트 상태 - PhotonView: {hasPhotonView}, PlayerSetup: {hasPlayerSetup}, " +
                 $"NetworkSync: {hasPlayerNetworkSync}, AnimationSync: {hasPlayerAnimationSync}, " +
                 $"VRMovementController: {hasVRMovementController}, PaddleChangeController: {hasPaddleChangeController}");
    }
    
    /// <summary>
    /// 네트워크 준비 상태 확인
    /// </summary>
    public bool IsNetworkReady()
    {
        return hasPhotonView && hasPlayerSetup && hasPlayerNetworkSync && hasVRMovementController;
    }
    
    /// <summary>
    /// 패들 동기화 준비 상태 확인
    /// </summary>
    public bool IsPaddleSyncReady()
    {
        return hasPaddleChangeController && hasPhotonView;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터 전용: 모든 Player_Origin 프리팹 설정
    /// </summary>
    [MenuItem("Tools/Setup All Player Prefabs")]
    public static void SetupAllPlayerPrefabs()
    {
        string[] prefabGUIDs = AssetDatabase.FindAssets("Player_Origin t:Prefab");
        
        foreach (string guid in prefabGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null)
            {
                PlayerPrefabSetup setup = prefab.GetComponent<PlayerPrefabSetup>();
                if (setup != null)
                {
                    setup.SetupPlayerPrefab();
                    EditorUtility.SetDirty(prefab);
                    Debug.Log($"프리팹 설정 완료: {path}");
                }
                else
                {
                    Debug.LogWarning($"PlayerPrefabSetup 컴포넌트가 없습니다: {path}");
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log("모든 Player_Origin 프리팹 설정 완료!");
    }
    
    /// <summary>
    /// 에디터 전용: 선택된 프리팹만 설정
    /// </summary>
    [MenuItem("Tools/Setup Selected Player Prefab")]
    public static void SetupSelectedPlayerPrefab()
    {
        GameObject selectedObject = Selection.activeGameObject;
        
        if (selectedObject != null)
        {
            PlayerPrefabSetup setup = selectedObject.GetComponent<PlayerPrefabSetup>();
            if (setup != null)
            {
                setup.SetupPlayerPrefab();
                EditorUtility.SetDirty(selectedObject);
                Debug.Log($"선택된 프리팹 설정 완료: {selectedObject.name}");
            }
            else
            {
                Debug.LogError("선택된 오브젝트에 PlayerPrefabSetup 컴포넌트가 없습니다!");
            }
        }
        else
        {
            Debug.LogError("프리팹을 선택해주세요!");
        }
    }
    
    /// <summary>
    /// 에디터 전용: 패들 컨트롤러 상태 확인
    /// </summary>
    [MenuItem("Tools/Check Paddle Controllers")]
    public static void CheckPaddleControllers()
    {
        GameObject selectedObject = Selection.activeGameObject;
        
        if (selectedObject != null)
        {
            PaddleChangeController[] controllers = selectedObject.GetComponentsInChildren<PaddleChangeController>();
            
            Debug.Log($"=== 패들 컨트롤러 상태 확인: {selectedObject.name} ===");
            Debug.Log($"발견된 PaddleChangeController 수: {controllers.Length}");
            
            for (int i = 0; i < controllers.Length; i++)
            {
                var controller = controllers[i];
                string position = controller.name.Contains("Right") ? "우측" : "좌측";
                Debug.Log($"{i + 1}. {position} 컨트롤러: {controller.gameObject.name}");
                Debug.Log($"   - 현재 패들: {controller.CurrentPaddleName}");
                Debug.Log($"   - PhotonView 연결: {(controller.GetComponentInParent<PhotonView>() != null ? "있음" : "없음")}");
            }
            Debug.Log("=======================================");
        }
        else
        {
            Debug.LogError("프리팹을 선택해주세요!");
        }
    }
#endif
} 