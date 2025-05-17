using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallSync : MonoBehaviourPun, IPunObservable
{
    // 네트워크 관련 변수
    Vector3 networkPos;
    Vector3 networkVel;
    Vector3 lastNetworkPos;
    
    // 부드러운 동기화를 위한 변수
    [Header("동기화 설정")]
    [SerializeField] private float minSyncSpeed = 5f; // 가까운 거리에서의 최소 보간 속도
    [SerializeField] private float maxSyncSpeed = 15f; // 먼 거리에서의 최대 보간 속도
    [SerializeField] private float distanceMultiplier = 3f; // 거리에 따른 보간 속도 승수
    [SerializeField] private float velocityLerpSpeed = 3f; // 속도 보간 속도
    [SerializeField] private float delayCompensation = 0.5f; // 지연 보정 비율 (0.5 = 50%)
    [SerializeField] private float teleportThreshold = 5f; // 순간이동 임계값 (유닛)
    
    private float syncTime = 0.1f; // 동기화 간격 (포톤 기본값)
    private float syncDelay = 0; // 마지막 데이터 수신 후 경과 시간
    private float lastReceiveTime; // 마지막 데이터 수신 시간
    
    private Rigidbody rb;
    private bool hasReceivedData = false; // 첫 데이터 수신 여부

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        networkPos = rb.position;
        lastNetworkPos = rb.position;
        networkVel = Vector3.zero;
        lastReceiveTime = Time.time;
    }
    
    void Start()
    {
        // 마스터 클라이언트가 공의 소유권을 가짐
        if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient)
        {
            photonView.RequestOwnership();
        }
    }

    void FixedUpdate()
    {
        // 공의 속도 감시 및 보정 (내가 소유한 공일 경우)
        if (photonView.IsMine)
        {
            // 속도가 너무 낮으면 최소 속도 보장 (0보다 크고 1보다 작을 때만)
            if (rb.velocity.magnitude < 1f && rb.velocity.magnitude > 0.1f)
            {
                rb.velocity = rb.velocity.normalized * 1f;
            }
        }
        // 다른 클라이언트에서 수신받은 공일 경우
        else if (hasReceivedData)
        {
            // 수신 후 경과 시간 업데이트
            syncDelay = Mathf.Min(syncDelay + Time.fixedDeltaTime, 1.0f); // 최대 1초까지만 누적
            
            // 거리에 따른 보간 계수 계산 (멀수록 빠르게, 가까우면 천천히)
            float distance = Vector3.Distance(rb.position, networkPos);
            
            // 거리에 기반한 보간 속도 계산 (거리가 멀수록 빠르게)
            float interpSpeed = Mathf.Lerp(minSyncSpeed, maxSyncSpeed, 
                                          Mathf.Clamp01(distance / distanceMultiplier));
            
            // 보간 계수 계산 (Time.fixedDeltaTime이 적용된 속도)
            float t = Time.fixedDeltaTime * interpSpeed;
            t = Mathf.Clamp01(t); // 0~1 사이 값으로 제한
            
            // 네트워크 지연 보정을 위한 예측 위치 계산 (delayCompensation이 0.5면 50%만 적용)
            Vector3 targetPos = networkPos + (networkVel * syncDelay * delayCompensation);
            
            // 위치 보간 (부드러운 이동)
            Vector3 newPos = Vector3.Lerp(rb.position, targetPos, t);
            rb.MovePosition(newPos);
            
            // 속도 보간 (부드러운 속도 변화)
            if (networkVel.magnitude > 0.1f)
            {
                // velocityLerpSpeed가 높을수록 빠르게 네트워크 속도로 변경됨
                rb.velocity = Vector3.Lerp(rb.velocity, networkVel, 
                                          Time.fixedDeltaTime * velocityLerpSpeed);
            }
            
            // 디버깅용 - 현재 보간 상태
            Debug.DrawLine(rb.position, networkPos, Color.red);
            Debug.DrawLine(rb.position, targetPos, Color.green);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 내 공의 정보를 다른 클라이언트에게 보냄
            stream.SendNext(rb.position);
            stream.SendNext(rb.velocity);
        }
        else
        {
            // 이전 위치 저장
            lastNetworkPos = networkPos;
            
            // 다른 클라이언트로부터 공의 정보를 수신
            networkPos = (Vector3)stream.ReceiveNext();
            networkVel = (Vector3)stream.ReceiveNext();
            
            // 수신 시간 기록 및 경과 시간 초기화
            float previousReceiveTime = lastReceiveTime;
            lastReceiveTime = Time.time;
            syncDelay = 0f;
            
            // 첫 데이터 수신 표시
            if (!hasReceivedData)
            {
                hasReceivedData = true;
                rb.position = networkPos;
                rb.velocity = networkVel;
            }
            
            // 위치 차이가 임계값을 초과할 경우 순간이동 (텔레포트)
            float positionDifference = Vector3.Distance(rb.position, networkPos);
            if (positionDifference > teleportThreshold)
            {
                // 임계값 초과 시 순간이동
                rb.position = networkPos;
                rb.velocity = networkVel;
                Debug.LogWarning($"Ball teleported: difference was {positionDifference:F2} units");
            }
            
            // 정지된 공이 갑자기 움직이기 시작한 경우 바로 적용
            if (rb.velocity.magnitude < 0.1f && networkVel.magnitude > 1f)
            {
                rb.velocity = networkVel;
            }
        }
    }
}