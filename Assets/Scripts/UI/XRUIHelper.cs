using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.EventSystems;

/// <summary>
/// XR UI 상호작용을 도와주는 헬퍼 클래스
/// </summary>
public class XRUIHelper : MonoBehaviour
{
    // Unity 에디터에서 설정할 수 있는 UI 모듈 참조
    [SerializeField] private XRUIInputModule xrUIInputModule;
    
    // Start 메서드에서 호출될 초기화 함수
    private void Start()
    {
        InitializeXRUI();
    }
    
    // XR UI 설정 초기화
    private void InitializeXRUI()
    {
        // XRUIInputModule이 설정되지 않았다면 찾기 시도
        if (xrUIInputModule == null)
        {
            xrUIInputModule = FindObjectOfType<XRUIInputModule>();
            
            if (xrUIInputModule == null)
            {
                Debug.LogError("XRUIInputModule을 찾을 수 없습니다. EventSystem에 추가해주세요.");
                return;
            }
        }
        
        // XRUIInputModule 설정 확인 및 업데이트
        UpdateXRUIModuleSettings();
    }
    
    // XRUIInputModule 설정 업데이트
    private void UpdateXRUIModuleSettings()
    {
        if (xrUIInputModule != null)
        {
            // 근본적인 설정 - XR 입력 활성화
            SetPrivateField(xrUIInputModule, "m_EnableXRInput", true);
            
            Debug.Log("XR UI 입력이 활성화되었습니다. 이제 VR 컨트롤러로 UI 요소와 상호작용할 수 있습니다.");
        }
    }
    
    // 리플렉션을 사용하여 private 필드 설정
    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(obj, value);
            Debug.Log($"필드 '{fieldName}'이(가) 성공적으로 업데이트되었습니다.");
        }
        else
        {
            Debug.LogWarning($"필드 '{fieldName}'을(를) 찾을 수 없습니다.");
        }
    }
}