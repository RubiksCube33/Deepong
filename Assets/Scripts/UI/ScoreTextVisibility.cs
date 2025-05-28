using UnityEngine;
using Photon.Pun;

public class ScoreTextVisibility : MonoBehaviour
{
    [Header("가시성 설정")]
    [SerializeField] private float updateInterval = 0.1f; // 업데이트 간격 (초)
    [SerializeField] private bool enableBillboard = true; // 빌보드 효과 활성화
    
    private float lastUpdateTime;
    private Camera mainCamera;
    
    private void Start()
    {
        // 초기 가시성 설정 - 항상 표시
        gameObject.SetActive(true);
        
        // 메인 카메라 찾기
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
    }
    
    private void Update()
    {
        // 성능 최적화: 일정 간격으로만 업데이트
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateVisibility();
            if (enableBillboard)
            {
                UpdateBillboard();
            }
            lastUpdateTime = Time.time;
        }
    }
    
    private void UpdateVisibility()
    {
        // 간소화: 모든 플레이어에게 동일한 텍스트를 표시
        // 네트워크 연결 여부와 상관없이 항상 표시
        gameObject.SetActive(true);
    }
    
    private void UpdateBillboard()
    {
        if (mainCamera == null) return;
        
        // 카메라를 향하도록 회전 (빌보드 효과)
        Vector3 directionToCamera = mainCamera.transform.position - transform.position;
        
        if (directionToCamera != Vector3.zero)
        {
            // LookRotation을 사용해서 카메라를 향하도록 설정
            Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
            
            // 텍스트가 올바른 방향으로 보이도록 Y축으로 180도 회전 추가
            targetRotation *= Quaternion.Euler(0, 180, 0);
            
            transform.rotation = targetRotation;
        }
    }
    
    // 수동으로 가시성 업데이트 (필요시 외부에서 호출)
    public void ForceUpdateVisibility()
    {
        UpdateVisibility();
    }
}