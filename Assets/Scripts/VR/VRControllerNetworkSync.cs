using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using Photon.Pun;
using DeepongVR.Court;

namespace DeepongVR.Network
{
    /// <summary>
    /// VR 컨트롤러의 움직임, 버튼 상태, 햅틱 피드백을 네트워크로 동기화합니다.
    /// 양손 컨트롤러의 위치/회전, 버튼 입력, 패들 상태 등을 실시간으로 전송합니다.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class VRControllerNetworkSync : MonoBehaviourPunCallbacks, IPunObservable
    {
        [Header("컨트롤러 참조")]
        [SerializeField] private Transform leftController;
        [SerializeField] private Transform rightController;
        [SerializeField] private Transform headset;
        
        [Header("시각화 오브젝트")]
        [SerializeField] private GameObject leftControllerVisualizer;
        [SerializeField] private GameObject rightControllerVisualizer;
        [SerializeField] private bool createVisualizersForRemotePlayers = true;
        [SerializeField] private Material leftControllerMaterial;
        [SerializeField] private Material rightControllerMaterial;
        
        [Header("동기화 설정")]
        [SerializeField] private float positionLerpRate = 15f;
        [SerializeField] private float rotationLerpRate = 15f;
        [SerializeField] private float teleportThreshold = 2f;
        [SerializeField] private bool syncButtonStates = true;
        [SerializeField] private bool syncHapticFeedback = true;
        
        [Header("최적화 설정")]
        [SerializeField] private float sendRate = 20f; // 초당 전송 횟수
        [SerializeField] private float positionThreshold = 0.01f; // 위치 변화 임계값
        [SerializeField] private float rotationThreshold = 1f; // 회전 변화 임계값 (도)
        
        // 네트워크로 수신한 데이터
        private Vector3 networkLeftPos;
        private Quaternion networkLeftRot;
        private Vector3 networkRightPos;
        private Quaternion networkRightRot;
        
        // 버튼 상태
        private bool networkLeftTrigger;
        private bool networkLeftGrip;
        private bool networkLeftPrimary;
        private bool networkLeftSecondary;
        private bool networkRightTrigger;
        private bool networkRightGrip;
        private bool networkRightPrimary;
        private bool networkRightSecondary;
        
        // 패들 상태
        private int networkPaddleIndex = 0;
        
        // 로컬 이전 상태 (변화 감지용)
        private Vector3 lastLeftPos;
        private Quaternion lastLeftRot;
        private Vector3 lastRightPos;
        private Quaternion lastRightRot;
        private float lastSendTime;
        
        // 입력 액션
        private InputAction leftTriggerAction;
        private InputAction leftGripAction;
        private InputAction leftPrimaryAction;
        private InputAction leftSecondaryAction;
        private InputAction rightTriggerAction;
        private InputAction rightGripAction;
        private InputAction rightPrimaryAction;
        private InputAction rightSecondaryAction;
        
        // 컴포넌트 참조
        private PaddleChangeController paddleController;
        
        // 햅틱 플레이어
        private XRBaseController leftXRController;
        private XRBaseController rightXRController;
        
        // 원격 플레이어 상태
        private bool hasReceivedInitialData = false;

        #region Unity 생명주기

        void Awake()
        {
            
            // PaddleChangeController를 더 확실하게 찾기
            paddleController = GetComponent<PaddleChangeController>();
            if (paddleController == null)
            {
                paddleController = GetComponentInChildren<PaddleChangeController>();
            }
            
            string playerName = (photonView.Owner != null) ? photonView.Owner.NickName : "Unknown";
            
            if (paddleController != null)
            {
                Debug.Log($"[VRControllerNetworkSync] PaddleChangeController 찾음: {paddleController.gameObject.name} (플레이어: {playerName})");
            }
            else
            {
                Debug.LogWarning($"[VRControllerNetworkSync] PaddleChangeController를 찾을 수 없습니다! (플레이어: {playerName})");
            }

            // XR 컨트롤러 참조 찾기
            FindXRControllers();
            
            // 원격 플레이어용 시각화 오브젝트 생성
            if (!photonView.IsMine && createVisualizersForRemotePlayers)
            {
                CreateControllerVisualizers();
            }
        }

        void Start()
        {
            if (photonView.IsMine)
            {
                SetupInputActions();
                InitializeLocalPlayer();
            }
            else
            {
                InitializeRemotePlayer();
            }
        }

        void Update()
        {
            if (photonView.IsMine)
            {
                // 로컬 플레이어: 입력 처리
                HandleLocalInput();
            }
            else
            {
                // 원격 플레이어: 동기화된 데이터로 업데이트
                UpdateRemotePlayer();
            }
        }

        void OnDestroy()
        {
            CleanupInputActions();
            DestroyControllerVisualizers();
        }

        #endregion

        #region 초기화

        private void FindXRControllers()
        {
            // XR 컨트롤러 컴포넌트 찾기
            if (leftController != null)
            {
                leftXRController = leftController.GetComponent<XRBaseController>();
            }
            
            if (rightController != null)
            {
                rightXRController = rightController.GetComponent<XRBaseController>();
            }
        }

        private void SetupInputActions()
        {
            // 왼손 컨트롤러 입력 액션 설정
            leftTriggerAction = new InputAction(binding: "<XRController>{LeftHand}/triggerPressed");
            leftGripAction = new InputAction(binding: "<XRController>{LeftHand}/gripPressed");
            leftPrimaryAction = new InputAction(binding: "<XRController>{LeftHand}/primaryButton");
            leftSecondaryAction = new InputAction(binding: "<XRController>{LeftHand}/secondaryButton");
            
            // 오른손 컨트롤러 입력 액션 설정
            rightTriggerAction = new InputAction(binding: "<XRController>{RightHand}/triggerPressed");
            rightGripAction = new InputAction(binding: "<XRController>{RightHand}/gripPressed");
            rightPrimaryAction = new InputAction(binding: "<XRController>{RightHand}/primaryButton");
            rightSecondaryAction = new InputAction(binding: "<XRController>{RightHand}/secondaryButton");
            
            // 액션 활성화
            leftTriggerAction.Enable();
            leftGripAction.Enable();
            leftPrimaryAction.Enable();
            leftSecondaryAction.Enable();
            rightTriggerAction.Enable();
            rightGripAction.Enable();
            rightPrimaryAction.Enable();
            rightSecondaryAction.Enable();
            
            Debug.Log("[VRControllerNetworkSync] 입력 액션 설정 완료");
        }

        private void CleanupInputActions()
        {
            leftTriggerAction?.Dispose();
            leftGripAction?.Dispose();
            leftPrimaryAction?.Dispose();
            leftSecondaryAction?.Dispose();
            rightTriggerAction?.Dispose();
            rightGripAction?.Dispose();
            rightPrimaryAction?.Dispose();
            rightSecondaryAction?.Dispose();
        }

        private void InitializeLocalPlayer()
        {
            if (leftController != null)
            {
                lastLeftPos = leftController.position;
                lastLeftRot = leftController.rotation;
            }
            
            if (rightController != null)
            {
                lastRightPos = rightController.position;
                lastRightRot = rightController.rotation;
            }
            
            lastSendTime = Time.time;
            
            Debug.Log("[VRControllerNetworkSync] 로컬 플레이어 초기화 완료");
        }

        private void InitializeRemotePlayer()
        {
            // 원격 플레이어의 패들 컨트롤러는 활성화 상태로 유지하되, 입력만 비활성화
            if (paddleController != null)
            {
                // PaddleChangeController의 입력 처리를 비활성화하는 것이 아니라
                // 네트워크에서 받은 데이터로만 패들을 변경하도록 함
                // paddleController.enabled = false; // 이 줄을 제거
                Debug.Log("[VRControllerNetworkSync] 원격 플레이어 패들 컨트롤러는 활성화 상태 유지");
            }
            
            Debug.Log("[VRControllerNetworkSync] 원격 플레이어 초기화 완료");
        }

        #endregion

        #region 로컬 플레이어 처리

        private void HandleLocalInput()
        {
            // 버튼 상태 업데이트
            if (syncButtonStates)
            {
                UpdateButtonStates();
            }
            
            // 패들 상태 업데이트
            if (paddleController != null)
            {
                int currentPaddleIndex = paddleController.CurrentPaddleIndex;
                if (currentPaddleIndex != networkPaddleIndex)
                {
                    Debug.Log($"[VRControllerNetworkSync] 로컬 패들 변경 감지: {networkPaddleIndex} → {currentPaddleIndex}");
                    networkPaddleIndex = currentPaddleIndex;
                    
                    // RPC로 다른 플레이어들에게 패들 변경 전송
                    photonView.RPC("OnPaddleChanged", RpcTarget.Others, currentPaddleIndex);
                }
            }
        }

        private void UpdateButtonStates()
        {
            // 왼손 버튼 상태
            bool currentLeftTrigger = leftTriggerAction.ReadValue<float>() > 0.5f;
            bool currentLeftGrip = leftGripAction.ReadValue<float>() > 0.5f;
            bool currentLeftPrimary = leftPrimaryAction.ReadValue<float>() > 0.5f;
            bool currentLeftSecondary = leftSecondaryAction.ReadValue<float>() > 0.5f;
            
            // 오른손 버튼 상태
            bool currentRightTrigger = rightTriggerAction.ReadValue<float>() > 0.5f;
            bool currentRightGrip = rightGripAction.ReadValue<float>() > 0.5f;
            bool currentRightPrimary = rightPrimaryAction.ReadValue<float>() > 0.5f;
            bool currentRightSecondary = rightSecondaryAction.ReadValue<float>() > 0.5f;
            
            // 버튼 상태 변화 감지 및 이벤트 전송
            if (currentLeftTrigger != networkLeftTrigger)
            {
                networkLeftTrigger = currentLeftTrigger;
                if (currentLeftTrigger) SendButtonEvent("LeftTrigger", true);
            }
            
            if (currentRightTrigger != networkRightTrigger)
            {
                networkRightTrigger = currentRightTrigger;
                if (currentRightTrigger) SendButtonEvent("RightTrigger", true);
            }
            
            // 다른 버튼들도 동일하게 처리
            networkLeftGrip = currentLeftGrip;
            networkLeftPrimary = currentLeftPrimary;
            networkLeftSecondary = currentLeftSecondary;
            networkRightGrip = currentRightGrip;
            networkRightPrimary = currentRightPrimary;
            networkRightSecondary = currentRightSecondary;
        }

        private void SendButtonEvent(string buttonName, bool pressed)
        {
            // 버튼 이벤트를 RPC로 전송
            photonView.RPC("OnButtonEvent", RpcTarget.Others, buttonName, pressed);
        }

        #endregion

        #region 원격 플레이어 처리

        private void UpdateRemotePlayer()
        {
            if (!hasReceivedInitialData) return;
            
            float deltaTime = Time.deltaTime;
            
            // 왼손 컨트롤러 업데이트
            if (leftControllerVisualizer != null)
            {
                UpdateControllerVisualizer(leftControllerVisualizer, networkLeftPos, networkLeftRot, deltaTime);
            }
            
            // 오른손 컨트롤러 업데이트
            if (rightControllerVisualizer != null)
            {
                UpdateControllerVisualizer(rightControllerVisualizer, networkRightPos, networkRightRot, deltaTime);
            }
        }

        private void UpdateControllerVisualizer(GameObject visualizer, Vector3 targetPos, Quaternion targetRot, float deltaTime)
        {
            Transform transform = visualizer.transform;
            
            // 순간이동 임계값 확인
            float distance = Vector3.Distance(transform.position, targetPos);
            if (distance > teleportThreshold)
            {
                // 거리가 너무 크면 순간이동
                transform.position = targetPos;
                transform.rotation = targetRot;
            }
            else
            {
                // 부드러운 보간
                transform.position = Vector3.Lerp(transform.position, targetPos, deltaTime * positionLerpRate);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, deltaTime * rotationLerpRate);
            }
        }

