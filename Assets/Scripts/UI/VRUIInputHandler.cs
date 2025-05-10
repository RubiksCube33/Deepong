using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class VRUIInputHandler : MonoBehaviour
{
    [SerializeField]
    private XRUIInputModule xrUIInputModule;

    [SerializeField] 
    private InputActionAsset actionAsset;
    
    [SerializeField]
    private string actionMapName = "XRI LeftHand Interaction";
    
    [SerializeField]
    private string selectAction = "Select";
    
    [SerializeField] 
    private string pointerPositionAction = "Position";
    
    private InputActionReference m_LeftSelectActionReference;
    private InputActionReference m_RightSelectActionReference;

    private void Awake()
    {
        if (xrUIInputModule == null)
        {
            xrUIInputModule = FindObjectOfType<XRUIInputModule>();
        }
        
        if (actionAsset == null)
        {
            Debug.LogError("VRUIInputHandler: Input Action Asset가 설정되지 않았습니다.");
            return;
        }
        
        // 왼쪽 컨트롤러 Select 액션 참조 생성
        var leftMap = actionAsset.FindActionMap("XRI LeftHand Interaction");
        if (leftMap != null)
        {
            var leftSelectAction = leftMap.FindAction("Select");
            if (leftSelectAction != null)
            {
                m_LeftSelectActionReference = InputActionReference.Create(leftSelectAction);
            }
        }
        
        // 오른쪽 컨트롤러 Select 액션 참조 생성
        var rightMap = actionAsset.FindActionMap("XRI RightHand Interaction");
        if (rightMap != null)
        {
            var rightSelectAction = rightMap.FindAction("Select");
            if (rightSelectAction != null)
            {
                m_RightSelectActionReference = InputActionReference.Create(rightSelectAction);
            }
        }
    }

    private void OnEnable()
    {
        if (xrUIInputModule != null)
        {
            SetupUIModule();
        }
    }

    private void SetupUIModule()
    {
        Debug.Log("VR UI 입력 핸들러: UI 모듈 설정 중...");
        
        // 왼쪽 및 오른쪽 컨트롤러 설정
        if (m_LeftSelectActionReference != null && m_LeftSelectActionReference.action != null)
        {
            Debug.Log("왼쪽 컨트롤러 Select 액션이 설정되었습니다.");
            
            // 액션이 활성화되었는지 확인
            if (!m_LeftSelectActionReference.action.enabled)
            {
                m_LeftSelectActionReference.action.Enable();
            }
        }
        else
        {
            Debug.LogWarning("왼쪽 컨트롤러 Select 액션이 설정되지 않았습니다.");
        }
        
        if (m_RightSelectActionReference != null && m_RightSelectActionReference.action != null)
        {
            Debug.Log("오른쪽 컨트롤러 Select 액션이 설정되었습니다.");
            
            // 액션이 활성화되었는지 확인
            if (!m_RightSelectActionReference.action.enabled)
            {
                m_RightSelectActionReference.action.Enable();
            }
        }
        else
        {
            Debug.LogWarning("오른쪽 컨트롤러 Select 액션이 설정되지 않았습니다.");
        }
        
        // InputActionManager를 통해 입력 액션 활성화
        var inputActionManager = FindObjectOfType<InputActionManager>();
        if (inputActionManager != null)
        {
            if (m_LeftSelectActionReference != null)
            {
                inputActionManager.actionAssets.Add(actionAsset);
            }
            
            inputActionManager.EnableInput();
        }
    }
}