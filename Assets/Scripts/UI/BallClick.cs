using UnityEngine;
using UnityEngine.EventSystems;

public class BallClick : MonoBehaviour
{
    private ScoreManager scoreManager;
    
    void Start()
    {
        // ScoreManager 찾기
        scoreManager = FindObjectOfType<ScoreManager>();
        
        if (scoreManager == null)
        {
            Debug.LogError("ScoreManager를 찾을 수 없습니다!");
        }
    }
    
    void OnMouseDown()
    {
        // 결과 패널이 활성화되어 있으면 클릭 무시
        if (scoreManager != null && scoreManager.IsResultPanelActive())
        {
            return;
        }
        
        if (scoreManager != null)
        {
            Debug.Log("공을 클릭했습니다!");
            scoreManager.AddScore();
        }
    }
}