        #endregion

        #region 시각화 오브젝트

        private void CreateControllerVisualizers()
        {
            // 왼손 컨트롤러 시각화 오브젝트
            if (leftControllerVisualizer == null)
            {
                leftControllerVisualizer = CreateControllerVisualizer("LeftController", Color.red);
            }
            
            // 오른손 컨트롤러 시각화 오브젝트
            if (rightControllerVisualizer == null)
            {
                rightControllerVisualizer = CreateControllerVisualizer("RightController", Color.blue);
            }
            
            Debug.Log($"[VRControllerNetworkSync] {photonView.Owner.NickName}의 컨트롤러 시각화 오브젝트 생성 완료");
        }

        private GameObject CreateControllerVisualizer(string name, Color color)
        {
            GameObject visualizer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visualizer.name = $"{photonView.Owner.NickName}_{name}";
            visualizer.transform.localScale = new Vector3(0.05f, 0.1f, 0.05f);
            
            // 머티리얼 설정
            Renderer renderer = visualizer.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                mat.SetFloat("_Metallic", 0.3f);
                mat.SetFloat("_Smoothness", 0.7f);
                renderer.material = mat;
            }
            
            // 콜라이더 제거 (시각화용)
            Collider collider = visualizer.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyImmediate(collider);
            }
            
            return visualizer;
        }

        private void DestroyControllerVisualizers()
        {
            if (leftControllerVisualizer != null)
            {
                DestroyImmediate(leftControllerVisualizer);
                leftControllerVisualizer = null;
            }
            
            if (rightControllerVisualizer != null)
            {
                DestroyImmediate(rightControllerVisualizer);
                rightControllerVisualizer = null;
            }
        }

        #endregion

        #region 네트워크 동기화 (IPunObservable)

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // 데이터 전송
                SendControllerData(stream);
            }
            else
            {
                // 데이터 수신
                ReceiveControllerData(stream);
            }
        }

        private void SendControllerData(PhotonStream stream)
        {
            // 왼손 컨트롤러 위치/회전
            if (leftController != null)
            {
                stream.SendNext(leftController.position);
                stream.SendNext(leftController.rotation);
            }
            else
            {
                stream.SendNext(Vector3.zero);
                stream.SendNext(Quaternion.identity);
            }
            
            // 오른손 컨트롤러 위치/회전
            if (rightController != null)
            {
                stream.SendNext(rightController.position);
                stream.SendNext(rightController.rotation);
            }
            else
            {
                stream.SendNext(Vector3.zero);
                stream.SendNext(Quaternion.identity);
            }
            
            // 버튼 상태 (비트 패킹으로 최적화)
            if (syncButtonStates)
            {
                byte buttonStates = 0;
                if (networkLeftTrigger) buttonStates |= 1;
                if (networkLeftGrip) buttonStates |= 2;
                if (networkLeftPrimary) buttonStates |= 4;
                if (networkLeftSecondary) buttonStates |= 8;
                if (networkRightTrigger) buttonStates |= 16;
                if (networkRightGrip) buttonStates |= 32;
                if (networkRightPrimary) buttonStates |= 64;
                if (networkRightSecondary) buttonStates |= 128;
                
                stream.SendNext(buttonStates);
            }
            else
            {
                stream.SendNext((byte)0);
            }
            
            // 패들 인덱스
            stream.SendNext(networkPaddleIndex);
        }

        private void ReceiveControllerData(PhotonStream stream)
        {
            // 왼손 컨트롤러 위치/회전
            networkLeftPos = (Vector3)stream.ReceiveNext();
            networkLeftRot = (Quaternion)stream.ReceiveNext();
            
            // 오른손 컨트롤러 위치/회전
            networkRightPos = (Vector3)stream.ReceiveNext();
            networkRightRot = (Quaternion)stream.ReceiveNext();
            
            // 버튼 상태
            byte buttonStates = (byte)stream.ReceiveNext();
            networkLeftTrigger = (buttonStates & 1) != 0;
            networkLeftGrip = (buttonStates & 2) != 0;
            networkLeftPrimary = (buttonStates & 4) != 0;
            networkLeftSecondary = (buttonStates & 8) != 0;
            networkRightTrigger = (buttonStates & 16) != 0;
            networkRightGrip = (buttonStates & 32) != 0;
            networkRightPrimary = (buttonStates & 64) != 0;
            networkRightSecondary = (buttonStates & 128) != 0;
            
            // 패들 인덱스
            int receivedPaddleIndex = (int)stream.ReceiveNext();
            
            // 패들 변경 감지만 하고 실제 적용은 RPC로 처리
            if (receivedPaddleIndex != networkPaddleIndex)
            {
                Debug.Log($"[VRControllerNetworkSync] 패들 인덱스 업데이트: {networkPaddleIndex} → {receivedPaddleIndex}");
                networkPaddleIndex = receivedPaddleIndex;
            }
            
            hasReceivedInitialData = true;
        }

        #endregion

        #region RPC 메서드

        [PunRPC]
        void OnButtonEvent(string buttonName, bool pressed)
        {
            // 원격 플레이어의 버튼 이벤트 처리
            Debug.Log($"[VRControllerNetworkSync] 원격 플레이어 버튼 이벤트: {buttonName} = {pressed}");
            
            // 햅틱 피드백 처리
            if (syncHapticFeedback && pressed)
            {
                TriggerHapticFeedback(buttonName);
            }
        }

        [PunRPC]
        void TriggerHapticFeedbackRPC(string controllerName, float intensity, float duration)
        {
            // 원격 햅틱 피드백 처리 (시각적 효과 등)
            Debug.Log($"[VRControllerNetworkSync] 원격 햅틱 피드백: {controllerName}, 강도: {intensity}, 지속시간: {duration}");
        }

        [PunRPC]
        void OnPaddleChanged(int newPaddleIndex)
        {
            // 원격 플레이어의 패들 변경 처리
            if (paddleController != null)
            {
                if (paddleController.enabled)
                {
                    int previousIndex = paddleController.CurrentPaddleIndex;
                    paddleController.CurrentPaddleIndex = newPaddleIndex;
                    Debug.Log($"[VRControllerNetworkSync] 원격 플레이어 패들 적용 완료: {previousIndex} → {paddleController.CurrentPaddleIndex}");
                }
                else
                {
                    Debug.LogWarning("[VRControllerNetworkSync] PaddleController가 비활성화되어 있어 패들 변경을 적용할 수 없음");
                }
            }
            else
            {
                Debug.LogWarning("[VRControllerNetworkSync] PaddleController 참조가 없어 패들 변경을 적용할 수 없음");
            }
        }

        #endregion

        #region 햅틱 피드백

        private void TriggerHapticFeedback(string buttonName)
        {
            // 버튼에 따른 햅틱 피드백 (시각적 표현)
            if (buttonName.Contains("Left") && leftControllerVisualizer != null)
            {
                StartCoroutine(FlashController(leftControllerVisualizer));
            }
            else if (buttonName.Contains("Right") && rightControllerVisualizer != null)
            {
                StartCoroutine(FlashController(rightControllerVisualizer));
            }
        }

        private IEnumerator FlashController(GameObject controller)
        {
            Renderer renderer = controller.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color originalColor = renderer.material.color;
                renderer.material.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                renderer.material.color = originalColor;
            }
        }

        /// <summary>
        /// 로컬 햅틱 피드백을 네트워크로 전송
        /// </summary>
        public void SendHapticFeedback(string controllerName, float intensity = 0.5f, float duration = 0.1f)
        {
            if (photonView.IsMine && syncHapticFeedback)
            {
                photonView.RPC("TriggerHapticFeedbackRPC", RpcTarget.Others, controllerName, intensity, duration);
            }
        }

        #endregion

        #region 공용 메서드

        /// <summary>
        /// 원격 플레이어의 컨트롤러 위치 가져오기
        /// </summary>
        public Vector3 GetRemoteControllerPosition(bool isLeftHand)
        {
            return isLeftHand ? networkLeftPos : networkRightPos;
        }

        /// <summary>
        /// 원격 플레이어의 컨트롤러 회전 가져오기
        /// </summary>
        public Quaternion GetRemoteControllerRotation(bool isLeftHand)
        {
            return isLeftHand ? networkLeftRot : networkRightRot;
        }

        /// <summary>
        /// 원격 플레이어의 버튼 상태 가져오기
        /// </summary>
        public bool GetRemoteButtonState(string buttonName)
        {
            switch (buttonName.ToLower())
            {
                case "lefttrigger": return networkLeftTrigger;
                case "leftgrip": return networkLeftGrip;
                case "leftprimary": return networkLeftPrimary;
                case "leftsecondary": return networkLeftSecondary;
                case "righttrigger": return networkRightTrigger;
                case "rightgrip": return networkRightGrip;
                case "rightprimary": return networkRightPrimary;
                case "rightsecondary": return networkRightSecondary;
                default: return false;
            }
        }

        #endregion
    }
} 