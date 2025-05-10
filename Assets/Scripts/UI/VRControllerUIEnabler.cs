using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// VR 컨트롤러의 UI 상호작용을 활성화하는 스크립트
/// </summary>
public class VRControllerUIEnabler : MonoBehaviour
{
    // Near-Far Interactor 컴포넌트 참조
    private NearFarInteractor nearFarInteractor;

    private void Awake()
    {
        // Near-Far Interactor 컴포넌트 찾기
        nearFarInteractor = GetComponent<NearFarInteractor>();
        
        if (nearFarInteractor == null)
        {
            Debug.LogError("VRControllerUIEnabler: NearFarInteractor 컴포넌트를 찾을 수 없습니다.");
            return;
        }
    }

    private void Start()
    {
        // UI 상호작용 활성화
        EnableUIInteraction();
    }

    private void EnableUIInteraction()
    {
        if (nearFarInteractor != null)
        {
            // 리플렉션을 통해 private 필드 접근
            var enableUIField = nearFarInteractor.GetType().GetField("m_EnableUIInteraction", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (enableUIField != null)
            {
                enableUIField.SetValue(nearFarInteractor, true);
                Debug.Log("VR 컨트롤러의 UI 상호작용이 활성화되었습니다.");
            }
            else
            {
                Debug.LogWarning("m_EnableUIInteraction 필드를 찾을 수 없습니다.");
            }
            
            // UI Press Input 모드 설정
            var uiPressInputField = nearFarInteractor.GetType().GetField("m_UIPressInput", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (uiPressInputField != null)
            {
                var uiPressInput = uiPressInputField.GetValue(nearFarInteractor);
                if (uiPressInput != null)
                {
                    var inputSourceModeField = uiPressInput.GetType().GetField("m_InputSourceMode", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    
                    if (inputSourceModeField != null)
                    {
                        // 입력 소스 모드를 2(Action Reference)로 설정
                        inputSourceModeField.SetValue(uiPressInput, 2);
                        Debug.Log("UI Press Input 모드가 설정되었습니다.");
                    }
                }
            }
            else
            {
                Debug.LogWarning("m_UIPressInput 필드를 찾을 수 없습니다.");
            }
        }
    }
}