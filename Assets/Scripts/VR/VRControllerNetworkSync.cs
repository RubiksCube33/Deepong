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
    /// 양손 컨트롤러의 위치/회전, 버튼 입력을 실시간으로 전송합니다.
    /// 패들 동기화는 PaddleChangeController가 독립적으로 처리합니다.
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
        private VRHumanoidController vrController;
        
        // 햅틱 플레이어
        private XRBaseController leftXRController;
        private XRBaseController rightXRController;
        
        // 원격 플레이어 상태
        private bool hasReceivedInitialData = false;

        #region Unity 생명주기

        void Awake()
        {
            // 컴포넌트 참조 가져오기
            vrController = GetComponent<VRHumanoidController>();
            
            string playerName = (photonView.Owner != null) ? photonView.Owner.NickName : "Unknown";
            Debug.Log($"[VRControllerNetworkSync] 초기화 중... (플레이어: {playerName})");
            
            // VRHumanoidController에서 컨트롤러 참조 가져오기
            if (vrController != null)
            {
                leftController = vrController.LeftHandController;
                rightController = vrController.RightHandController;
                headset = vrController.Headset;
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

        void OnEnable()
        {
            if (photonView.IsMine)
            {
                EnableInputActions();
            }
        }

        void OnDisable()
        {
            DisableInputActions();
        }

        void Update()
        {
            if (photonView.IsMine)
            {
                HandleLocalInput();
            }
            else
            {
                HandleRemotePlayerUpdates();
            }
        }

        #endregion

        #region 초기화

        private void FindXRControllers()
        {
            XRBaseController[] controllers = FindObjectsOfType<XRBaseController>();
            
            foreach (var controller in controllers)
            {
                if (controller.name.ToLower().Contains("left"))
                {
                    leftXRController = controller;
                }
                else if (controller.name.ToLower().Contains("right"))
                {
                    rightXRController = controller;
                }
            }
            
            Debug.Log($"[VRControllerNetworkSync] XR 컨트롤러 참조 완료 - Left: {(leftXRController != null ? "찾음" : "없음")}, Right: {(rightXRController != null ? "찾음" : "없음")}");
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
            // 원격 플레이어의 입력 관련 컴포넌트 비활성화
            if (vrController != null)
            {
                vrController.enabled = false;
            }
            
            Debug.Log("[VRControllerNetworkSync] 원격 플레이어 초기화 완료 - VR 입력 비활성화됨");
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
        }

        private void UpdateButtonStates()
        {
            // 왼손 버튼 상태 업데이트
            bool newLeftTrigger = (leftTriggerAction?.ReadValue<float>() ?? 0f) > 0.5f;
            bool newLeftGrip = (leftGripAction?.ReadValue<float>() ?? 0f) > 0.5f;
            bool newLeftPrimary = leftPrimaryAction?.IsPressed() ?? false;
            bool newLeftSecondary = leftSecondaryAction?.IsPressed() ?? false;
            
            // 오른손 버튼 상태 업데이트
            bool newRightTrigger = (rightTriggerAction?.ReadValue<float>() ?? 0f) > 0.5f;
            bool newRightGrip = (rightGripAction?.ReadValue<float>() ?? 0f) > 0.5f;
            bool newRightPrimary = rightPrimaryAction?.IsPressed() ?? false;
            bool newRightSecondary = rightSecondaryAction?.IsPressed() ?? false;
            
            // 변경 감지 및 RPC 전송
            CheckButtonChange(ref networkLeftTrigger, newLeftTrigger, "LeftTrigger");
            CheckButtonChange(ref networkLeftGrip, newLeftGrip, "LeftGrip");
            CheckButtonChange(ref networkLeftPrimary, newLeftPrimary, "LeftPrimary");
            CheckButtonChange(ref networkLeftSecondary, newLeftSecondary, "LeftSecondary");
            CheckButtonChange(ref networkRightTrigger, newRightTrigger, "RightTrigger");
            CheckButtonChange(ref networkRightGrip, newRightGrip, "RightGrip");
            CheckButtonChange(ref networkRightPrimary, newRightPrimary, "RightPrimary");
            CheckButtonChange(ref networkRightSecondary, newRightSecondary, "RightSecondary");
        }

        private void CheckButtonChange(ref bool currentState, bool newState, string buttonName)
        {
            if (currentState != newState)
            {
                currentState = newState;
                
                // RPC로 버튼 이벤트 전송
                photonView.RPC("OnButtonEvent", RpcTarget.Others, buttonName, newState);
            }
        }

        #endregion

        #region 원격 플레이어 처리

        private void HandleRemotePlayerUpdates()
        {
            if (!hasReceivedInitialData) return;
            
            // 위치/회전 보간
            UpdateControllerPositions();
        }

        private void UpdateControllerPositions()
        {
            if (leftControllerVisualizer != null)
            {
                float distance = Vector3.Distance(leftControllerVisualizer.transform.position, networkLeftPos);
                
                if (distance > teleportThreshold)
                {
                    // 순간이동
                    leftControllerVisualizer.transform.position = networkLeftPos;
                    leftControllerVisualizer.transform.rotation = networkLeftRot;
                }
                else
                {
                    // 부드러운 보간
                    leftControllerVisualizer.transform.position = Vector3.Lerp(
                        leftControllerVisualizer.transform.position, 
                        networkLeftPos, 
                        positionLerpRate * Time.deltaTime
                    );
                    leftControllerVisualizer.transform.rotation = Quaternion.Lerp(
                        leftControllerVisualizer.transform.rotation, 
                        networkLeftRot, 
                        rotationLerpRate * Time.deltaTime
                    );
                }
            }
            
            if (rightControllerVisualizer != null)
            {
                float distance = Vector3.Distance(rightControllerVisualizer.transform.position, networkRightPos);
                
                if (distance > teleportThreshold)
                {
                    // 순간이동
                    rightControllerVisualizer.transform.position = networkRightPos;
                    rightControllerVisualizer.transform.rotation = networkRightRot;
                }
                else
                {
                    // 부드러운 보간
                    rightControllerVisualizer.transform.position = Vector3.Lerp(
                        rightControllerVisualizer.transform.position, 
                        networkRightPos, 
                        positionLerpRate * Time.deltaTime
                    );
                    rightControllerVisualizer.transform.rotation = Quaternion.Lerp(
                        rightControllerVisualizer.transform.rotation, 
                        networkRightRot, 
                        rotationLerpRate * Time.deltaTime
                    );
                }
            }
        }

        #endregion

        #region 입력 액션 설정

        private void SetupInputActions()
        {
            // 왼손 입력 액션
            leftTriggerAction = new InputAction(binding: "<XRController>{LeftHand}/trigger");
            leftGripAction = new InputAction(binding: "<XRController>{LeftHand}/grip");
            leftPrimaryAction = new InputAction(binding: "<XRController>{LeftHand}/primaryButton");
            leftSecondaryAction = new InputAction(binding: "<XRController>{LeftHand}/secondaryButton");
            
            // 오른손 입력 액션
            rightTriggerAction = new InputAction(binding: "<XRController>{RightHand}/trigger");
            rightGripAction = new InputAction(binding: "<XRController>{RightHand}/grip");
            rightPrimaryAction = new InputAction(binding: "<XRController>{RightHand}/primaryButton");
            rightSecondaryAction = new InputAction(binding: "<XRController>{RightHand}/secondaryButton");
            
            Debug.Log("[VRControllerNetworkSync] 입력 액션 설정 완료");
        }

        private void EnableInputActions()
        {
            leftTriggerAction?.Enable();
            leftGripAction?.Enable();
            leftPrimaryAction?.Enable();
            leftSecondaryAction?.Enable();
            rightTriggerAction?.Enable();
            rightGripAction?.Enable();
            rightPrimaryAction?.Enable();
            rightSecondaryAction?.Enable();
        }

        private void DisableInputActions()
        {
            leftTriggerAction?.Disable();
            leftGripAction?.Disable();
            leftPrimaryAction?.Disable();
            leftSecondaryAction?.Disable();
            rightTriggerAction?.Disable();
            rightGripAction?.Disable();
            rightPrimaryAction?.Disable();
            rightSecondaryAction?.Disable();
        }

        #endregion

        #region 네트워크 동기화

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                SendControllerData(stream);
            }
            else
            {
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
        void OnPaddleChangedRPC(int newPaddleIndex)
        {
            // 자식 오브젝트의 PaddleChangeController를 찾아서 패들 변경 적용
            PaddleChangeController[] paddleControllers = GetComponentsInChildren<PaddleChangeController>();
            
            if (paddleControllers.Length > 0)
            {
                foreach (var controller in paddleControllers)
                {
                    if (controller != null)
                    {
                        controller.ApplyRemotePaddleChange(newPaddleIndex);
                        Debug.Log($"[VRControllerNetworkSync] 패들 변경 RPC 적용 완료: {controller.gameObject.name} → 인덱스 {newPaddleIndex}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[VRControllerNetworkSync] PaddleChangeController를 찾을 수 없어 패들 변경을 적용할 수 없습니다.");
            }
        }

        #endregion

        #region 햅틱 피드백

        private void TriggerHapticFeedback(string buttonName)
        {
            float intensity = 0.5f;
            float duration = 0.1f;
            
            if (buttonName.Contains("Left") && leftXRController != null)
            {
                leftXRController.SendHapticImpulse(intensity, duration);
            }
            else if (buttonName.Contains("Right") && rightXRController != null)
            {
                rightXRController.SendHapticImpulse(intensity, duration);
            }
        }

        public void SendHapticFeedback(string controllerName, float intensity, float duration)
        {
            if (photonView.IsMine)
            {
                // 로컬 햅틱 피드백
                TriggerHapticFeedback(controllerName);
                
                // 원격 플레이어들에게 전송
                photonView.RPC("TriggerHapticFeedbackRPC", RpcTarget.Others, controllerName, intensity, duration);
            }
        }

        #endregion

        #region 시각화 오브젝트

        private void CreateControllerVisualizers()
        {
            // 왼손 컨트롤러 시각화
            if (leftControllerVisualizer == null)
            {
                leftControllerVisualizer = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leftControllerVisualizer.name = $"{photonView.Owner.NickName}_LeftController";
                leftControllerVisualizer.transform.localScale = new Vector3(0.1f, 0.1f, 0.15f);
                
                Renderer leftRenderer = leftControllerVisualizer.GetComponent<Renderer>();
                if (leftRenderer != null)
                {
                    Material leftMat = new Material(Shader.Find("Standard"));
                    leftMat.color = Color.red;
                    leftMat.SetFloat("_Metallic", 0.5f);
                    leftMat.SetFloat("_Smoothness", 0.8f);
                    leftRenderer.material = leftMat;
                }
                
                Collider leftCollider = leftControllerVisualizer.GetComponent<Collider>();
                if (leftCollider != null) DestroyImmediate(leftCollider);
            }
            
            // 오른손 컨트롤러 시각화
            if (rightControllerVisualizer == null)
            {
                rightControllerVisualizer = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rightControllerVisualizer.name = $"{photonView.Owner.NickName}_RightController";
                rightControllerVisualizer.transform.localScale = new Vector3(0.1f, 0.1f, 0.15f);
                
                Renderer rightRenderer = rightControllerVisualizer.GetComponent<Renderer>();
                if (rightRenderer != null)
                {
                    Material rightMat = new Material(Shader.Find("Standard"));
                    rightMat.color = Color.blue;
                    rightMat.SetFloat("_Metallic", 0.5f);
                    rightMat.SetFloat("_Smoothness", 0.8f);
                    rightRenderer.material = rightMat;
                }
                
                Collider rightCollider = rightControllerVisualizer.GetComponent<Collider>();
                if (rightCollider != null) DestroyImmediate(rightCollider);
            }
            
            Debug.Log($"[VRControllerNetworkSync] 원격 플레이어 {photonView.Owner.NickName}의 컨트롤러 시각화 오브젝트 생성 완료");
        }

        #endregion

        #region 공개 메서드

        /// <summary>
        /// 원격 플레이어의 컨트롤러 위치 가져오기
        /// </summary>
        public Vector3 GetRemoteControllerPosition(bool isLeftHand)
        {
            return isLeftHand ? networkLeftPos : networkRightPos;
        }

        /// <summary>
        /// 원격 플레이어의 버튼 상태 확인
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

        #region 정리

        void OnDestroy()
        {
            // 입력 액션 정리
            DisableInputActions();
            
            leftTriggerAction?.Dispose();
            leftGripAction?.Dispose();
            leftPrimaryAction?.Dispose();
            leftSecondaryAction?.Dispose();
            rightTriggerAction?.Dispose();
            rightGripAction?.Dispose();
            rightPrimaryAction?.Dispose();
            rightSecondaryAction?.Dispose();
            
            // 시각화 오브젝트 정리
            if (leftControllerVisualizer != null)
                DestroyImmediate(leftControllerVisualizer);
            if (rightControllerVisualizer != null)
                DestroyImmediate(rightControllerVisualizer);
        }

        #endregion
    }
} 