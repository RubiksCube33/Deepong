using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// 씬의 모든 NearFarInteractor 컴포넌트를 찾아 UI 상호작용을 활성화하는 스크립트
/// </summary>
public class InteractorUISetup : MonoBehaviour
{
    private void Start()
    {
        // 모든 NearFarInteractor 찾기
        var interactors = FindObjectsOfType<NearFarInteractor>();
        
        if (interactors.Length == 0)
        {
            Debug.LogWarning("씬에서 NearFarInteractor를 찾을 수 없습니다.");
            return;
        }
        
        // 각 인터랙터에 대해 UI 상호작용 활성화
        foreach (var interactor in interactors)
        {
            EnableUIInteraction(interactor);
        }
    }
    
    private void EnableUIInteraction(NearFarInteractor interactor)
    {
        Debug.Log($"인터랙터 '{interactor.name}'에 대한 UI 상호작용 활성화 시도");
        
        // VRControllerUIEnabler 컴포넌트가 이미 있는지 확인
        if (interactor.GetComponent<VRControllerUIEnabler>() == null)
        {
            // 없으면 추가
            interactor.gameObject.AddComponent<VRControllerUIEnabler>();
            Debug.Log($"인터랙터 '{interactor.name}'에 VRControllerUIEnabler 컴포넌트 추가 완료");
        }
        else
        {
            Debug.Log($"인터랙터 '{interactor.name}'에 이미 VRControllerUIEnabler 컴포넌트가 있음");
        }
    }
}