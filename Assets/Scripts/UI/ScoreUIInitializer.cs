using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class ScoreUIInitializer : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("게임 결과를 표시할 패널 (초기에 비활성화)")]
    [SerializeField] private GameObject resultPanel;

    [Tooltip("결과 메시지를 표시할 TextMeshProUGUI 컴포넌트")]
    [SerializeField] private TextMeshProUGUI resultText;

    [Tooltip("게임 재시작 버튼")]
    [SerializeField] private Button restartButton;

    [Tooltip("메인 메뉴 이동 버튼")]
    [SerializeField] private Button mainMenuButton;

    [Tooltip("설정 화면 버튼")]
    [SerializeField] private Button settingsButton;

    [Header("3D 텍스트 설정")]
    [Tooltip("월드 스페이스에 표시할 3D 텍스트")]
    [SerializeField] private TextMeshPro scoreText3D;

    [Tooltip("플레이어 1의 위치(MasterClient)")]
    [SerializeField] private Transform player1Position;

    [Tooltip("플레이어 2의 위치")]
    [SerializeField] private Transform player2Position;

    [Header("자동 찾기 설정")]
    [Tooltip("3D 텍스트를 자동으로 찾을지 여부")]
    [SerializeField] private bool autoFindScoreText3D = true;

    [Tooltip("플레이어 위치를 자동으로 찾을지 여부")]
    [SerializeField] private bool autoFindPlayerPositions = true;

    [Tooltip("ScoreManager를 자동으로 생성할지 여부")]
    [SerializeField] private bool autoCreateScoreManager = true;

    private void Start()
    {
        // 결과 패널 초기 비활성화
        if (resultPanel != null)
            resultPanel.SetActive(false);

        // 버튼 이벤트 리스너 추가
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);

        // 자동 찾기 수행
        PerformAutoFind();

        // ScoreManager 초기화
        InitializeScoreManager();

        Debug.Log("ScoreUI 초기화가 완료되었습니다.");
    }

    private void PerformAutoFind()
    {
        // 3D 텍스트 자동 찾기 - 간단하게 첫 번째 TextMeshPro 사용
        if (autoFindScoreText3D && scoreText3D == null)
        {
            scoreText3D = FindObjectOfType<TextMeshPro>();
            if (scoreText3D == null)
            {
                Debug.LogWarning("3D 텍스트(TextMeshPro)를 찾을 수 없습니다. 직접 연결하거나 씬에 추가해주세요.");
            }
        }

        // 플레이어 위치 자동 찾기 (태그 수정)
        if (autoFindPlayerPositions)
        {
            if (player1Position == null)
            {
                GameObject player1 = GameObject.FindGameObjectWithTag("player1");
                if (player1 != null)
                {
                    player1Position = player1.transform;
                }
                else
                {
                    Debug.LogWarning("player1 태그를 가진 오브젝트를 찾을 수 없습니다.");
                }
            }

            if (player2Position == null)
            {
                GameObject player2 = GameObject.FindGameObjectWithTag("player2");
                if (player2 != null)
                {
                    player2Position = player2.transform;
                }
                else
                {
                    Debug.LogWarning("player2 태그를 가진 오브젝트를 찾을 수 없습니다.");
                }
            }
        }
    }

    private void InitializeScoreManager()
    {
        // ScoreManager 찾기 또는 생성
        ScoreManager scoreManager = FindObjectOfType<ScoreManager>();
        
        if (scoreManager == null && autoCreateScoreManager)
        {
            scoreManager = CreateScoreManager();
        }

        if (scoreManager == null)
        {
            Debug.LogError("ScoreManager를 찾을 수 없고 자동 생성도 비활성화되어 있습니다.");
            return;
        }

        // ScoreView 초기화
        InitializeScoreView(scoreManager);
    }

    private ScoreManager CreateScoreManager()
    {
        // ScoreManager 생성
        GameObject scoreManagerObj = new GameObject("ScoreManager");
        ScoreManager scoreManager = scoreManagerObj.AddComponent<ScoreManager>();
        
        // 네트워크 동기화를 위한 PhotonView 추가 (네트워크 연결 시에만)
        if (PhotonNetwork.IsConnected || PhotonNetwork.NetworkingClient != null)
        {
            PhotonView photonView = scoreManagerObj.AddComponent<PhotonView>();
            
            // ViewID 자동 할당 (999는 고정값 대신 자동 할당 사용)
            if (PhotonNetwork.AllocateViewID(photonView))
            {
                photonView.Synchronization = ViewSynchronization.UnreliableOnChange;
                photonView.ObservedComponents = new System.Collections.Generic.List<Component> { scoreManager };
                photonView.OwnershipTransfer = OwnershipOption.Takeover;
            }
            else
            {
                Debug.LogWarning("PhotonView ID 할당에 실패했습니다.");
            }
        }

        Debug.Log("ScoreManager가 성공적으로 생성되었습니다.");
        return scoreManager;
    }

    private void InitializeScoreView(ScoreManager scoreManager)
    {
        // ScoreView 컴포넌트 추가 및 참조 설정
        ScoreView scoreView = scoreManager.gameObject.GetComponent<ScoreView>();
        if (scoreView == null)
        {
            scoreView = scoreManager.gameObject.AddComponent<ScoreView>();
        }

        // 공개 메서드를 사용하여 UI 참조 설정 (리플렉션 대신)
        scoreView.InitializeUIReferences(
            resultPanel,
            resultText,
            restartButton,
            mainMenuButton,
            settingsButton,
            scoreText3D,
            player1Position,
            player2Position
        );
    }

    // 수동으로 ScoreManager 재초기화 (필요시 외부에서 호출)
    public void ReinitializeScoreManager()
    {
        InitializeScoreManager();
    }

    // UI 참조들이 올바르게 설정되었는지 검증
    public bool ValidateUIReferences()
    {
        bool isValid = true;

        if (resultPanel == null)
        {
            Debug.LogWarning("Result Panel이 설정되지 않았습니다.");
            isValid = false;
        }

        if (resultText == null)
        {
            Debug.LogWarning("Result Text가 설정되지 않았습니다.");
            isValid = false;
        }

        if (restartButton == null)
        {
            Debug.LogWarning("Restart Button이 설정되지 않았습니다.");
            isValid = false;
        }

        if (scoreText3D == null)
        {
            Debug.LogWarning("Score Text 3D가 설정되지 않았습니다.");
            isValid = false;
        }

        return isValid;
    }

    // 메인 메뉴로 이동하는 메서드
    private void GoToMainMenu()
    {
        // 네트워크 연결 해제 (Photon 사용 시)
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.LeaveRoom();
        }
        
        // 메인 메뉴 씬으로 이동 (실제 씬 이름으로 변경 필요)
        SceneManager.LoadScene("MainMenuScene");
    }
}