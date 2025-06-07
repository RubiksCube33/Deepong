using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;
using System;
using Photon.Pun;

namespace DeepongVR.Court
{
    /// <summary>
    /// A버튼으로 패들을 순환 변경하는 간단한 컨트롤러
    /// </summary>
    public class PaddleChangeController : MonoBehaviour
    {
        [Header("Paddle Settings")]
        [SerializeField] private GameObject paddle_racket;      // 첫 번째 패들
        [SerializeField] private GameObject paddle_sword;       // 두 번째 패들
        [SerializeField] private GameObject paddle_glove_left;  // 세 번째 패들
        [SerializeField] private GameObject paddle_glove_right; // 세 번째 패들의 짝

        [Header("Controller Settings")]
        [SerializeField] private bool isRightController = true; // 오른쪽 컨트롤러인지 여부

        [Header("Events")]
        [SerializeField] private UnityEvent<int> OnPaddleChanged; // 패들 변경 시 발생하는 이벤트

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        // 내부 변수들
        private InputAction primaryButtonAction;
        private int _currentPaddleIndex = 0;
        private GameObject[] paddles;
        
        // 네트워크 관련 참조
        private PhotonView photonView;

        // 정적 이벤트 (다른 스크립트에서 구독 가능)
        public static event Action<int, string> OnPaddleChangedGlobal;

        /// <summary>
        /// 현재 패들 인덱스 (다른 스크립트에서 읽기/쓰기 가능)
        /// </summary>
        public int CurrentPaddleIndex
        {
            //Getter & Setter 사용
            //값 변경 시 이벤트 발생
            get { return _currentPaddleIndex; }
            set 
            { 
                if (_currentPaddleIndex != value)
                {
                    int previousIndex = _currentPaddleIndex;
                    _currentPaddleIndex = Mathf.Clamp(value, 0, 2); // 0~2 범위로 제한
                    SetActivePaddle(_currentPaddleIndex);
                    
                    // 이벤트 발생
                    OnPaddleChanged?.Invoke(_currentPaddleIndex);
                    OnPaddleChangedGlobal?.Invoke(_currentPaddleIndex, GetCurrentPaddleName());
                    
                    if (enableDebugLogs)
                    {
                        bool isLocal = IsLocalPlayer();
                        string playerType = isLocal ? "로컬" : "원격";
                        string playerName = photonView != null ? photonView.Owner.NickName : "Local";
                        Debug.Log($"[PaddleChangeController] {playerType} 플레이어({playerName}) 패들 변경: {previousIndex} → {_currentPaddleIndex} ({GetCurrentPaddleName()})");
                    }
                }
            }
        }

        /// <summary>
        /// 현재 패들 이름 (읽기 전용)
        /// </summary>
        public string CurrentPaddleName => GetCurrentPaddleName();

        /// <summary>
        /// 패들 타입 열거형
        /// </summary>
        public enum PaddleType
        {
            Racket = 0,
            Sword = 1,
            Glove = 2
        }

        /// <summary>
        /// 현재 패들 타입 (읽기 전용)
        /// </summary>
        public PaddleType CurrentPaddleType => (PaddleType)_currentPaddleIndex;

        void Start()
        {
            // 패들 배열 초기화
            paddles = new GameObject[] { paddle_racket, paddle_sword, paddle_glove_left, paddle_glove_right };
            
            // PhotonView 참조 가져오기
            photonView = GetComponent<PhotonView>();
            if (photonView == null)
            {
                Debug.LogWarning("[PaddleChangeController] PhotonView가 없습니다. 로컬 전용 모드로 동작합니다.");
            }
            
            // 초기 패들 설정 (첫 번째만 활성화)
            CurrentPaddleIndex = 0; // Property를 사용해서 이벤트도 발생
            
            if (enableDebugLogs)
            {
                bool isLocal = photonView == null || photonView.IsMine;
                Debug.Log($"[PaddleChangeController] 초기화 완료 - 로컬 플레이어: {isLocal}");
            }
        }

        void OnEnable()
        {
            // 로컬 플레이어일 때만 입력 액션 설정
            if (IsLocalPlayer())
            {
                SetupInputAction();
            }
        }

        void OnDisable()
        {
            // 로컬 플레이어일 때만 입력 액션 정리
            if (IsLocalPlayer())
            {
                CleanupInputAction();
            }
        }
        
        /// <summary>
        /// 현재 플레이어가 로컬 플레이어인지 확인
        /// </summary>
        private bool IsLocalPlayer()
        {
            return photonView == null || photonView.IsMine;
        }

        private void SetupInputAction()
        {
            // 오른쪽/왼쪽 컨트롤러에 따라 Primary Button 액션 생성
            string controllerHand = isRightController ? "RightHand" : "LeftHand";
            
            primaryButtonAction = new InputAction(
                name: "PaddleChangePrimaryButton",
                binding: $"<XRController>{{{controllerHand}}}/primaryButton"
            );

            primaryButtonAction.performed += OnPrimaryButtonPressed;
            primaryButtonAction.Enable();

            if (enableDebugLogs)
            {
                Debug.Log($"[PaddleChangeController] {controllerHand} Primary Button 액션 설정 완료");
            }
        }

        private void CleanupInputAction()
        {
            if (primaryButtonAction != null)
            {
                primaryButtonAction.performed -= OnPrimaryButtonPressed;
                primaryButtonAction.Disable();
                primaryButtonAction.Dispose();
            }
        }

