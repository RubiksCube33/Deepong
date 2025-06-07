using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

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

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        // 내부 변수들
        private InputAction primaryButtonAction;
        private int currentPaddleIndex = 0;
        private GameObject[] paddles;

        void Start()
        {
            // 패들 배열 초기화
            paddles = new GameObject[] { paddle_racket, paddle_sword, paddle_glove_left, paddle_glove_right };
            
            // 초기 패들 설정 (첫 번째만 활성화)
            SetActivePaddle(0);
        }

        void OnEnable()
        {
            SetupInputAction();
        }

        void OnDisable()
        {
            CleanupInputAction();
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
                // 다음 패들로 변경
                ChangeToNextPaddle();
            }
        }

        /// <summary>
        /// 다음 패들로 변경
        /// </summary>
        public void ChangeToNextPaddle()
        {
            currentPaddleIndex = (currentPaddleIndex + 1) % 3; // 패들 타입은 3개 (racket, sword, glove)
            SetActivePaddle(currentPaddleIndex);

            if (enableDebugLogs)
            {
                string paddleName = GetCurrentPaddleName();
                Debug.Log($"[PaddleChangeController] 패들 변경: {paddleName}");
            }
        }

        /// <summary>
        /// 특정 패들을 활성화하고 나머지는 비활성화
        /// </summary>
        private void SetActivePaddle(int index)
        {
            // 모든 패들 비활성화
            for (int i = 0; i < paddles.Length; i++)
            {
                if (paddles[i] != null)
                {
                    paddles[i].SetActive(false);
                }
            }

            // 선택된 패들 활성화
            switch (index)
            {
                case 0: // Racket
                    if (paddle_racket != null)
                        paddle_racket.SetActive(true);
                    break;
                case 1: // Sword
                    if (paddle_sword != null)
                        paddle_sword.SetActive(true);
                    break;
                case 2: // Glove (both left and right)
                    if (paddle_glove_left != null)
                        paddle_glove_left.SetActive(true);
                    if (paddle_glove_right != null)
                        paddle_glove_right.SetActive(true);
                    break;
            }
        }

        /// <summary>
        /// 현재 패들 이름 가져오기
        /// </summary>
        private string GetCurrentPaddleName()
        {
            switch (currentPaddleIndex)
            {
                case 0: return "Racket";
                case 1: return "Sword";
                case 2: return "Glove (Both Hands)";
                default: return "Unknown";
            }
        }
    }
} 