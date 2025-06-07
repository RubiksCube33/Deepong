using Photon.Pun;
using UnityEngine;

/// <summary>
/// 플레이어의 애니메이션 상태를 네트워크를 통해 동기화합니다.
/// Animator의 파라미터들과 애니메이션 트리거를 동기화합니다.
/// </summary>
[RequireComponent(typeof(Animator), typeof(PhotonView))]
public class PlayerAnimationSync : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("동기화할 애니메이션 파라미터")]
    [SerializeField] private string[] floatParameters = {"Speed", "MotionSpeed"};
    [SerializeField] private string[] boolParameters = {"Grounded"};
    [SerializeField] private string[] intParameters = {};
    
    [Header("동기화 설정")]
    [SerializeField] private bool syncAnimationParameters = true;
    [SerializeField] private bool syncAnimationStates = true;
    [SerializeField] private float parameterSmoothTime = 0.1f; // 파라미터 보간 시간
    
    private Animator animator;
    
    // 네트워크에서 수신받은 애니메이션 파라미터들
    private float[] networkFloatParams;
    private bool[] networkBoolParams;
    private int[] networkIntParams;
    
    // 현재 애니메이션 상태 정보
    private int currentAnimationHash;
    private float currentAnimationTime;
    private int networkAnimationHash;
    private float networkAnimationTime;
    
    // 부드러운 보간을 위한 변수들
    private float[] targetFloatParams;
    private float[] currentFloatParams;
    private float[] floatParamVelocities;

    void Awake()
    {
        animator = GetComponent<Animator>();
        
        // 배열 초기화
        InitializeArrays();
    }
    
    void InitializeArrays()
    {
        // Float 파라미터 배열 초기화
        networkFloatParams = new float[floatParameters.Length];
        targetFloatParams = new float[floatParameters.Length];
        currentFloatParams = new float[floatParameters.Length];
        floatParamVelocities = new float[floatParameters.Length];
        
        // Bool 파라미터 배열 초기화
        networkBoolParams = new bool[boolParameters.Length];
        
        // Int 파라미터 배열 초기화
        networkIntParams = new int[intParameters.Length];
        
        // 현재 값으로 초기화
        for (int i = 0; i < floatParameters.Length; i++)
        {
            if (animator.parameters.Length > 0)
            {
                float currentValue = animator.GetFloat(floatParameters[i]);
                networkFloatParams[i] = currentValue;
                targetFloatParams[i] = currentValue;
                currentFloatParams[i] = currentValue;
            }
        }
        
        for (int i = 0; i < boolParameters.Length; i++)
        {
            if (animator.parameters.Length > 0)
            {
                networkBoolParams[i] = animator.GetBool(boolParameters[i]);
            }
        }
        
        for (int i = 0; i < intParameters.Length; i++)
        {
            if (animator.parameters.Length > 0)
            {
                networkIntParams[i] = animator.GetInteger(intParameters[i]);
            }
        }
    }

    void Update()
    {
        // 내가 소유한 플레이어가 아닌 경우에만 동기화 적용
        if (!photonView.IsMine && syncAnimationParameters)
        {
            SyncAnimationParameters();
        }
        
        // 애니메이션 상태 동기화
        if (!photonView.IsMine && syncAnimationStates)
        {
            SyncAnimationStates();
        }
    }
    
    void SyncAnimationParameters()
    {
        // Float 파라미터들을 부드럽게 보간
        for (int i = 0; i < floatParameters.Length; i++)
        {
            // SmoothDamp를 사용하여 부드러운 전환
            currentFloatParams[i] = Mathf.SmoothDamp(
                currentFloatParams[i], 
                networkFloatParams[i], 
                ref floatParamVelocities[i], 
                parameterSmoothTime
            );
            
            animator.SetFloat(floatParameters[i], currentFloatParams[i]);
        }
        
        // Bool 파라미터들은 즉시 적용
        for (int i = 0; i < boolParameters.Length; i++)
        {
            animator.SetBool(boolParameters[i], networkBoolParams[i]);
        }
        
        // Int 파라미터들은 즉시 적용
        for (int i = 0; i < intParameters.Length; i++)
        {
            animator.SetInteger(intParameters[i], networkIntParams[i]);
        }
    }
    
    void SyncAnimationStates()
    {
        // 애니메이션 상태가 다른 경우 동기화
        if (networkAnimationHash != 0 && currentAnimationHash != networkAnimationHash)
        {
            // 특정 애니메이션 상태로 강제 전환
            animator.Play(networkAnimationHash, 0, networkAnimationTime);
            currentAnimationHash = networkAnimationHash;
            currentAnimationTime = networkAnimationTime;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 내 플레이어의 애니메이션 정보를 다른 클라이언트에게 전송
            
            // Float 파라미터들 전송
            for (int i = 0; i < floatParameters.Length; i++)
            {
                stream.SendNext(animator.GetFloat(floatParameters[i]));
            }
            
            // Bool 파라미터들 전송
            for (int i = 0; i < boolParameters.Length; i++)
            {
                stream.SendNext(animator.GetBool(boolParameters[i]));
            }
            
            // Int 파라미터들 전송
            for (int i = 0; i < intParameters.Length; i++)
            {
                stream.SendNext(animator.GetInteger(intParameters[i]));
            }
            
            // 현재 애니메이션 상태 전송
            if (syncAnimationStates && animator.GetCurrentAnimatorStateInfo(0).length > 0)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                stream.SendNext(stateInfo.shortNameHash);
                stream.SendNext(stateInfo.normalizedTime);
            }
            else
            {
                stream.SendNext(0); // 해시
                stream.SendNext(0f); // 시간
            }
        }
        else
        {
            // 다른 클라이언트로부터 애니메이션 정보를 수신
            
            // Float 파라미터들 수신
            for (int i = 0; i < floatParameters.Length; i++)
            {
                networkFloatParams[i] = (float)stream.ReceiveNext();
            }
            
            // Bool 파라미터들 수신
            for (int i = 0; i < boolParameters.Length; i++)
            {
                networkBoolParams[i] = (bool)stream.ReceiveNext();
            }
            
            // Int 파라미터들 수신
            for (int i = 0; i < intParameters.Length; i++)
            {
                networkIntParams[i] = (int)stream.ReceiveNext();
            }
            
            // 애니메이션 상태 수신
            networkAnimationHash = (int)stream.ReceiveNext();
            networkAnimationTime = (float)stream.ReceiveNext();
        }
    }
    
    /// <summary>
    /// 애니메이션 트리거를 네트워크를 통해 동기화합니다.
    /// </summary>
    /// <param name="triggerName">트리거 파라미터 이름</param>
    [PunRPC]
    public void SyncAnimationTrigger(string triggerName)
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerName);
            Debug.Log($"애니메이션 트리거 동기화: {triggerName}");
        }
    }
    
    /// <summary>
    /// 애니메이션 트리거를 모든 클라이언트에게 전송합니다.
    /// </summary>
    /// <param name="triggerName">트리거 파라미터 이름</param>
    public void TriggerAnimationForAll(string triggerName)
    {
        // 내 애니메이터에서 즉시 실행
        if (photonView.IsMine)
        {
            animator.SetTrigger(triggerName);
        }
        
        // 다른 클라이언트들에게도 전송
        photonView.RPC("SyncAnimationTrigger", RpcTarget.Others, triggerName);
    }
    
    /// <summary>
    /// 특정 애니메이션 상태로 강제 이동합니다.
    /// </summary>
    /// <param name="stateName">애니메이션 상태 이름</param>
    /// <param name="normalizedTime">정규화된 시간 (0~1)</param>
    [PunRPC]
    public void ForceAnimationState(string stateName, float normalizedTime = 0f)
    {
        if (animator != null)
        {
            animator.Play(stateName, 0, normalizedTime);
            Debug.Log($"강제 애니메이션 상태 변경: {stateName} at {normalizedTime}");
        }
    }
    
    /// <summary>
    /// 모든 클라이언트에서 특정 애니메이션 상태로 강제 이동합니다.
    /// </summary>
    /// <param name="stateName">애니메이션 상태 이름</param>
    /// <param name="normalizedTime">정규화된 시간 (0~1)</param>
    public void ForceAnimationStateForAll(string stateName, float normalizedTime = 0f)
    {
        // 내 애니메이터에서 즉시 실행
        if (photonView.IsMine)
        {
            animator.Play(stateName, 0, normalizedTime);
        }
        
        // 다른 클라이언트들에게도 전송
        photonView.RPC("ForceAnimationState", RpcTarget.Others, stateName, normalizedTime);
    }
    
    /// <summary>
    /// 현재 동기화 상태를 디버깅합니다.
    /// </summary>
    [ContextMenu("Debug Animation Sync")]
    public void DebugAnimationSync()
    {
        Debug.Log($"=== Animation Sync Debug ({gameObject.name}) ===");
        Debug.Log($"Is Mine: {photonView.IsMine}");
        Debug.Log($"Sync Parameters: {syncAnimationParameters}");
        Debug.Log($"Sync States: {syncAnimationStates}");
        
        // Float 파라미터 상태
        for (int i = 0; i < floatParameters.Length; i++)
        {
            float current = animator.GetFloat(floatParameters[i]);
            Debug.Log($"Float Param '{floatParameters[i]}': Current={current:F2}, Network={networkFloatParams[i]:F2}");
        }
        
        // Bool 파라미터 상태
        for (int i = 0; i < boolParameters.Length; i++)
        {
            bool current = animator.GetBool(boolParameters[i]);
            Debug.Log($"Bool Param '{boolParameters[i]}': Current={current}, Network={networkBoolParams[i]}");
        }
        
        // 현재 애니메이션 상태
        if (animator.GetCurrentAnimatorStateInfo(0).length > 0)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"Current State: Hash={stateInfo.shortNameHash}, Time={stateInfo.normalizedTime:F2}");
            Debug.Log($"Network State: Hash={networkAnimationHash}, Time={networkAnimationTime:F2}");
        }
    }
} 