        private void OnPrimaryButtonPressed(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
            {
                // 로컬 플레이어만 패들 변경 가능
                if (!IsLocalPlayer())
                {
                    if (enableDebugLogs)
                    {
                        Debug.LogWarning("[PaddleChangeController] 원격 플레이어는 직접 패들을 변경할 수 없습니다.");
                    }
                    return;
                }
                
                // 다음 패들로 변경
                ChangeToNextPaddle();

                if (enableDebugLogs)
                {
                    Debug.Log("[PaddleChangeController] A 버튼 입력으로 패들 변경됨 - 네트워크 동기화 시작");
                }
            }
        }

        /// <summary>
        /// 다음 패들로 변경
        /// </summary>
        public void ChangeToNextPaddle()
        {
            CurrentPaddleIndex = (_currentPaddleIndex + 1) % 3; // Property 사용
        }

        /// <summary>
        /// 이전 패들로 변경
        /// </summary>
        public void ChangeToPreviousPaddle()
        {
            CurrentPaddleIndex = (_currentPaddleIndex - 1 + 3) % 3; // Property 사용
        }

        /// <summary>
        /// 특정 패들로 직접 변경
        /// </summary>
        /// <param name="paddleType">변경할 패들 타입</param>
        public void ChangeToPaddle(PaddleType paddleType)
        {
            CurrentPaddleIndex = (int)paddleType;
        }

        /// <summary>
        /// 특정 패들을 활성화하고 나머지는 비활성화
        /// </summary>
        private void SetActivePaddle(int index)
        {
            if (enableDebugLogs)
            {
                string playerType = IsLocalPlayer() ? "로컬" : "원격";
                Debug.Log($"[PaddleChangeController] {playerType} 플레이어 패들 변경 시작: 인덱스 {index} ({GetPaddleNameByIndex(index)})");
            }

            // 모든 패들 비활성화
            for (int i = 0; i < paddles.Length; i++)
            {
                if (paddles[i] != null)
                {
                    bool wasActive = paddles[i].activeSelf;
                    paddles[i].SetActive(false);
                    
                    if (enableDebugLogs && wasActive)
                    {
                        Debug.Log($"[PaddleChangeController] 패들 비활성화: {paddles[i].name}");
                    }
                }
                else if (enableDebugLogs)
                {
                    Debug.LogWarning($"[PaddleChangeController] 패들 {i}가 null입니다");
                }
            }

            // 선택된 패들 활성화
            bool activationSuccess = false;
            switch (index)
            {
                case 0: // Racket
                    if (paddle_racket != null)
                    {
                        paddle_racket.SetActive(true);
                        activationSuccess = true;
                        if (enableDebugLogs)
                            Debug.Log($"[PaddleChangeController] Racket 패들 활성화됨: {paddle_racket.name}");
                    }
                    else if (enableDebugLogs)
                    {
                        Debug.LogError("[PaddleChangeController] paddle_racket이 null입니다!");
                    }
                    break;
                case 1: // Sword
                    if (paddle_sword != null)
                    {
                        paddle_sword.SetActive(true);
                        activationSuccess = true;
                        if (enableDebugLogs)
                            Debug.Log($"[PaddleChangeController] Sword 패들 활성화됨: {paddle_sword.name}");
                    }
                    else if (enableDebugLogs)
                    {
                        Debug.LogError("[PaddleChangeController] paddle_sword가 null입니다!");
                    }
                    break;
                case 2: // Glove (both left and right)
                    if (paddle_glove_left != null && paddle_glove_right != null)
                    {
                        paddle_glove_left.SetActive(true);
                        paddle_glove_right.SetActive(true);
                        activationSuccess = true;
                        if (enableDebugLogs)
                            Debug.Log($"[PaddleChangeController] Glove 패들들 활성화됨: {paddle_glove_left.name}, {paddle_glove_right.name}");
                    }
                    else if (enableDebugLogs)
                    {
                        if (paddle_glove_left == null)
                            Debug.LogError("[PaddleChangeController] paddle_glove_left가 null입니다!");
                        if (paddle_glove_right == null)
                            Debug.LogError("[PaddleChangeController] paddle_glove_right가 null입니다!");
                    }
                    break;
                default:
                    if (enableDebugLogs)
                        Debug.LogError($"[PaddleChangeController] 잘못된 패들 인덱스: {index}");
                    break;
            }

            if (enableDebugLogs)
            {
                string playerType = IsLocalPlayer() ? "로컬" : "원격";
                Debug.Log($"[PaddleChangeController] {playerType} 플레이어 패들 변경 완료: {(activationSuccess ? "성공" : "실패")}");
            }
        }

        /// <summary>
        /// 인덱스로 패들 이름 가져오기
        /// </summary>
        private string GetPaddleNameByIndex(int index)
        {
            switch (index)
            {
                case 0: return "Racket";
                case 1: return "Sword";
                case 2: return "Glove (Both Hands)";
                default: return "Unknown";
            }
        }

        /// <summary>
        /// 현재 패들 이름 가져오기
        /// </summary>
        private string GetCurrentPaddleName()
        {
            switch (_currentPaddleIndex)
            {
                case 0: return "Racket";
                case 1: return "Sword";
                case 2: return "Glove (Both Hands)";
                default: return "Unknown";
            }
        }

        // Context Menu로 에디터에서 테스트 가능
        [ContextMenu("Next Paddle")]
        private void TestNextPaddle()
        {
            ChangeToNextPaddle();
        }

        [ContextMenu("Previous Paddle")]
        private void TestPreviousPaddle()
        {
            ChangeToPreviousPaddle();
        }
    }
} 