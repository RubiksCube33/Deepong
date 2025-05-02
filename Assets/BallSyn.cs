using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallSyn : MonoBehaviourPun, IPunObservable
{
    // 네트워크 관련 변수
    Vector3 networkPos;
    Vector3 networkVel;
    
    // 부드러운 동기화를 위한 변수
    float syncTime = 0.1f; // 더 짧은 고정값 사용
    private float syncDelay = 0; // 마지막 데이터 수신 후 경과 시간
    
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        networkPos = rb.position;
        networkVel = Vector3.zero;
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine)
        {
            // 수신 후 경과 시간 업데이트
            syncDelay += Time.fixedDeltaTime;
            
            // 거리에 따른 보간 계수 계산 (멀수록 빠르게, 가까우면 천천히)
            float distance = Vector3.Distance(rb.position, networkPos);
            float t = Time.fixedDeltaTime * (5f + distance * 2f); // 거리에 따라 보간 속도 조정
            t = Mathf.Clamp01(t); // 0~1 사이 값으로 제한
            
            // 위치 보간
            Vector3 targetPos = networkPos + (networkVel * syncDelay * 0.5f); // 지연 보정 50%만 적용
            Vector3 newPos = Vector3.Lerp(rb.position, targetPos, t);
            rb.MovePosition(newPos);
            
            // 속도 부분 적용 (완전히 덮어쓰지 않음)
            if (networkVel.magnitude > 0.1f)
            {
                rb.velocity = Vector3.Lerp(rb.velocity, networkVel, Time.fixedDeltaTime * 3f);
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(rb.position);
            stream.SendNext(rb.velocity);
        }
        else
        {
            networkPos = (Vector3)stream.ReceiveNext();
            networkVel = (Vector3)stream.ReceiveNext();
            
            // 수신 시 경과 시간 초기화
            syncDelay = 0f;
            
            // 위치 차이가 너무 클 경우에만 순간이동 (텔레포트 임계값)
            float positionDifference = Vector3.Distance(rb.position, networkPos);
            if (positionDifference > 5f) // 5유닛 이상 차이날 경우
            {
                rb.position = networkPos;
                rb.velocity = networkVel;
            }
        }
    }
}