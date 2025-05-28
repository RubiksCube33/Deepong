using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;

// Model - 데이터 관리
public class ScoreModel
{
    // 주의: MyScore = 플레이어 1의 점수, OpponentScore = 플레이어 2의 점수
    // 각 플레이어의 시점에서 표시할 때는 UpdateScoreUI에서 변환됨
    public int MyScore { get; private set; } = 0;        // 플레이어 1의 점수
    public int OpponentScore { get; private set; } = 0;  // 플레이어 2의 점수
    public int ScoreToWin { get; private set; } = 11;
    public bool GameEnded { get; private set; } = false;

    public void AddMyScore()
    {
        if (GameEnded) return;
        MyScore++;
        CheckWinCondition();
    }

    public void AddOpponentScore()
    {
        if (GameEnded) return;
        OpponentScore++;
        CheckWinCondition();
    }

    public void Reset()
    {
        MyScore = 0;
        OpponentScore = 0;
        GameEnded = false;
    }

    private void CheckWinCondition()
    {
        if (MyScore >= ScoreToWin || OpponentScore >= ScoreToWin)
        {
            GameEnded = true;
        }
    }

    public void SetScoreToWin(int scoreToWin)
    {
        ScoreToWin = scoreToWin;
    }
}

// View - UI 표시
public class ScoreView : MonoBehaviour
{
    [Header("결과 패널")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button settingsButton;

    [Header("3D 텍스트 설정")]
    [SerializeField] private TextMeshPro scoreText3D; // 하나의 3D 텍스트만 사용
    [SerializeField] private Transform player1Position; // 플레이어 1 위치
    [SerializeField] private Transform player2Position; // 플레이어 2 위치

    private float buttonActivationDelay = 0.5f;
    private bool buttonsInteractable = true;

    private void Start()
    {
        // 결과 패널 초기에 비활성화
        if (resultPanel != null)
            resultPanel.SetActive(false);

        // 3D 텍스트 초기화
        if (scoreText3D != null)
        {
            // 초기 텍스트 설정
            scoreText3D.text = "0 : 0";
        }
        else
        {
            Debug.LogError("ScoreText3D가 연결되지 않았습니다. 3D 텍스트를 추가하고 연결해주세요.");
        }
    }

    public void UpdateScoreText(int myScore, int opponentScore, bool isPlayer1)
    {
        // 3D 텍스트 업데이트 - 간단하게 하나의 텍스트만 사용
        if (scoreText3D != null)
        {
            string scoreDisplay = $"{myScore} : {opponentScore}";
            scoreText3D.text = scoreDisplay;
        }
    }

    public void ShowResult(string message)
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

    public bool IsResultPanelActive()
    {
        return resultPanel != null && resultPanel.activeSelf;
    }

    public bool AreButtonsInteractable()
    {
        return buttonsInteractable;
    }

    public void SetResultPanelActive(bool active)
    {
        if (resultPanel != null)
            resultPanel.SetActive(active);
    }

    // 버튼 접근자
    public Button RestartButton => restartButton;
    public Button MainMenuButton => mainMenuButton;
    public Button SettingsButton => settingsButton;
    
    // 공개 초기화 메서드 - 리플렉션 대신 사용
    public void InitializeUIReferences(
        GameObject resultPanelRef,
        TextMeshProUGUI resultTextRef,
        Button restartButtonRef,
        Button mainMenuButtonRef,
        Button settingsButtonRef,
        TextMeshPro scoreText3DRef,
        Transform player1PositionRef,
        Transform player2PositionRef)
    {
        resultPanel = resultPanelRef;
        resultText = resultTextRef;
        restartButton = restartButtonRef;
        mainMenuButton = mainMenuButtonRef;
        settingsButton = settingsButtonRef;
        scoreText3D = scoreText3DRef;
        player1Position = player1PositionRef;
        player2Position = player2PositionRef;
        
        Debug.Log("ScoreView UI 참조가 성공적으로 설정되었습니다.");
    }
}

// Controller - 로직 처리 및 이벤트 연결
public class ScoreManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private int scoreToWin = 11;
    [SerializeField] private Vector3 ballInitialPosition = new Vector3(-1.05f, 1.004f, -4.362f);

    // 볼 참조 추가
    [SerializeField] private GameObject ballObject;
    private BallController ballController;

    private ScoreModel model;
    private ScoreView view;

    void Awake()
    {
        // Model 생성 및 초기화
        model = new ScoreModel();
        model.SetScoreToWin(scoreToWin);

        // View 찾기
        view = GetComponent<ScoreView>();
        if (view == null)
        {
            view = gameObject.AddComponent<ScoreView>();
            Debug.LogWarning("ScoreView 컴포넌트가 없어 자동으로 추가되었습니다.");
        }
    }

    void Start()
    {
        // 버튼 이벤트 설정
        if (view.RestartButton != null)
            view.RestartButton.onClick.AddListener(RestartGame);
            
        if (view.MainMenuButton != null)
            view.MainMenuButton.onClick.AddListener(GoToMainMenu);
            
        if (view.SettingsButton != null)
            view.SettingsButton.onClick.AddListener(OpenSettings);

        // 초기 점수 표시 업데이트
        UpdateScoreUI();
        
        // 공 객체 찾기
        if (ballObject == null)
        {
            ballObject = GameObject.FindGameObjectWithTag("Game_Ball");
            if (ballObject == null)
            {
                // 태그가 없으면 이름으로 시도
                ballObject = GameObject.Find("GameBall");
                if (ballObject == null)
                {
                    Debug.LogError("공 객체를 찾을 수 없습니다!");
                }
            }
        }
        
        // 볼 컨트롤러 참조 가져오기
        if (ballObject != null)
        {
            ballController = ballObject.GetComponent<BallController>();
        }
    }

