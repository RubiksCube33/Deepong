using Photon.Pun;
using UnityEngine;
using DeepongVR.Court;

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
    [SerializeField] private bool enableDebugLogs = false;
    
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
        if (paddleController != null)
        {
            // PaddleChangeController의 private 필드에 접근하기 위해 
            // 활성화된 패들 오브젝트를 찾는 방식 사용
            Transform playerRoot = transform;
            
            // 패들 오브젝트들을 이름으로 찾기
            if (racketTransform == null)
                racketTransform = FindChildRecursive(playerRoot, "paddle_racket");
            
            if (swordTransform == null)
                swordTransform = FindChildRecursive(playerRoot, "paddle_sword");
                
            if (leftGloveTransform == null)
                leftGloveTransform = FindChildRecursive(playerRoot, "paddle_glove_left");
                
            if (rightGloveTransform == null)
                rightGloveTransform = FindChildRecursive(playerRoot, "paddle_glove_right");
                
            if (enableDebugLogs)
            {
                Debug.Log($"[PaddleNetworkSync] 패들 Transform 찾기 결과:");
                Debug.Log($"  Racket: {(racketTransform != null ? racketTransform.name : "null")}");
                Debug.Log($"  Sword: {(swordTransform != null ? swordTransform.name : "null")}");
                Debug.Log($"  Left Glove: {(leftGloveTransform != null ? leftGloveTransform.name : "null")}");
                Debug.Log($"  Right Glove: {(rightGloveTransform != null ? rightGloveTransform.name : "null")}");
            }
        }
    }
    
    /// <summary>
    /// 자식 오브젝트를 재귀적으로 찾습니다.
    /// </summary>
    Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLower().Contains(name.ToLower()))
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
    }
    
    /// <summary>
    /// 라켓 위치를 오른손 컨트롤러 기준으로 업데이트합니다.
    /// </summary>
    void UpdateRacketPosition()
    {
        if (vrController != null && vrController.RightHandController != null)
        {
            currentPaddlePosition = vrController.RightHandController.position;
            currentPaddleRotation = vrController.RightHandController.rotation;
        }
        else if (racketTransform != null)
        {
            currentPaddlePosition = racketTransform.position;
            currentPaddleRotation = racketTransform.rotation;
        }
    }
    
    /// <summary>
    /// 검 위치를 오른손 컨트롤러 기준으로 업데이트합니다.
    /// </summary>
    void UpdateSwordPosition()
    {
        if (vrController != null && vrController.RightHandController != null)
        {
            currentPaddlePosition = vrController.RightHandController.position;
            currentPaddleRotation = vrController.RightHandController.rotation;
        }
        else if (swordTransform != null)
        {
            currentPaddlePosition = swordTransform.position;
            currentPaddleRotation = swordTransform.rotation;
        }
    }
    
    /// <summary>
    /// 글러브 위치를 양손 컨트롤러 기준으로 업데이트합니다.
    /// </summary>
    void UpdateGlovePositions()
    {
        if (vrController != null)
        {
            if (vrController.LeftHandController != null)
            {
                currentLeftGlovePosition = vrController.LeftHandController.position;
                currentLeftGloveRotation = vrController.LeftHandController.rotation;
            }
            
            if (vrController.RightHandController != null)
            {
                currentRightGlovePosition = vrController.RightHandController.position;
                currentRightGloveRotation = vrController.RightHandController.rotation;
            }
        }
        else
        {
            if (leftGloveTransform != null)
            {
                currentLeftGlovePosition = leftGloveTransform.position;
                currentLeftGloveRotation = leftGloveTransform.rotation;
            }
            
            if (rightGloveTransform != null)
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
        switch (networkData.paddleType)
        {
            case 0: // Racket
                if (racketTransform != null)
                {
                    SyncTransform(racketTransform, networkData.position, networkData.rotation);
                }
                break;
                
            case 1: // Sword
                if (swordTransform != null)
                {
                    SyncTransform(swordTransform, networkData.position, networkData.rotation);
                }
                break;
                
            case 2: // Glove
                if (leftGloveTransform != null)
                {
                    SyncTransform(leftGloveTransform, networkData.leftGlovePosition, networkData.leftGloveRotation);
                }
                if (rightGloveTransform != null)
                {
                    SyncTransform(rightGloveTransform, networkData.rightGlovePosition, networkData.rightGloveRotation);
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
                Debug.Log($"[PaddleNetworkSync] 데이터 전송 - 패들타입: {currentPaddleType}");
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
                Debug.Log($"[PaddleNetworkSync] 데이터 수신 - 패들타입: {networkData.paddleType}");
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