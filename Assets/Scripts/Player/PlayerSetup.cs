using UnityEngine;

using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using Photon.Pun;
using System.Collections;

/// <summary>
/// Player_Origin.prefab의 VR 컴포넌트들을 로컬/원격 플레이어에 따라 적절히 활성화/비활성화하는 스크립트
/// 프리팹에서 이미 대부분의 컴포넌트들이 비활성화되어 있으므로, 로컬 플레이어일 때만 필요한 것들을 활성화합니다.
/// </summary>
public class PlayerSetup : MonoBehaviourPun, IPunObservable
{
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    // 주요 컴포넌트들
    private MonoBehaviour xrOrigin;
    private CharacterController characterController;
    private MonoBehaviour xrInputModalityManager;
    private MonoBehaviour inputActionManager;
    
    // 컨트롤러 관련
    private GameObject leftController;
    private GameObject rightController;
    private Camera mainCamera;
    
    // 다른 스크립트들과의 호환성을 위한 속성들
    public bool IsPlayerOne { get; private set; }
    
    void Start()
    {
        StartCoroutine(DelayedNetworkSetup());
    }
    
    private IEnumerator DelayedNetworkSetup()
    {
        // PhotonView 소유권이 확실히 설정될 때까지 대기
        yield return new WaitForSeconds(0.5f);
        
        // 네트워크 소유권 재확인
        yield return new WaitUntil(() => photonView != null && photonView.Owner != null);
        
        SetupPlayer();
    }
    
    private void SetupPlayer()
    {
        bool isLocalPlayer = photonView.IsMine;
        
        // 플레이어 번호 설정 (액터 번호 기반)
        IsPlayerOne = photonView.Owner.ActorNumber == 1;
        
        if (enableDebugLogs)
        {
            string playerType = isLocalPlayer ? "🟢 로컬" : "🔴 원격";
            Debug.Log($"[PlayerSetup] {playerType} 플레이어 설정 시작 - ActorNumber: {photonView.Owner.ActorNumber}, IsPlayerOne: {IsPlayerOne}");
        }
        
        // 컴포넌트 찾기
        FindComponents();
        
        // 로컬 플레이어와 원격 플레이어 설정
        if (isLocalPlayer)
        {
            SetupLocalPlayer();
        }
        else
        {
            SetupRemotePlayer();
        }
        
        if (enableDebugLogs)
        {
            string playerType = isLocalPlayer ? "🟢 로컬" : "🔴 원격";
            Debug.Log($"[PlayerSetup] {playerType} 플레이어 설정 완료");
        }
    }
    
    private void FindComponents()
    {
        // 루트에서 컴포넌트들 찾기
        characterController = GetComponent<CharacterController>();
        
        // 타입 이름으로 컴포넌트 찾기 (정확한 타입을 모르는 경우)
        var allComponents = GetComponents<MonoBehaviour>();
        foreach (var comp in allComponents)
        {
            string typeName = comp.GetType().Name;
            
            if (typeName.Contains("XROrigin") || typeName.Contains("Origin"))
            {
                xrOrigin = comp;
            }
            else if (typeName.Contains("XRInputModality"))
            {
                xrInputModalityManager = comp;
            }
            else if (typeName.Contains("InputActionManager"))
            {
                inputActionManager = comp;
            }
        }
        
        // 컨트롤러들 찾기
        leftController = transform.Find("Camera Offset/Left Controller")?.gameObject;
        rightController = transform.Find("Camera Offset/Right Controller")?.gameObject;
        
        // 메인 카메라 찾기
        Transform cameraTransform = transform.Find("Camera Offset/Main Camera");
        if (cameraTransform != null)
        {
            mainCamera = cameraTransform.GetComponent<Camera>();
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[PlayerSetup] 컴포넌트 찾기 결과:");
            Debug.Log($"  - XROrigin: {(xrOrigin != null ? "✅" : "❌")} (타입: {xrOrigin?.GetType().Name}) (현재 상태: {(xrOrigin?.enabled == true ? "활성" : "비활성")})");
            Debug.Log($"  - CharacterController: {(characterController != null ? "✅" : "❌")} (현재 상태: {(characterController?.enabled == true ? "활성" : "비활성")})");
            Debug.Log($"  - XRInputModalityManager: {(xrInputModalityManager != null ? "✅" : "❌")} (타입: {xrInputModalityManager?.GetType().Name}) (현재 상태: {(xrInputModalityManager?.enabled == true ? "활성" : "비활성")})");
            Debug.Log($"  - InputActionManager: {(inputActionManager != null ? "✅" : "❌")} (타입: {inputActionManager?.GetType().Name}) (현재 상태: {(inputActionManager?.enabled == true ? "활성" : "비활성")})");
            Debug.Log($"  - Left Controller: {(leftController != null ? "✅" : "❌")} (현재 상태: {(leftController?.activeInHierarchy == true ? "활성" : "비활성")})");
            Debug.Log($"  - Right Controller: {(rightController != null ? "✅" : "❌")} (현재 상태: {(rightController?.activeInHierarchy == true ? "활성" : "비활성")})");
            Debug.Log($"  - Main Camera: {(mainCamera != null ? "✅" : "❌")} (현재 상태: {(mainCamera?.enabled == true ? "활성" : "비활성")})");
        }
    }
    
