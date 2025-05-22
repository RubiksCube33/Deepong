using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

[RequireComponent(typeof(Rigidbody))]
public class PlayerSetup : MonoBehaviourPunCallbacks
{
    [Header("플레이어 설정")]
    public bool isPlayerOne = false; // true면 player1, false면 player2
    
    [Header("스폰 위치")]
    public Vector3 player1Position = new Vector3(-9f, 1f, 0f); // 코트 왼쪽 끝
    public Vector3 player2Position = new Vector3(9f, 1f, 0f);  // 코트 오른쪽 끝
    
    [Header("스폰 회전")]
    public Quaternion player1Rotation = Quaternion.Euler(0, 90, 0);  // 오른쪽을 바라보도록
    public Quaternion player2Rotation = Quaternion.Euler(0, -90, 0); // 왼쪽을 바라보도록

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        // 약간의 딜레이 후 플레이어를 자기 위치로 이동 (다른 스크립트 이후 실행되도록)
        Invoke("SetInitialPosition", 0.1f);
    }

    void SetInitialPosition()
    {
        // 네트워크에 연결된 경우
        if (PhotonNetwork.IsConnected)
        {
            // 액터 번호에 따라 플레이어 결정 (1번이 player1, 2번이 player2)
            isPlayerOne = photonView.Owner.ActorNumber == 1;
        }
        
        // 플레이어 타입에 따라 위치 설정
        if (isPlayerOne)
        {
            transform.position = player1Position;
            // 설정된 회전값 사용
            transform.rotation = player1Rotation;
            Debug.Log($"PlayerSetup: Player 1 회전값 = {player1Rotation.eulerAngles}");
            Debug.Log("Player 1이 왼쪽 위치에 배치되었습니다.");
        }
        else
        {
            transform.position = player2Position;
            // 설정된 회전값 사용
            transform.rotation = player2Rotation;
            Debug.Log($"PlayerSetup: Player 2 회전값 = {player2Rotation.eulerAngles}");
            Debug.Log("Player 2가 오른쪽 위치에 배치되었습니다.");
        }
        
        // 위치 이동 후 물리 시뮬레이션 안정화
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    
    // 네트워크에서 플레이어가 참가했을 때 호출
    public override void OnJoinedRoom()
    {
        SetInitialPosition();
    }
}