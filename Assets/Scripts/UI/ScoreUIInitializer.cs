using UnityEngine;
using TMPro;
using UnityEngine.UI;


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

    [Tooltip("3D 텍스트를 자동으로 찾을지 여부")]
    [SerializeField] private bool autoFindScoreText3D = true;

    [Tooltip("플레이어 위치를 자동으로 찾을지 여부")]
    [SerializeField] private bool autoFindPlayerPositions = true;

    private void Start()
    {
        // 결과 패널 초기 비활성화
        if (resultPanel != null)
            resultPanel.SetActive(false);

        // 3D 텍스트 자동 찾기
        if (autoFindScoreText3D && scoreText3D == null)
        {
            scoreText3D = FindObjectOfType<TextMeshPro>();
            if (scoreText3D == null)
            {
                Debug.LogError("3D 텍스트(TextMeshPro)를 찾을 수 없습니다. 직접 연결하거나 씬에 추가해주세요.");
                return;
            }
        }

        // 플레이어 위치 자동 찾기
        if (autoFindPlayerPositions)
        {
            if (player1Position == null)
            {
                GameObject player1 = GameObject.FindGameObjectWithTag("Player1");
                if (player1 != null)
                {
                    player1Position = player1.transform;
                }
                else
                {
                    Debug.LogWarning("Player1 태그를 가진 오브젝트를 찾을 수 없습니다.");
                }
            }

            if (player2Position == null)
            {
                GameObject player2 = GameObject.FindGameObjectWithTag("Player2");
                if (player2 != null)
                {
                    player2Position = player2.transform;
                }
                else
                {
                    Debug.LogWarning("Player2 태그를 가진 오브젝트를 찾을 수 없습니다.");
                }
            }
        }

        // ScoreManager가 이미 있는지 확인
        ScoreManager scoreManager = FindObjectOfType<ScoreManager>();
        if (scoreManager == null)
        {
            // ScoreManager 생성
            GameObject scoreManagerObj = new GameObject("ScoreManager");
            scoreManager = scoreManagerObj.AddComponent<ScoreManager>();
            
            // 네트워크 동기화를 위한 PhotonView 추가
            Photon.Pun.PhotonView photonView = scoreManagerObj.AddComponent<Photon.Pun.PhotonView>();
            photonView.ViewID = 999; // 고유한 ViewID 설정
            photonView.Synchronization = Photon.Pun.ViewSynchronization.UnreliableOnChange;
            photonView.ObservedComponents = new System.Collections.Generic.List<Component> { scoreManager };
            photonView.OwnershipTransfer = Photon.Pun.OwnershipOption.Takeover;
        }

        // ScoreView 컴포넌트 추가 및 참조 설정
        ScoreView scoreView = scoreManager.gameObject.GetComponent<ScoreView>();
        if (scoreView == null)
        {
            scoreView = scoreManager.gameObject.AddComponent<ScoreView>();
        }

        // ScoreView에 UI 참조 전달을 위한 리플렉션 사용
        SetPrivateFieldValue(scoreView, "resultPanel", resultPanel);
        SetPrivateFieldValue(scoreView, "resultText", resultText);
        SetPrivateFieldValue(scoreView, "restartButton", restartButton);
        SetPrivateFieldValue(scoreView, "mainMenuButton", mainMenuButton);
        SetPrivateFieldValue(scoreView, "settingsButton", settingsButton);
        
        // 3D 텍스트 및 플레이어 위치 참조 설정
        SetPrivateFieldValue(scoreView, "scoreText3D", scoreText3D);
        SetPrivateFieldValue(scoreView, "player1Position", player1Position);
        SetPrivateFieldValue(scoreView, "player2Position", player2Position);

        Debug.Log("ScoreUI가 성공적으로 초기화되었습니다.");
    }

    // 리플렉션을 사용하여 private 필드 값 설정
    private void SetPrivateFieldValue(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            field.SetValue(target, value);
        }
        else
        {
            Debug.LogError($"필드 '{fieldName}'을 찾을 수 없습니다.");
        }
    }
}