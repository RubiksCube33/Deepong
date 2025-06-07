using UnityEngine;
using DeepongVR.Network;
using Photon.Pun;

namespace DeepongVR.Examples
{
    /// <summary>
    /// VRControllerNetworkSync를 사용하는 방법을 보여주는 예제입니다.
    /// Player_Origin 프리팹에 이 컴포넌트를 추가하여 VR 컨트롤러 동기화를 확인할 수 있습니다.
    /// </summary>
    public class VRControllerSyncExample : MonoBehaviourPunCallbacks
    {
        [Header("테스트 설정")]
        [SerializeField] private bool enableDebugVisualization = true;
        [SerializeField] private bool showButtonStates = true;
        [SerializeField] private GameObject debugUI;
        
        private VRControllerNetworkSync controllerSync;
        private float lastUpdateTime;
        private GUIStyle guiStyle;
        
        void Start()
        {
            // VRControllerNetworkSync 컴포넌트 찾기
            controllerSync = GetComponent<VRControllerNetworkSync>();
            if (controllerSync == null)
            {
                Debug.LogError("VRControllerNetworkSync 컴포넌트를 찾을 수 없습니다!");
                return;
            }
            
            // GUI 스타일 설정
            guiStyle = new GUIStyle();
            guiStyle.fontSize = 20;
            guiStyle.normal.textColor = Color.white;
            
            Debug.Log($"[VRControllerSyncExample] 초기화 완료 - 플레이어: {photonView.Owner.NickName}");
        }
        
        void Update()
        {
            if (controllerSync == null) return;
            
            // 로컬 플레이어인 경우 입력 테스트
            if (photonView.IsMine)
            {
                TestLocalInput();
            }
            
            // 원격 플레이어인 경우 동기화된 데이터 표시
            else
            {
                TestRemoteData();
            }
        }
        
        /// <summary>
        /// 로컬 플레이어 입력 테스트
        /// </summary>
        private void TestLocalInput()
        {
            // 예제: 오른쪽 트리거를 누르면 햅틱 피드백 전송
            if (Input.GetKeyDown(KeyCode.Space)) // 테스트용 키보드 입력
            {
                Debug.Log("[VRControllerSyncExample] 햅틱 피드백 전송 (오른손)");
                controllerSync.SendHapticFeedback("RightController", 0.7f, 0.2f);
            }
            
            // 예제: 왼쪽 트리거를 누르면 햅틱 피드백 전송
            if (Input.GetKeyDown(KeyCode.LeftShift)) // 테스트용 키보드 입력
            {
                Debug.Log("[VRControllerSyncExample] 햅틱 피드백 전송 (왼손)");
                controllerSync.SendHapticFeedback("LeftController", 0.5f, 0.15f);
            }
        }
        
        /// <summary>
        /// 원격 플레이어 데이터 테스트
        /// </summary>
        private void TestRemoteData()
        {
            // 1초마다 원격 플레이어의 컨트롤러 위치 출력
            if (Time.time - lastUpdateTime > 1f)
            {
                Vector3 leftPos = controllerSync.GetRemoteControllerPosition(true);
                Vector3 rightPos = controllerSync.GetRemoteControllerPosition(false);
                
                if (enableDebugVisualization)
                {
                    Debug.Log($"[VRControllerSyncExample] 원격 플레이어 컨트롤러 위치 - 왼손: {leftPos}, 오른손: {rightPos}");
                }
                
                lastUpdateTime = Time.time;
            }
        }
        
