using Photon.Pun;
using UnityEngine;
using DeepongVR.Court;
using System.Reflection;
using System.Linq;
using System;

/// <summary>
/// 패들의 위치, 회전, 타입을 네트워크를 통해 동기화합니다.
/// PaddleChangeController와 연동하여 패들 타입별로 다른 동기화 방식을 적용합니다.
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class PaddleNetworkSync : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("패들 컨트롤러 참조")]
    [SerializeField] private PaddleChangeController paddleController;
    [SerializeField] private PlayerNetworkSync playerNetworkSync;
    [SerializeField] private VRHumanoidController vrController;
    
    [Header("동기화 설정")]
    [SerializeField] private bool enablePaddleSync = true;
    [SerializeField] private float positionLerpRate = 15f;
    [SerializeField] private float rotationLerpRate = 15f;
    [SerializeField] private float teleportThreshold = 2f;
    
    [Header("패들 오브젝트 참조")]
    [SerializeField] private Transform racketTransform;
    [SerializeField] private Transform swordTransform;
    [SerializeField] private Transform leftGloveTransform;
    [SerializeField] private Transform rightGloveTransform;
    
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // 네트워크에서 수신받은 패들 데이터
    private struct NetworkPaddleData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 leftGlovePosition;
        public Quaternion leftGloveRotation;
        public Vector3 rightGlovePosition;
        public Quaternion rightGloveRotation;
        public int paddleType;
        public bool isActive;
    }
    
    private NetworkPaddleData networkData;
    private bool hasReceivedData = false;
    
    // 로컬 패들 데이터 (전송용)
    private Vector3 currentPaddlePosition;
    private Quaternion currentPaddleRotation;
    private Vector3 currentLeftGlovePosition;
    private Quaternion currentLeftGloveRotation;
    private Vector3 currentRightGlovePosition;
    private Quaternion currentRightGloveRotation;
    
    void Awake()
    {
        // 자동으로 컴포넌트 참조 찾기
        if (paddleController == null)
            paddleController = GetComponent<PaddleChangeController>();
        
        if (playerNetworkSync == null)
            playerNetworkSync = GetComponent<PlayerNetworkSync>();
            
        if (vrController == null)
            vrController = GetComponent<VRHumanoidController>();
        
        // 패들 Transform 자동 찾기
        FindPaddleTransforms();
    }
    
    void Start()
    {
        // PhotonView 설정 확인
        if (photonView == null)
        {
            Debug.LogError("[PaddleNetworkSync] PhotonView가 없습니다!");
            return;
        }
        
        // PhotonView 설정 확인
        CheckPhotonViewSetup();
        
        // 패들 Transform 찾기
        FindPaddleTransforms();
        
        // 상세한 구조 진단
        if (enableDebugLogs)
        {
            DiagnoseObjectStructure();
        }
        
        // 네트워크 데이터 초기화
        InitializeNetworkData();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[PaddleNetworkSync] 초기화 완료 - IsMine: {photonView.IsMine}");
        }
    }
    
    void Update()
    {
        if (!enablePaddleSync) return;
        
        if (photonView.IsMine)
        {
            // 로컬 플레이어: 현재 패들 위치 계산
            UpdateLocalPaddleData();
        }
        else if (hasReceivedData)
        {
            // 원격 플레이어: 네트워크 데이터로 패들 위치 동기화
            UpdateRemotePaddlePositions();
        }
    }
    
    /// <summary>
    /// 패들 Transform들을 자동으로 찾습니다.
    /// </summary>
    void FindPaddleTransforms()
    {
        Transform playerRoot = transform;
        
        // 실제 프리팹 이름으로 패들 오브젝트들 찾기
        if (racketTransform == null)
            racketTransform = FindChildRecursive(playerRoot, "Racket");
        
        if (swordTransform == null)
            swordTransform = FindChildRecursive(playerRoot, "Sword");
            
        if (leftGloveTransform == null)
            leftGloveTransform = FindChildRecursive(playerRoot, "Gloves_L");
            
        if (rightGloveTransform == null)
            rightGloveTransform = FindChildRecursive(playerRoot, "Gloves_R");
        
        // VR 컨트롤러 참조 추가 확인
        CheckVRControllerReferences();
            
        if (enableDebugLogs)
        {
            Debug.Log($"[PaddleNetworkSync] 패들 Transform 찾기 결과:");
            Debug.Log($"  Racket: {(racketTransform != null ? GetFullPath(racketTransform) : "null")}");
            Debug.Log($"  Sword: {(swordTransform != null ? GetFullPath(swordTransform) : "null")}");
            Debug.Log($"  Left Glove: {(leftGloveTransform != null ? GetFullPath(leftGloveTransform) : "null")}");
            Debug.Log($"  Right Glove: {(rightGloveTransform != null ? GetFullPath(rightGloveTransform) : "null")}");
            Debug.Log($"[PaddleNetworkSync] VR 컨트롤러 참조:");
            Debug.Log($"  VRController: {(vrController != null ? "Found" : "null")}");
            Debug.Log($"  LeftHand: {(vrController?.LeftHandController != null ? GetFullPath(vrController.LeftHandController) : "null")}");
            Debug.Log($"  RightHand: {(vrController?.RightHandController != null ? GetFullPath(vrController.RightHandController) : "null")}");
        }
    }
    
    /// <summary>
    /// VR 컨트롤러 참조를 확인하고 추가 설정을 수행합니다.
    /// </summary>
    void CheckVRControllerReferences()
    {
        // VRHumanoidController가 없다면 PlayerNetworkSync에서 참조 가져오기 시도
        if (vrController == null && playerNetworkSync != null)
        {
            // PlayerNetworkSync에서 VR 컨트롤러 참조 정보 가져오기 (리플렉션 사용)
            var vrControllerField = typeof(PlayerNetworkSync).GetField("vrController", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (vrControllerField != null)
            {
                vrController = vrControllerField.GetValue(playerNetworkSync) as VRHumanoidController;
            }
        }
        
        // 여전히 VRHumanoidController를 찾지 못했다면 직접 찾기
        if (vrController == null)
        {
            vrController = GetComponent<VRHumanoidController>();
            if (vrController == null)
            {
                vrController = GetComponentInParent<VRHumanoidController>();
            }
            if (vrController == null)
            {
                vrController = FindObjectOfType<VRHumanoidController>();
            }
        }
        
        // VRHumanoidController를 찾지 못했다면 직접 컨트롤러 Transform 찾기
        if (vrController == null)
        {
            Debug.LogWarning("[PaddleNetworkSync] VRHumanoidController를 찾을 수 없어서 직접 컨트롤러를 찾습니다.");
            FindControllerTransformsDirectly();
        }
        else
        {
            // 컨트롤러 참조 유효성 확인
            if (vrController.LeftHandController == null || vrController.RightHandController == null)
            {
                Debug.LogWarning("[PaddleNetworkSync] VRHumanoidController는 찾았지만 손 컨트롤러 참조가 없습니다!");
                Debug.LogWarning("직접 컨트롤러를 찾아보겠습니다.");
                FindControllerTransformsDirectly();
            }
        }
    }
    
    /// <summary>
    /// VRHumanoidController 없이 직접 컨트롤러 Transform들을 찾습니다.
    /// </summary>
    void FindControllerTransformsDirectly()
    {
        Transform playerRoot = transform;
        
        // 구조 분석 결과를 바탕으로 경로 찾기
        // Player_Origin/Camera Offset/Left Controller
        // Player_Origin/Camera Offset/Right Controller
        
        Transform cameraOffset = FindChildRecursive(playerRoot, "Camera Offset");
        if (cameraOffset != null)
        {
            Transform leftController = FindChildRecursive(cameraOffset, "Left Controller");
            Transform rightController = FindChildRecursive(cameraOffset, "Right Controller");
            
            if (enableDebugLogs)
            {
                Debug.Log($"[PaddleNetworkSync] 직접 컨트롤러 찾기:");
                Debug.Log($"  Camera Offset: {(cameraOffset != null ? GetFullPath(cameraOffset) : "null")}");
                Debug.Log($"  Left Controller: {(leftController != null ? GetFullPath(leftController) : "null")}");
                Debug.Log($"  Right Controller: {(rightController != null ? GetFullPath(rightController) : "null")}");
            }
            
            // 가상 VRHumanoidController 정보 생성
            if (vrController == null && (leftController != null || rightController != null))
            {
                // 임시로 Transform 정보를 사용 (VRHumanoidController 없이)
                if (enableDebugLogs)
                {
                    Debug.Log("[PaddleNetworkSync] VRHumanoidController 없이 직접 컨트롤러 Transform 사용");
                }
            }
        }
        else
        {
            Debug.LogWarning("[PaddleNetworkSync] Camera Offset을 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// PhotonView 설정을 확인하고 자동으로 설정합니다.
    /// </summary>
    void CheckPhotonViewSetup()
    {
        if (photonView == null)
        {
            Debug.LogError("[PaddleNetworkSync] PhotonView를 찾을 수 없습니다!");
            return;
        }
        
        // Observed Components에 이 스크립트가 추가되어 있는지 확인
        bool isObserved = false;
        foreach (var observed in photonView.ObservedComponents)
        {
            if (observed == this)
            {
                isObserved = true;
                break;
            }
        }
        
        if (!isObserved)
        {
            // 자동으로 Observed Components에 추가
            var observedList = new System.Collections.Generic.List<Component>(photonView.ObservedComponents);
            observedList.Add(this);
            photonView.ObservedComponents = observedList;
            
            Debug.Log("[PaddleNetworkSync] PhotonView Observed Components에 자동으로 추가되었습니다.");
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[PaddleNetworkSync] PhotonView 설정:");
            Debug.Log($"  ViewID: {photonView.ViewID}");
            Debug.Log($"  IsMine: {photonView.IsMine}");
            Debug.Log($"  Observed Components 수: {photonView.ObservedComponents.Count}");
            Debug.Log($"  이 스크립트 관찰됨: {isObserved}");
        }
    }
    
    /// <summary>
    /// 오브젝트 구조를 진단합니다.
    /// </summary>
    void DiagnoseObjectStructure()
    {
        Debug.Log("=== PaddleNetworkSync 구조 진단 시작 ===");
        Debug.Log($"현재 오브젝트: {gameObject.name}");
        Debug.Log($"현재 오브젝트 경로: {GetFullPath(transform)}");
        
        // VRHumanoidController 찾기
        Debug.Log("--- VRHumanoidController 검색 ---");
        var vrControllers = FindObjectsOfType<VRHumanoidController>();
        Debug.Log($"씬에 있는 VRHumanoidController 수: {vrControllers.Length}");
        
        for (int i = 0; i < vrControllers.Length; i++)
        {
            var controller = vrControllers[i];
            Debug.Log($"VRController {i}: {GetFullPath(controller.transform)}");
            Debug.Log($"  LeftHandController: {(controller.LeftHandController != null ? GetFullPath(controller.LeftHandController) : "null")}");
            Debug.Log($"  RightHandController: {(controller.RightHandController != null ? GetFullPath(controller.RightHandController) : "null")}");
            Debug.Log($"  Headset: {(controller.Headset != null ? GetFullPath(controller.Headset) : "null")}");
        }
        
        // 패들 오브젝트 찾기
        Debug.Log("--- 패들 오브젝트 검색 ---");
        string[] paddleNames = {"Racket", "Sword", "Gloves_L", "Gloves_R"};
        
        foreach (string paddleName in paddleNames)
        {
            GameObject[] foundPaddles = GameObject.FindObjectsOfType<GameObject>()
                .Where(go => go.name.ToLower().Contains(paddleName.ToLower())).ToArray();
            
            Debug.Log($"{paddleName} 검색 결과: {foundPaddles.Length}개");
            foreach (var paddle in foundPaddles)
            {
                Debug.Log($"  - {GetFullPath(paddle.transform)} (Active: {paddle.activeInHierarchy})");
            }
        }
        
        // PaddleChangeController 찾기
        Debug.Log("--- PaddleChangeController 검색 ---");
        var paddleChangeControllers = FindObjectsOfType<DeepongVR.Court.PaddleChangeController>();
        Debug.Log($"씬에 있는 PaddleChangeController 수: {paddleChangeControllers.Length}");
        
        for (int i = 0; i < paddleChangeControllers.Length; i++)
        {
            var controller = paddleChangeControllers[i];
            Debug.Log($"PaddleChangeController {i}: {GetFullPath(controller.transform)}");
            Debug.Log($"  CurrentPaddleIndex: {controller.CurrentPaddleIndex}");
            Debug.Log($"  CurrentPaddleName: {controller.CurrentPaddleName}");
        }
        
        Debug.Log("=== 구조 진단 완료 ===");
    }
    
    /// <summary>
    /// Transform의 전체 경로를 반환합니다.
    /// </summary>
    string GetFullPath(Transform transform)
    {
        if (transform == null) return "null";
        
        string path = transform.name;
        Transform parent = transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
    
    /// <summary>
    /// 왼쪽 컨트롤러 Transform을 반환합니다.
    /// </summary>
    Transform GetLeftControllerTransform()
    {
        Transform cameraOffset = FindChildRecursive(transform, "Camera Offset");
        if (cameraOffset != null)
        {
            return FindChildRecursive(cameraOffset, "Left Controller");
        }
        return null;
    }
    
    /// <summary>
    /// 오른쪽 컨트롤러 Transform을 반환합니다.
    /// </summary>
    Transform GetRightControllerTransform()
    {
        Transform cameraOffset = FindChildRecursive(transform, "Camera Offset");
        if (cameraOffset != null)
        {
            return FindChildRecursive(cameraOffset, "Right Controller");
        }
        return null;
    }
    
    /// <summary>
    /// 자식 오브젝트를 재귀적으로 찾습니다.
    /// </summary>
    Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            // 정확한 이름 매칭 (대소문자 구분 없음)
            if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return child;
                
            Transform found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }
    
    /// <summary>
    /// 네트워크 데이터를 초기값으로 설정합니다.
    /// </summary>
    void InitializeNetworkData()
    {
        networkData = new NetworkPaddleData
        {
            position = transform.position,
            rotation = transform.rotation,
            leftGlovePosition = transform.position,
            leftGloveRotation = transform.rotation,
            rightGlovePosition = transform.position,
            rightGloveRotation = transform.rotation,
            paddleType = paddleController != null ? paddleController.CurrentPaddleIndex : 0,
            isActive = true
        };
    }
    
    /// <summary>
    /// 로컬 플레이어의 패들 데이터를 업데이트합니다.
    /// </summary>
    void UpdateLocalPaddleData()
    {
        if (paddleController == null) return;
        
        int currentPaddleType = paddleController.CurrentPaddleIndex;
        Vector3 previousPosition = currentPaddlePosition;
        
        switch (currentPaddleType)
        {
            case 0: // Racket
                UpdateRacketPosition();
                break;
            case 1: // Sword  
                UpdateSwordPosition();
                break;
            case 2: // Glove
                UpdateGlovePositions();
                break;
        }
        
        // 위치 변화 디버깅
        if (enableDebugLogs && Vector3.Distance(previousPosition, currentPaddlePosition) > 0.01f)
        {
            Debug.Log($"[PaddleNetworkSync] 위치 업데이트 - 패들타입: {currentPaddleType}, 위치: {currentPaddlePosition}");
        }
    }
    
    /// <summary>
    /// 라켓 위치를 오른손 컨트롤러 기준으로 업데이트합니다.
    /// </summary>
    void UpdateRacketPosition()
    {
        // VRHumanoidController가 있고 RightHandController가 설정된 경우
        if (vrController != null && vrController.RightHandController != null)
        {
            currentPaddlePosition = vrController.RightHandController.position;
            currentPaddleRotation = vrController.RightHandController.rotation;
        }
        // VRHumanoidController가 없는 경우 직접 Right Controller 찾기
        else
        {
            Transform rightController = GetRightControllerTransform();
            if (rightController != null)
            {
                currentPaddlePosition = rightController.position;
                currentPaddleRotation = rightController.rotation;
            }
            // 마지막 대안으로 패들 자체 위치 사용
            else if (racketTransform != null)
            {
                currentPaddlePosition = racketTransform.position;
                currentPaddleRotation = racketTransform.rotation;
            }
        }
    }
    
    /// <summary>
    /// 검 위치를 오른손 컨트롤러 기준으로 업데이트합니다.
    /// </summary>
    void UpdateSwordPosition()
    {
        // VRHumanoidController가 있고 RightHandController가 설정된 경우
        if (vrController != null && vrController.RightHandController != null)
        {
            currentPaddlePosition = vrController.RightHandController.position;
            currentPaddleRotation = vrController.RightHandController.rotation;
        }
        // VRHumanoidController가 없는 경우 직접 Right Controller 찾기
        else
        {
            Transform rightController = GetRightControllerTransform();
            if (rightController != null)
            {
                currentPaddlePosition = rightController.position;
                currentPaddleRotation = rightController.rotation;
            }
            // 마지막 대안으로 패들 자체 위치 사용
            else if (swordTransform != null)
            {
                currentPaddlePosition = swordTransform.position;
                currentPaddleRotation = swordTransform.rotation;
            }
        }
    }
    
    /// <summary>
    /// 글러브 위치를 양손 컨트롤러 기준으로 업데이트합니다.
    /// </summary>
    void UpdateGlovePositions()
    {
        // VRHumanoidController가 있고 양손 컨트롤러가 설정된 경우
        if (vrController != null && vrController.LeftHandController != null && vrController.RightHandController != null)
        {
            currentLeftGlovePosition = vrController.LeftHandController.position;
            currentLeftGloveRotation = vrController.LeftHandController.rotation;
            currentRightGlovePosition = vrController.RightHandController.position;
            currentRightGloveRotation = vrController.RightHandController.rotation;
        }
        // VRHumanoidController가 없는 경우 직접 컨트롤러 찾기
        else
        {
            Transform leftController = GetLeftControllerTransform();
            Transform rightController = GetRightControllerTransform();
            
            if (leftController != null)
            {
                currentLeftGlovePosition = leftController.position;
                currentLeftGloveRotation = leftController.rotation;
            }
            else if (leftGloveTransform != null)
            {
                currentLeftGlovePosition = leftGloveTransform.position;
                currentLeftGloveRotation = leftGloveTransform.rotation;
            }
            
            if (rightController != null)
            {
                currentRightGlovePosition = rightController.position;
                currentRightGloveRotation = rightController.rotation;
            }
            else if (rightGloveTransform != null)
            {
                currentRightGlovePosition = rightGloveTransform.position;
                currentRightGloveRotation = rightGloveTransform.rotation;
            }
        }
    }
    
    /// <summary>
    /// 원격 플레이어의 패들 위치를 네트워크 데이터로 동기화합니다.
    /// </summary>
    void UpdateRemotePaddlePositions()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[PaddleNetworkSync] 원격 패들 위치 업데이트 시작 - 패들타입: {networkData.paddleType}");
        }
        
        switch (networkData.paddleType)
        {
            case 0: // Racket
                if (racketTransform != null)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[PaddleNetworkSync] Racket 동기화: {networkData.position} -> {racketTransform.name}");
                    }
                    SyncTransform(racketTransform, networkData.position, networkData.rotation);
                }
                else if (enableDebugLogs)
                {
                    Debug.LogWarning("[PaddleNetworkSync] Racket Transform이 null입니다!");
                }
                break;
                
            case 1: // Sword
                if (swordTransform != null)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[PaddleNetworkSync] Sword 동기화: {networkData.position} -> {swordTransform.name}");
                    }
                    SyncTransform(swordTransform, networkData.position, networkData.rotation);
                }
                else if (enableDebugLogs)
                {
                    Debug.LogWarning("[PaddleNetworkSync] Sword Transform이 null입니다!");
                }
                break;
                
            case 2: // Glove
                if (leftGloveTransform != null)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[PaddleNetworkSync] Left Glove 동기화: {networkData.leftGlovePosition} -> {leftGloveTransform.name}");
                    }
                    SyncTransform(leftGloveTransform, networkData.leftGlovePosition, networkData.leftGloveRotation);
                }
                else if (enableDebugLogs)
                {
                    Debug.LogWarning("[PaddleNetworkSync] Left Glove Transform이 null입니다!");
                }
                
                if (rightGloveTransform != null)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[PaddleNetworkSync] Right Glove 동기화: {networkData.rightGlovePosition} -> {rightGloveTransform.name}");
                    }
                    SyncTransform(rightGloveTransform, networkData.rightGlovePosition, networkData.rightGloveRotation);
                }
                else if (enableDebugLogs)
                {
                    Debug.LogWarning("[PaddleNetworkSync] Right Glove Transform이 null입니다!");
                }
                break;
        }
    }
    
    /// <summary>
    /// Transform을 네트워크 위치로 부드럽게 동기화합니다.
    /// </summary>
    void SyncTransform(Transform target, Vector3 networkPosition, Quaternion networkRotation)
    {
        if (target == null) return;
        
        float distance = Vector3.Distance(target.position, networkPosition);
        
        // 순간이동 임계값 확인
        if (distance > teleportThreshold)
        {
            target.position = networkPosition;
            target.rotation = networkRotation;
        }
        else
        {
            // 부드러운 보간
            target.position = Vector3.Lerp(target.position, networkPosition, positionLerpRate * Time.deltaTime);
            target.rotation = Quaternion.Lerp(target.rotation, networkRotation, rotationLerpRate * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// Photon 네트워크 동기화 구현
    /// </summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (!enablePaddleSync) return;
        
        if (stream.IsWriting)
        {
            // 데이터 전송
            int currentPaddleType = paddleController != null ? paddleController.CurrentPaddleIndex : 0;
            stream.SendNext(currentPaddleType);
            
            switch (currentPaddleType)
            {
                case 0: // Racket
                case 1: // Sword
                    stream.SendNext(currentPaddlePosition);
                    stream.SendNext(currentPaddleRotation);
                    break;
                    
                case 2: // Glove
                    stream.SendNext(currentLeftGlovePosition);
                    stream.SendNext(currentLeftGloveRotation);
                    stream.SendNext(currentRightGlovePosition);
                    stream.SendNext(currentRightGloveRotation);
                    break;
            }
            
            if (enableDebugLogs)
            {
                string positionInfo = "";
                switch (currentPaddleType)
                {
                    case 0:
                    case 1:
                        positionInfo = $", 위치: {currentPaddlePosition}";
                        break;
                    case 2:
                        positionInfo = $", 왼손: {currentLeftGlovePosition}, 오른손: {currentRightGlovePosition}";
                        break;
                }
                Debug.Log($"[PaddleNetworkSync] 데이터 전송 - 패들타입: {currentPaddleType}{positionInfo}");
            }
        }
        else
        {
            // 데이터 수신
            networkData.paddleType = (int)stream.ReceiveNext();
            
            switch (networkData.paddleType)
            {
                case 0: // Racket
                case 1: // Sword
                    networkData.position = (Vector3)stream.ReceiveNext();
                    networkData.rotation = (Quaternion)stream.ReceiveNext();
                    break;
                    
                case 2: // Glove
                    networkData.leftGlovePosition = (Vector3)stream.ReceiveNext();
                    networkData.leftGloveRotation = (Quaternion)stream.ReceiveNext();
                    networkData.rightGlovePosition = (Vector3)stream.ReceiveNext();
                    networkData.rightGloveRotation = (Quaternion)stream.ReceiveNext();
                    break;
            }
            
            networkData.isActive = true;
            hasReceivedData = true;
            
            if (enableDebugLogs)
            {
                string positionInfo = "";
                switch (networkData.paddleType)
                {
                    case 0:
                    case 1:
                        positionInfo = $", 위치: {networkData.position}";
                        break;
                    case 2:
                        positionInfo = $", 왼손: {networkData.leftGlovePosition}, 오른손: {networkData.rightGlovePosition}";
                        break;
                }
                Debug.Log($"[PaddleNetworkSync] 데이터 수신 - 패들타입: {networkData.paddleType}{positionInfo}");
            }
        }
    }
    
    /// <summary>
    /// 디버그용 기즈모 그리기
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!enablePaddleSync || !hasReceivedData) return;
        
        Gizmos.color = Color.cyan;
        
        switch (networkData.paddleType)
        {
            case 0: // Racket
            case 1: // Sword
                Gizmos.DrawWireSphere(networkData.position, 0.1f);
                break;
                
            case 2: // Glove
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(networkData.leftGlovePosition, 0.05f);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(networkData.rightGlovePosition, 0.05f);
                break;
        }
    }
    
    /// <summary>
    /// 패들 동기화 활성/비활성
    /// </summary>
    public void SetPaddleSyncEnabled(bool enabled)
    {
        enablePaddleSync = enabled;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[PaddleNetworkSync] 패들 동기화 {(enabled ? "활성화" : "비활성화")}");
        }
    }
    
    /// <summary>
    /// 현재 동기화 상태 확인
    /// </summary>
    public bool IsPaddleSyncEnabled => enablePaddleSync;
    
    /// <summary>
    /// 네트워크 데이터 수신 여부 확인
    /// </summary>
    public bool HasReceivedNetworkData => hasReceivedData;
} 