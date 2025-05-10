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

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        // 게임 시작 시 플레이어를 자기 위치로 이동
        SetInitialPosition();
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
            Debug.Log("Player 1이 왼쪽 위치에 배치되었습니다.");
        }
        else
        {
            transform.position = player2Position;
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