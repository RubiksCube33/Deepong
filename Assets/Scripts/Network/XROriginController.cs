using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Photon.Pun;

/// <summary>
/// XR Origin의 입력 제어 및 네트워크 소유권 관리
/// 로컬 플레이어만 입력을 받고, 원격 플레이어는 네트워크 동기화만 수행
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class XROriginController : MonoBehaviourPunCallbacks
{
    [Header("XR 컴포넌트 참조")]
    [SerializeField] private MonoBehaviour xrOriginComponent; // XROrigin 컴포넌트
    [SerializeField] private CharacterController characterController;
    [SerializeField] private MonoBehaviour inputActionManager; // InputActionManager 또는 유사한 컴포넌트
    
    [Header("입력 제어 설정")]
    [SerializeField] private bool isLocalPlayerControlled = false;
    
    // XR 컴포넌트들의 원래 상태 저장
    private bool originalCharacterControllerState;
    private bool originalInputActionManagerState;
    
    void Awake()
    {
        // 자동으로 컴포넌트 찾기
        if (xrOriginComponent == null)
        {
            // XROrigin 타입 찾기
            var components = GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp.GetType().Name.Contains("XROrigin") || comp.GetType().Name.Contains("Origin"))
                {
                    xrOriginComponent = comp;
                    break;
                }
            }
        }
            
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
            
        if (inputActionManager == null)
        {
            // InputActionManager를 찾거나, 유사한 컴포넌트 찾기
            var components = GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp.GetType().Name.Contains("InputAction") || comp.GetType().Name.Contains("ActionManager"))
                {
                    inputActionManager = comp;
                    break;
                }
            }
        }
        
        // 원래 상태 저장
        if (characterController != null)
            originalCharacterControllerState = characterController.enabled;
            
        if (inputActionManager != null)
            originalInputActionManagerState = inputActionManager.enabled;
    }
    
    void Start()
    {
        // PhotonView의 소유권에 따라 입력 제어 설정
        SetInputControl(photonView.IsMine);
    }
    
    /// <summary>
    /// 입력 제어를 설정합니다.
    /// </summary>
    /// <param name="enableInput">입력을 활성화할지 여부</param>
    public void SetInputControl(bool enableInput)
    {
        isLocalPlayerControlled = enableInput;
        
        // 로컬 플레이어만 입력 활성화
        if (characterController != null)
        {
            characterController.enabled = enableInput;
        }
        
        if (inputActionManager != null)
        {
            inputActionManager.enabled = enableInput;
        }
        
        // XR 컨트롤러들 찾아서 설정
        SetXRControllersInput(enableInput);
        
        Debug.Log($"XROriginController: {gameObject.name}의 입력 제어 = {enableInput}");
    }
    
    /// <summary>
    /// XR 컨트롤러들의 입력을 설정합니다.
    /// </summary>
    void SetXRControllersInput(bool enableInput)
    {
        // XR 컨트롤러들 찾기
        var leftController = FindXRController("LeftHand");
        var rightController = FindXRController("RightHand");
        
        if (leftController != null)
        {
            SetControllerInput(leftController, enableInput);
        }
        
        if (rightController != null)
        {
            SetControllerInput(rightController, enableInput);
        }
    }
    
    /// <summary>
    /// 특정 XR 컨트롤러의 입력을 설정합니다.
    /// </summary>
    void SetControllerInput(GameObject controller, bool enableInput)
    {
        // XR Controller 컴포넌트들 비활성화/활성화 (타입 이름으로 찾기)
        var allComponents = controller.GetComponents<MonoBehaviour>();
        foreach (var comp in allComponents)
        {
            string typeName = comp.GetType().Name;
            if (typeName.Contains("ActionBasedController") || 
                typeName.Contains("XRController") || 
                typeName.Contains("Interactor"))
            {
                comp.enabled = enableInput;
            }
        }
    }
    
    /// <summary>
    /// 이름으로 XR 컨트롤러를 찾습니다.
    /// </summary>
    GameObject FindXRController(string controllerName)
    {
        // Camera Offset 찾기
        Transform cameraOffset = transform.Find("Camera Offset");
        if (cameraOffset == null)
            return null;
        
        // 컨트롤러 찾기 (여러 가능한 이름 시도)
        string[] possibleNames = {
            controllerName + " Controller",
            controllerName.Replace("Hand", "") + " Controller", 
            controllerName + "Anchor",
            controllerName + "Hand Controller",
            controllerName.Replace("Hand", "Hand") + " Controller"
        };
        
        foreach (string name in possibleNames)
        {
            Transform controller = cameraOffset.Find(name);
            if (controller != null)
            {
                Debug.Log($"XROriginController: {controllerName} 컨트롤러를 찾았습니다: {controller.name}");
                return controller.gameObject;
            }
        }
        
        Debug.LogWarning($"XROriginController: {controllerName} 컨트롤러를 찾을 수 없습니다.");
        return null;
    }
    
    /// <summary>
    /// 소유권이 변경되었을 때 호출
    /// </summary>
    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // PhotonView 소유권 변경 시 입력 제어 재설정
        if (targetPlayer == PhotonNetwork.LocalPlayer)
        {
            SetInputControl(photonView.IsMine);
        }
    }
    
    /// <summary>
    /// 현재 로컬 플레이어가 제어하는지 확인
    /// </summary>
    public bool IsLocalPlayerControlled()
    {
        return isLocalPlayerControlled;
    }
    
    /// <summary>
    /// 강제로 입력을 비활성화 (디버그용)
    /// </summary>
    [ContextMenu("Disable Input")]
    public void DisableInput()
    {
        SetInputControl(false);
    }
    
    /// <summary>
    /// 강제로 입력을 활성화 (디버그용)
    /// </summary>
    [ContextMenu("Enable Input")]
    public void EnableInput()
    {
        SetInputControl(true);
    }
    
    /// <summary>
    /// 원래 상태로 복원 (디버그용)
    /// </summary>
    [ContextMenu("Restore Original State")]
    public void RestoreOriginalState()
    {
        if (characterController != null)
            characterController.enabled = originalCharacterControllerState;
            
        if (inputActionManager != null)
            inputActionManager.enabled = originalInputActionManagerState;
    }
} 