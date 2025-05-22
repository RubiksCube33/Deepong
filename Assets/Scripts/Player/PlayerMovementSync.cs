using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementSync : MonoBehaviourPun, IPunObservable
{
    [Header("네트워크 동기화 설정")]
    [SerializeField] private float syncSmoothing = 10f;        // 위치 보간 속도
    [SerializeField] private float syncRotationSmoothing = 8f; // 회전 보간 속도
    [SerializeField] private float syncThreshold = 0.1f;       // 동기화 임계값 (이 거리 이상 차이날 때만 동기화)
    [SerializeField] private float maxSyncDistance = 5f;       // 최대 동기화 거리 (이 거리 이상 차이나면 즉시 이동)
    
    [Header("애니메이션 동기화")]
    [SerializeField] private string[] syncedAnimParams;        // 동기화할 애니메이션 파라미터 이름들
    
    // 네트워크 동기화 변수
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private Vector3 networkVelocity;
    private Vector3 lastPosition;
    private float networkSpeed;
    
    // 컴포넌트 캐싱
    private Rigidbody rb;
    private Animator animator;
    
    // 애니메이션 동기화를 위한 변수
    private Dictionary<string, int> animParamHashes = new Dictionary<string, int>();
    private Dictionary<string, float> animFloatValues = new Dictionary<string, float>();
    private Dictionary<string, bool> animBoolValues = new Dictionary<string, bool>();
    private Dictionary<string, int> animIntValues = new Dictionary<string, int>();
    private Dictionary<string, AnimatorControllerParameterType> animParamTypes = new Dictionary<string, AnimatorControllerParameterType>();
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        
        // 초기화
        networkPosition = transform.position;
        networkRotation = transform.rotation;
        lastPosition = transform.position;
        
        // 애니메이션 파라미터 해시 캐싱 (성능 최적화)
        if (syncedAnimParams != null)
        {
            CacheAnimationParameters();
        }
    }
    
    // 외부에서 동기화할 애니메이션 파라미터 설정
    public void SetAnimationParameters(string[] parameters)
    {
        syncedAnimParams = parameters;
        CacheAnimationParameters();
    }
    
    private void CacheAnimationParameters()
    {
        if (animator == null || syncedAnimParams == null)
            return;
            
        foreach (string paramName in syncedAnimParams)
        {
            int hash = Animator.StringToHash(paramName);
            animParamHashes[paramName] = hash;
            
            // 파라미터 타입 저장
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == paramName)
                {
                    animParamTypes[paramName] = param.type;
                    
                    // 타입에 따라 기본값 초기화
                    switch (param.type)
                    {
                        case AnimatorControllerParameterType.Float:
                            animFloatValues[paramName] = 0f;
                            break;
                        case AnimatorControllerParameterType.Bool:
                            animBoolValues[paramName] = false;
                            break;
                        case AnimatorControllerParameterType.Int:
                            animIntValues[paramName] = 0;
                            break;
                    }
                    break;
                }
            }
        }
    }
    
    private void Update()
    {
        // 내 캐릭터가 아닌 경우 위치/회전 보간
        if (!photonView.IsMine)
        {
            // 거리가 너무 크면 즉시 이동
            if (Vector3.Distance(transform.position, networkPosition) > maxSyncDistance)
            {
                transform.position = networkPosition;
                transform.rotation = networkRotation;
                
                if (rb != null)
                {
                    rb.velocity = networkVelocity;
                }
            }
            else if (Vector3.Distance(transform.position, networkPosition) > syncThreshold)
            {
                // 부드러운 위치 보간
                transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * syncSmoothing);
                transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation, Time.deltaTime * syncRotationSmoothing);
                
                // 리지드바디가 있으면 속도도 보간
                if (rb != null && !rb.isKinematic)
                {
                    rb.velocity = Vector3.Lerp(rb.velocity, networkVelocity, Time.deltaTime * syncSmoothing);
                }
            }
        }
    }
    
    // 속도 계산
    private void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            // 속도 계산 (자신의 캐릭터만)
            networkSpeed = Vector3.Distance(transform.position, lastPosition) / Time.fixedDeltaTime;
            lastPosition = transform.position;
        }
    }
    
    // Photon 네트워크 동기화 인터페이스 구현
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // 데이터 송신 (자신의 캐릭터)
        if (stream.IsWriting)
        {
            // 위치, 회전, 속도 전송
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            
            if (rb != null)
            {
                stream.SendNext(rb.velocity);
            }
            else
            {
                stream.SendNext(Vector3.zero);
            }
            
            // 애니메이션 파라미터 전송
            foreach (string paramName in syncedAnimParams)
            {
                if (!animParamTypes.ContainsKey(paramName))
                    continue;
                    
                switch (animParamTypes[paramName])
                {
                    case AnimatorControllerParameterType.Float:
                        stream.SendNext(animator.GetFloat(animParamHashes[paramName]));
                        break;
                    case AnimatorControllerParameterType.Bool:
                        stream.SendNext(animator.GetBool(animParamHashes[paramName]));
                        break;
                    case AnimatorControllerParameterType.Int:
                        stream.SendNext(animator.GetInteger(animParamHashes[paramName]));
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        // 트리거는 보통 동기화하지 않음 (상태 전이용이므로)
                        break;
                }
            }
        }
        // 데이터 수신 (다른 플레이어의 캐릭터)
        else
        {
            // 위치, 회전, 속도 수신
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            networkVelocity = (Vector3)stream.ReceiveNext();
            
            // 지연 보정
            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            networkPosition += networkVelocity * lag;
            
            // 애니메이션 파라미터 수신
            foreach (string paramName in syncedAnimParams)
            {
                if (!animParamTypes.ContainsKey(paramName))
                    continue;
                    
                switch (animParamTypes[paramName])
                {
                    case AnimatorControllerParameterType.Float:
                        float floatValue = (float)stream.ReceiveNext();
                        animator.SetFloat(animParamHashes[paramName], floatValue);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        bool boolValue = (bool)stream.ReceiveNext();
                        animator.SetBool(animParamHashes[paramName], boolValue);
                        break;
                    case AnimatorControllerParameterType.Int:
                        int intValue = (int)stream.ReceiveNext();
                        animator.SetInteger(animParamHashes[paramName], intValue);
                        break;
                }
            }
        }
    }
} 