        /// <summary>
        /// GUI로 버튼 상태 표시 (디버그용)
        /// </summary>
        void OnGUI()
        {
            if (!showButtonStates || controllerSync == null) return;
            
            // 원격 플레이어의 버튼 상태만 표시
            if (!photonView.IsMine)
            {
                float yOffset = 10f;
                GUI.Label(new Rect(10, yOffset, 400, 30), $"원격 플레이어: {photonView.Owner.NickName}", guiStyle);
                yOffset += 35f;
                
                // 왼손 버튼 상태
                GUI.Label(new Rect(10, yOffset, 200, 25), "왼손 버튼:", guiStyle);
                yOffset += 30f;
                
                GUI.Label(new Rect(20, yOffset, 150, 20), $"트리거: {controllerSync.GetRemoteButtonState("LeftTrigger")}", guiStyle);
                yOffset += 25f;
                GUI.Label(new Rect(20, yOffset, 150, 20), $"그립: {controllerSync.GetRemoteButtonState("LeftGrip")}", guiStyle);
                yOffset += 25f;
                GUI.Label(new Rect(20, yOffset, 150, 20), $"A버튼: {controllerSync.GetRemoteButtonState("LeftPrimary")}", guiStyle);
                yOffset += 25f;
                GUI.Label(new Rect(20, yOffset, 150, 20), $"B버튼: {controllerSync.GetRemoteButtonState("LeftSecondary")}", guiStyle);
                yOffset += 35f;
                
                // 오른손 버튼 상태
                GUI.Label(new Rect(10, yOffset, 200, 25), "오른손 버튼:", guiStyle);
                yOffset += 30f;
                
                GUI.Label(new Rect(20, yOffset, 150, 20), $"트리거: {controllerSync.GetRemoteButtonState("RightTrigger")}", guiStyle);
                yOffset += 25f;
                GUI.Label(new Rect(20, yOffset, 150, 20), $"그립: {controllerSync.GetRemoteButtonState("RightGrip")}", guiStyle);
                yOffset += 25f;
                GUI.Label(new Rect(20, yOffset, 150, 20), $"A버튼: {controllerSync.GetRemoteButtonState("RightPrimary")}", guiStyle);
                yOffset += 25f;
                GUI.Label(new Rect(20, yOffset, 150, 20), $"B버튼: {controllerSync.GetRemoteButtonState("RightSecondary")}", guiStyle);
                
                // 컨트롤러 위치 정보
                yOffset += 35f;
                Vector3 leftPos = controllerSync.GetRemoteControllerPosition(true);
                Vector3 rightPos = controllerSync.GetRemoteControllerPosition(false);
                
                GUI.Label(new Rect(10, yOffset, 200, 25), "컨트롤러 위치:", guiStyle);
                yOffset += 30f;
                GUI.Label(new Rect(20, yOffset, 400, 20), $"왼손: ({leftPos.x:F2}, {leftPos.y:F2}, {leftPos.z:F2})", guiStyle);
                yOffset += 25f;
                GUI.Label(new Rect(20, yOffset, 400, 20), $"오른손: ({rightPos.x:F2}, {rightPos.y:F2}, {rightPos.z:F2})", guiStyle);
            }
            else
            {
                // 로컬 플레이어 안내
                GUI.Label(new Rect(10, 10, 400, 30), $"로컬 플레이어: {photonView.Owner.NickName}", guiStyle);
                GUI.Label(new Rect(10, 45, 400, 25), "Space: 오른손 햅틱 피드백", guiStyle);
                GUI.Label(new Rect(10, 70, 400, 25), "Left Shift: 왼손 햅틱 피드백", guiStyle);
            }
        }
        
        /// <summary>
        /// 컨트롤러 동기화 품질 테스트
        /// </summary>
        [ContextMenu("컨트롤러 동기화 테스트")]
        public void TestControllerSync()
        {
            if (controllerSync == null)
            {
                Debug.LogError("VRControllerNetworkSync 컴포넌트가 없습니다!");
                return;
            }
            
            if (photonView.IsMine)
            {
                Debug.Log("[VRControllerSyncExample] 로컬 플레이어 - 모든 햅틱 피드백 테스트 시작");
                StartCoroutine(TestAllHapticFeedback());
            }
            else
            {
                Debug.Log("[VRControllerSyncExample] 원격 플레이어 - 버튼 상태 테스트");
                TestAllButtonStates();
            }
        }
        
        /// <summary>
        /// 모든 햅틱 피드백 테스트 (로컬 플레이어용)
        /// </summary>
        private System.Collections.IEnumerator TestAllHapticFeedback()
        {
            string[] controllers = { "LeftController", "RightController" };
            float[] intensities = { 0.3f, 0.5f, 0.7f, 1.0f };
            
            foreach (string controller in controllers)
            {
                foreach (float intensity in intensities)
                {
                    Debug.Log($"햅틱 테스트: {controller}, 강도: {intensity}");
                    controllerSync.SendHapticFeedback(controller, intensity, 0.2f);
                    yield return new WaitForSeconds(0.5f);
                }
            }
            
            Debug.Log("햅틱 피드백 테스트 완료");
        }
        
        /// <summary>
        /// 모든 버튼 상태 테스트 (원격 플레이어용)
        /// </summary>
        private void TestAllButtonStates()
        {
            string[] buttons = { 
                "LeftTrigger", "LeftGrip", "LeftPrimary", "LeftSecondary",
                "RightTrigger", "RightGrip", "RightPrimary", "RightSecondary"
            };
            
            Debug.Log("=== 원격 플레이어 버튼 상태 ===");
            foreach (string button in buttons)
            {
                bool state = controllerSync.GetRemoteButtonState(button);
                Debug.Log($"{button}: {state}");
            }
            
            Vector3 leftPos = controllerSync.GetRemoteControllerPosition(true);
            Vector3 rightPos = controllerSync.GetRemoteControllerPosition(false);
            Debug.Log($"왼손 위치: {leftPos}");
            Debug.Log($"오른손 위치: {rightPos}");
        }
    }
} 