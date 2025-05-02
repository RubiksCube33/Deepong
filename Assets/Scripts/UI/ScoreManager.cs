using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class ScoreManager : MonoBehaviour
{
    public TextMeshProUGUI playerScoreText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    
    public Button restartButton;
    public Button mainMenuButton;
    public Button settingsButton;
    
    [SerializeField] private int scoreToWin = 11;
    [SerializeField] private Vector3 ballInitialPosition = new Vector3(-1.05f, 1.004f, -4.362f);
    
    private int playerScore = 0;
    private bool gameEnded = false;
    private float buttonActivationDelay = 0.5f; // 버튼 활성화 지연 시간 (초)
    private bool buttonsInteractable = true;
    
    void Start()
    {
        // 결과 패널 초기에 비활성화
        if (resultPanel != null)
            resultPanel.SetActive(false);
            
        // 초기 점수 표시
        UpdateScoreText();
        
        // 버튼 클릭 이벤트 설정
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
            
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);
            
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);
    }
    
    void OnDestroy()
    {
        // 버튼 이벤트 리스너 제거
        if (restartButton != null)
            restartButton.onClick.RemoveListener(RestartGame);
            
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(GoToMainMenu);
            
        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OpenSettings);
    }
    
    // 점수 증가
    public void AddScore()
    {
        if (gameEnded) return;
        
        playerScore++;
        UpdateScoreText();
        Debug.Log("점수: " + playerScore);
        
        // 승리 조건 확인
        CheckWinCondition();
    }
    
    // 점수 텍스트 업데이트
    private void UpdateScoreText()
    {
        if (playerScoreText != null)
            playerScoreText.text = playerScore.ToString();
    }
    
    // 승리 조건 확인
    private void CheckWinCondition()
    {
        if (playerScore >= scoreToWin)
        {
            ShowResult("YOU WIN!");
            gameEnded = true;
        }
    }
    
    // 결과 표시
    private void ShowResult(string message)
    {
        if (resultPanel != null && resultText != null)
        {
            // 버튼 상호작용 비활성화
            SetButtonsInteractable(false);
            
            // 결과 패널 활성화
            resultPanel.SetActive(true);
            resultText.text = message;
            
            // 지연 후 버튼 활성화
            StartCoroutine(EnableButtonsAfterDelay());
        }
    }
    
    // 일정 시간 후 버튼 활성화
    private IEnumerator EnableButtonsAfterDelay()
    {
        yield return new WaitForSeconds(buttonActivationDelay);
        SetButtonsInteractable(true);
    }
    
    // 모든 버튼의 상호작용 설정
    private void SetButtonsInteractable(bool interactable)
    {
        buttonsInteractable = interactable;
        
        if (restartButton != null)
            restartButton.interactable = interactable;
            
        if (mainMenuButton != null)
            mainMenuButton.interactable = interactable;
            
        if (settingsButton != null)
            settingsButton.interactable = interactable;
    }
    
    // 게임 재시작
    public void RestartGame()
    {
        // 버튼이 비활성화 상태이면 무시
        if (!buttonsInteractable) return;
        
        playerScore = 0;
        gameEnded = false;
        UpdateScoreText();
        
        if (resultPanel != null)
            resultPanel.SetActive(false);
            
        // 공 위치 초기화
        GameObject ball = GameObject.FindGameObjectWithTag("Game_Ball");
        if (ball != null)
        {
            ball.transform.position = ballInitialPosition;
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            Debug.LogWarning("Game_Ball 태그를 가진 오브젝트를 찾을 수 없습니다!");
        }
    }
    
    // 메인 메뉴로 이동
    public void GoToMainMenu()
    {
        // 버튼이 비활성화 상태이면 무시
        if (!buttonsInteractable) return;
        
        SceneManager.LoadScene("MainMenuScene");
    }
    
    // 설정 화면 열기
    public void OpenSettings()
    {
        // 버튼이 비활성화 상태이면 무시
        if (!buttonsInteractable) return;
        
        Debug.Log("설정 화면 열기");
    }
    
    public bool IsResultPanelActive()
    {
        return resultPanel != null && resultPanel.activeSelf;
    }
}