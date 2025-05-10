using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// VR 컨트롤러와 UI 상호작용을 설정하는 클래스
/// </summary>
public class XRUIInputSetup : MonoBehaviour
{
    [SerializeField] private InputActionReference leftControllerTrackingAction;
    [SerializeField] private InputActionReference rightControllerTrackingAction;
    [SerializeField] private InputActionReference leftControllerSelectAction;
    [SerializeField] private InputActionReference rightControllerSelectAction;
    
    private XRUIInputModule xrUIInputModule;

    private void Awake()
    {
        xrUIInputModule = GetComponent<XRUIInputModule>();
        if (xrUIInputModule == null)
        {
            Debug.LogError("XRUIInputModule를 찾을 수 없습니다.");
            return;
        }
    }

    private void OnEnable()
    {
        // XRUIInputModule가 VR 컨트롤러 입력을 인식하도록 설정
        EnableTrackedDeviceInput();
    }

    private void OnDisable()
    {
        // 스크립트 비활성화 시 정리
        DisableTrackedDeviceInput();
    }

    private void EnableTrackedDeviceInput()
    {
        if (xrUIInputModule == null) return;

        // 왼쪽 컨트롤러 입력 활성화 (필요시)
        if (leftControllerTrackingAction != null && leftControllerSelectAction != null)
        {
            // XRUIInputModule에 등록
            leftControllerTrackingAction.action?.Enable();
            leftControllerSelectAction.action?.Enable();
            Debug.Log("왼쪽 VR 컨트롤러 UI 입력 활성화됨");
        }

        // 오른쪽 컨트롤러 입력 활성화 (필요시)
        if (rightControllerTrackingAction != null && rightControllerSelectAction != null)
        {
            // XRUIInputModule에 등록
            rightControllerTrackingAction.action?.Enable();
            rightControllerSelectAction.action?.Enable();
            Debug.Log("오른쪽 VR 컨트롤러 UI 입력 활성화됨");
        }
    }

    private void DisableTrackedDeviceInput()
    {
        if (xrUIInputModule == null) return;

        // 왼쪽 컨트롤러 입력 비활성화
        if (leftControllerTrackingAction != null && leftControllerSelectAction != null)
        {
            leftControllerTrackingAction.action?.Disable();
            leftControllerSelectAction.action?.Disable();
        }

        // 오른쪽 컨트롤러 입력 비활성화
        if (rightControllerTrackingAction != null && rightControllerSelectAction != null)
        {
            rightControllerTrackingAction.action?.Disable();
            rightControllerSelectAction.action?.Disable();
        }
    }
}