    void OnDestroy()
    {
        // 버튼 이벤트 리스너 제거
        if (view.RestartButton != null)
            view.RestartButton.onClick.RemoveListener(RestartGame);
            
        if (view.MainMenuButton != null)
            view.MainMenuButton.onClick.RemoveListener(GoToMainMenu);
            
        if (view.SettingsButton != null)
            view.SettingsButton.onClick.RemoveListener(OpenSettings);
    }

    // 점수 증가 (로컬 플레이어)
    public void AddScore()
    {
        if (PhotonNetwork.IsConnected)
        {
            // 멀티플레이어 모드 - Photon RPC를 통해 모든 클라이언트에 점수 증가 전파
            photonView.RPC("AddScoreRPC", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
        }
        else
        {
            // 싱글플레이어 모드 - 직접 점수 추가
            model.AddMyScore();
            UpdateScoreUI();
            CheckGameOver();
        }
    }

    // 상대편 점수 증가 (싱글플레이어 전용)
    public void AddOpponentScore()
    {
        if (!PhotonNetwork.IsConnected)
        {
            // 싱글플레이어 모드에서만 사용
            model.AddOpponentScore();
            UpdateScoreUI();
            CheckGameOver();
        }
    }

    [PunRPC]
    void AddScoreRPC(int playerActorNumber)
    {
        if (playerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            model.AddMyScore();
        }
        else
        {
            model.AddOpponentScore();
        }

        UpdateScoreUI();
        CheckGameOver();
    }

    // 점수 UI 업데이트
    private void UpdateScoreUI()
    {
        // 플레이어별로 점수 표시 방식 변경
        // 각 플레이어는 자신의 점수를 왼쪽에, 상대방 점수를 오른쪽에 봅니다.
        bool isPlayer1 = PhotonNetwork.LocalPlayer.ActorNumber == 1;
        
        int myScore, opponentScore;
        
        if (isPlayer1)
        {
            // 플레이어 1의 시점: 자신의 점수(MyScore)가 왼쪽, 상대방 점수(OpponentScore)가 오른쪽
            myScore = model.MyScore;
            opponentScore = model.OpponentScore;
        }
        else
        {
            // 플레이어 2의 시점: 자신의 점수(OpponentScore)가 왼쪽, 상대방 점수(MyScore)가 오른쪽
            myScore = model.OpponentScore;
            opponentScore = model.MyScore;
        }
        
        view.UpdateScoreText(myScore, opponentScore, isPlayer1);
    }

    // 게임 오버 체크
    private void CheckGameOver()
    {
        if (model.GameEnded)
        {
            string resultMessage;
            
            if (PhotonNetwork.IsConnected)
            {
                // 멀티플레이어 모드
                bool isPlayer1 = PhotonNetwork.LocalPlayer.ActorNumber == 1;
                
                // 각 플레이어의 시점에서 승리/패배 판단
                int myActualScore, opponentActualScore;
                
                if (isPlayer1)
                {
                    // 플레이어 1의 시점
                    myActualScore = model.MyScore;
                    opponentActualScore = model.OpponentScore;
                }
                else
                {
                    // 플레이어 2의 시점
                    myActualScore = model.OpponentScore;
                    opponentActualScore = model.MyScore;
                }
                
                if (myActualScore > opponentActualScore)
                {
                    resultMessage = "YOU WIN!";
                }
                else
                {
                    resultMessage = "YOU LOSE!";
                }
            }
            else
            {
                // 싱글플레이어 모드 - 단순히 점수 도달 메시지
                resultMessage = $"게임 완료! 점수: {model.MyScore}";
            }
            
            view.ShowResult(resultMessage);
        }
    }

    // 게임 재시작
    public void RestartGame()
    {
        if (PhotonNetwork.IsConnected)
        {
            // 네트워크 게임인 경우 모든 클라이언트에 재시작 전파
            photonView.RPC("RestartGameRPC", RpcTarget.All);
        }
        else
        {
            // 싱글플레이어 게임은 로컬에서만 재시작
            RestartGameRPC();
        }
    }
    
    [PunRPC]
    void RestartGameRPC()
    {
        // 점수 초기화
        model.Reset();
        
        // 결과 패널 숨기기
        view.SetResultPanelActive(false);
        
        // UI 업데이트
        UpdateScoreUI();
        
        // 공 위치 초기화
        ResetBall();
    }
    
    // 공 위치 초기화
    private void ResetBall()
    {
        // 볼 컨트롤러 사용
        if (ballController != null)
        {
            ballController.ResetBallPosition();
        }
        // 없으면 직접 위치 설정
        else if (ballObject != null)
        {
            Rigidbody ballRb = ballObject.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                ballRb.velocity = Vector3.zero;
                ballRb.angularVelocity = Vector3.zero;
            }
            ballObject.transform.position = ballInitialPosition;
        }
    }

    // 메인 메뉴로 이동
    public void GoToMainMenu()
    {
        // 네트워크 연결 해제 및 초기 씬으로 이동
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
        SceneManager.LoadScene("MainMenu");
    }
    
    // 설정 메뉴 열기
    public void OpenSettings()
    {
        // 설정 UI 활성화 등의 작업 (향후 구현)
        Debug.Log("설정 메뉴 열기");
    }
    
    public bool IsResultPanelActive()
    {
        return view.IsResultPanelActive();
    }
}