    private void SetupLocalPlayer()
    {
        if (enableDebugLogs)
        {
            Debug.Log("[PlayerSetup] 🟢 로컬 플레이어 - VR 컴포넌트들을 활성화합니다");
        }
        
        // XR Origin 활성화 (VR 추적을 위해 필수)
        if (xrOrigin != null)
        {
            xrOrigin.enabled = true;
            if (enableDebugLogs) Debug.Log($"[PlayerSetup] ✅ {xrOrigin.GetType().Name} 활성화 (VR 추적 시작)");
        }
        
        // Character Controller 활성화 (VR 이동을 위해 필수)
        if (characterController != null)
        {
            characterController.enabled = true;
            if (enableDebugLogs) Debug.Log("[PlayerSetup] ✅ CharacterController 활성화 (VR 이동 가능)");
        }
        
        // XR Input Modality Manager 활성화 (VR 입력 모드 관리)
        if (xrInputModalityManager != null)
        {
            xrInputModalityManager.enabled = true;
            if (enableDebugLogs) Debug.Log($"[PlayerSetup] ✅ {xrInputModalityManager.GetType().Name} 활성화 (VR 입력 모드 관리)");
        }
        
        // Input Action Manager 활성화 (VR 입력 처리)
        if (inputActionManager != null)
        {
            inputActionManager.enabled = true;
            if (enableDebugLogs) Debug.Log($"[PlayerSetup] ✅ {inputActionManager.GetType().Name} 활성화 (VR 입력 처리)");
        }
        
        // 컨트롤러들 활성화 (VR 컨트롤러 표시 및 상호작용)
        if (leftController != null)
        {
            leftController.SetActive(true);
            EnableControllerInput(leftController);
            if (enableDebugLogs) Debug.Log("[PlayerSetup] ✅ Left Controller 활성화 (VR 왼손 컨트롤러)");
        }
        
        if (rightController != null)
        {
            rightController.SetActive(true);
            EnableControllerInput(rightController);
            if (enableDebugLogs) Debug.Log("[PlayerSetup] ✅ Right Controller 활성화 (VR 오른손 컨트롤러)");
        }
        
        // 메인 카메라 활성화 (VR 시야)
        if (mainCamera != null)
        {
            mainCamera.enabled = true;
            if (enableDebugLogs) Debug.Log("[PlayerSetup] ✅ Main Camera 활성화 (VR 시야)");
        }
        
        // 로컬 플레이어 태그 설정
        gameObject.tag = "Player";
        
        if (enableDebugLogs)
        {
            Debug.Log("[PlayerSetup] 🟢 로컬 플레이어 설정 완료 - 모든 VR 기능이 활성화되었습니다");
        }
    }
    
