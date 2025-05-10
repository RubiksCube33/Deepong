using UnityEngine;

/// <summary>
/// VR 환경에서 사용할 캔버스를 설정하는 스크립트
/// </summary>
public class VRCanvasSetup : MonoBehaviour
{
    // Canvas 컴포넌트 참조
    private Canvas canvas;
    
    // 월드 스페이스로 렌더링할지 여부
    [SerializeField] private bool worldSpaceRendering = true;
    
    private void Awake()
    {
        // Canvas 컴포넌트 찾기
        canvas = GetComponent<Canvas>();
        
        if (canvas == null)
        {
            Debug.LogError("VRCanvasSetup: Canvas 컴포넌트를 찾을 수 없습니다.");
            return;
        }
    }
    
    private void Start()
    {
        // 캔버스 설정
        SetupCanvas();
    }
    
    private void SetupCanvas()
    {
        if (canvas != null && worldSpaceRendering)
        {
            // 캔버스를 월드 스페이스 모드로 설정
            canvas.renderMode = RenderMode.WorldSpace;
            Debug.Log("캔버스가 월드 스페이스 모드로 설정되었습니다.");
            
            // 추가 설정이 필요한 경우 여기에 추가
        }
    }
}