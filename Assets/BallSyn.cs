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

    void FixedUpdate()
    {
        if (!photonView.IsMine)
        {
            Vector3 nextPos = Vector3.Lerp(rb.position, networkPos, Time.fixedDeltaTime * 15f);
            rb.MovePosition(nextPos); // 더 자연스러움
        }
    }


    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(rb.velocity);
        }
        else
        {
            networkPos = (Vector3)stream.ReceiveNext();
            networkVel = (Vector3)stream.ReceiveNext();

            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            networkPos += networkVel * lag;
        }
    }

}