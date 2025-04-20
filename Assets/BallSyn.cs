using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallSyn : MonoBehaviourPun, IPunObservable
{
    Vector3 networkPos;
    Vector3 networkVel;
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        networkPos = transform.position;
        networkVel = Vector3.zero;
    }

    void Update()
    {
        if (!photonView.IsMine)
        {
            // 보간(Lerp) + 외삽(Extrapolation) 예시
            networkPos += networkVel * Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, networkPos, Time.deltaTime * 10f);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 내 오브젝트: 위치와 속도 전송
            stream.SendNext(transform.position);
            stream.SendNext(rb.velocity);
        }
        else
        {
            // 원격 오브젝트: 수신
            networkPos = (Vector3)stream.ReceiveNext();
            networkVel = (Vector3)stream.ReceiveNext();
        }
    }
}