    private void SetupRemotePlayer()
    {
        if (enableDebugLogs)
        {
            Debug.Log("[PlayerSetup] 🔴 원격 플레이어 - VR 컴포넌트들을 비활성화 상태로 유지합니다");
        }
        
        // XR Origin 비활성화 유지 (원격 플레이어는 VR 추적 불필요)
        if (xrOrigin != null)
        {
            xrOrigin.enabled = false;
            if (enableDebugLogs) Debug.Log($"[PlayerSetup] ❌ {xrOrigin.GetType().Name} 비활성화 유지 (원격 플레이어는 VR 추적 불필요)");
        }
        
        // Character Controller 활성화 (물리 충돌 및 네트워크 동기화를 위해)
        if (characterController != null)
        {
            characterController.enabled = true;
            if (enableDebugLogs) Debug.Log("[PlayerSetup] ✅ CharacterController 활성화 (물리 충돌 및 위치 동기화용)");
        }
        
        // Input 관련 컴포넌트들 비활성화 유지 (원격 플레이어는 입력 처리 불필요)
        if (xrInputModalityManager != null)
        {
            xrInputModalityManager.enabled = false;
            if (enableDebugLogs) Debug.Log($"[PlayerSetup] ❌ {xrInputModalityManager.GetType().Name} 비활성화 유지");
        }
        
        if (inputActionManager != null)
        {
            inputActionManager.enabled = false;
            if (enableDebugLogs) Debug.Log($"[PlayerSetup] ❌ {inputActionManager.GetType().Name} 비활성화 유지");
        }
        
        // 컨트롤러들은 시각적으로만 활성화 (다른 플레이어가 볼 수 있도록)
        if (leftController != null)
        {
            leftController.SetActive(true);
            DisableControllerInput(leftController);
            if (enableDebugLogs) Debug.Log("[PlayerSetup] ✅ Left Controller 시각적 활성화, 입력 비활성화");
        }
        
        if (rightController != null)
        {
            rightController.SetActive(true);
            DisableControllerInput(rightController);
            if (enableDebugLogs) Debug.Log("[PlayerSetup] ✅ Right Controller 시각적 활성화, 입력 비활성화");
        }
        
        // 메인 카메라 비활성화 (원격 플레이어 카메라는 불필요)
        if (mainCamera != null)
        {
            mainCamera.enabled = false;
            if (enableDebugLogs) Debug.Log("[PlayerSetup] ❌ Main Camera 비활성화 (원격 플레이어 카메라 불필요)");
        }
        
        // 원격 플레이어 태그 설정
        gameObject.tag = "RemotePlayer";
        
        if (enableDebugLogs)
        {
            Debug.Log("[PlayerSetup] 🔴 원격 플레이어 설정 완료 - 시각적 표현만 활성화되었습니다");
        }
    }
    
    private void EnableControllerInput(GameObject controller)
    {
        // 로컬 플레이어의 컨트롤러 입력 활성화
        // XRBaseInteractor 대신 MonoBehaviour로 찾아서 타입 체크
        var monoBehaviours = controller.GetComponentsInChildren<MonoBehaviour>();
        foreach (var mb in monoBehaviours)
        {
            string typeName = mb.GetType().Name;
            if (typeName.Contains("Interactor") ||
                typeName.Contains("Tracked") || 
                typeName.Contains("ActionBased") ||
                typeName.Contains("Input"))
            {
                mb.enabled = true;
                if (enableDebugLogs) Debug.Log($"[PlayerSetup] ✅ {typeName} 활성화");
            }
        }
    }
    
    private void DisableControllerInput(GameObject controller)
    {
        // 원격 플레이어의 컨트롤러 입력 비활성화
        // XRBaseInteractor 대신 MonoBehaviour로 찾아서 타입 체크
        var monoBehaviours = controller.GetComponentsInChildren<MonoBehaviour>();
        foreach (var mb in monoBehaviours)
        {
            string typeName = mb.GetType().Name;
            if (typeName.Contains("Interactor") ||
                typeName.Contains("Tracked") || 
                typeName.Contains("ActionBased") ||
                typeName.Contains("Input"))
            {
                mb.enabled = false;
                if (enableDebugLogs) Debug.Log($"[PlayerSetup] ❌ {typeName} 비활성화");
            }
        }
    }
    
    // 다른 스크립트들과의 호환성을 위한 메서드들
    public string GetPlayerInfo()
    {
        if (photonView == null || photonView.Owner == null) return "Unknown Player";
        
        return $"Player {photonView.Owner.ActorNumber} ({photonView.Owner.NickName}) - " +
               $"{(IsPlayerOne ? "Player1" : "Player2")} - " +
               $"{(photonView.IsMine ? "Local" : "Remote")} - " +
               $"Position: {transform.position}";
    }
    
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // 네트워크 동기화는 PlayerNetworkSync에서 처리
    }
}