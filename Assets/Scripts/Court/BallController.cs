using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Random = UnityEngine.Random;

public class BallController : MonoBehaviourPunCallbacks
{
    private Rigidbody rb;
    private float speed = 150f;
    private Renderer rend;
    
    [Header("VR 상호작용 설정")]
    public float baseForce = 10.0f;
    public float velocityMultiplier = 0.5f;

    [Header("게임 설정")]
    private Vector3 initialPosition;  // 시작 시 현재 위치를 저장
    [SerializeField] private string wallBackTag = "wallback";  // 플레이어1 뒤 벽
    [SerializeField] private string wallFrontTag = "wallfront"; // 플레이어2 뒤 벽

    // 점수 관리를 위한 참조
    private ScoreManager scoreManager;
    
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();
        
        // 현재 위치를 초기 위치로 저장
        initialPosition = transform.position;
        
        // 씬에서 ScoreManager 찾기
        scoreManager = FindObjectOfType<ScoreManager>();
        if (scoreManager == null)
        {
            Debug.LogError("ScoreManager를 찾을 수 없습니다!");
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint contact = collision.contacts[0];
        Vector3 hitDirection = (collision.transform.position - contact.point).normalized;
        Vector3 forceDirection = hitDirection;
        float forceMagnitude = baseForce;
        
        // 점수 벽과의 충돌 감지
        if (collision.gameObject.CompareTag(wallBackTag))
        {
            // WallBack(플레이어1 뒤 벽)에 맞음 = 플레이어2의 점수 추가
            if (PhotonNetwork.IsConnected)
            {
                // 멀티플레이어 모드: 플레이어2가 플레이어1의 뒤 벽을 맞춤 = 플레이어2 득점
                if (photonView.IsMine)
                {
                    // 플레이어2에게 점수 추가 (플레이어2가 직접 자신의 점수를 올림)
                    if (PhotonNetwork.LocalPlayer.ActorNumber == 2)
                    {
                        scoreManager.AddScore(); // 플레이어2 자신의 점수 추가
                    }
                    ResetBallPosition();
                }
            }
            else
            {
                // 싱글플레이어 모드: 플레이어1 뒤 벽 = 상대편(AI) 득점
                if (scoreManager != null)
                {
                    scoreManager.AddOpponentScore(); // 상대편(AI) 점수 추가
                    ResetBallPosition();
                }
            }
        }
        else if (collision.gameObject.CompareTag(wallFrontTag))
        {
            // WallFront(플레이어2 뒤 벽)에 맞음 = 플레이어1의 점수 추가
            if (PhotonNetwork.IsConnected)
            {
                // 멀티플레이어 모드: 플레이어1이 플레이어2의 뒤 벽을 맞춤 = 플레이어1 득점
                if (photonView.IsMine)
                {
                    // 플레이어1에게 점수 추가 (플레이어1이 직접 자신의 점수를 올림)
                    if (PhotonNetwork.LocalPlayer.ActorNumber == 1)
                    {
                        scoreManager.AddScore(); // 플레이어1 자신의 점수 추가
                    }
                    ResetBallPosition();
                }
            }
            else
            {
                // 싱글플레이어 모드: 플레이어2 뒤 벽 = 플레이어 득점
                if (scoreManager != null)
                {
                    scoreManager.AddScore(); // 플레이어 점수 추가
                    ResetBallPosition();
                }
            }
        }
        
        // VR 컨트롤러 검출
        else if (collision.gameObject.CompareTag("VRController"))
        {
            // VR 컨트롤러의 속도 정보 가져오기
            Rigidbody controllerRb = collision.gameObject.GetComponent<Rigidbody>();
            if (controllerRb != null)
            {
                // 컨트롤러의 속도를 기본 힘에 더함
                Vector3 controllerVelocity = controllerRb.velocity;
                forceDirection = (hitDirection + controllerVelocity.normalized) / 2f;
                forceMagnitude = baseForce + (controllerVelocity.magnitude * velocityMultiplier);
                rb.velocity = forceDirection * forceMagnitude;
                
                Debug.Log($"컨트롤러 속도: {controllerVelocity.magnitude}, 적용된 힘: {forceMagnitude}");
            }
        }
        
        // 색상 변경
        Color newColor = new Color(Random.value, Random.value, Random.value);
        rend.material.color = newColor;
    }
    
    // 공 위치 초기화
    public void ResetBallPosition()
    {
        // 멀티플레이어에서만 ownership 체크
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return;
            
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = initialPosition;
        
        // 네트워크 게임에서 다른 클라이언트들에게도 공 위치 초기화 알림
        if (PhotonNetwork.IsConnected && photonView.IsMine)
        {
            photonView.RPC("RPC_ResetBallPosition", RpcTarget.Others);
        }
    }
    
    [PunRPC]
    private void RPC_ResetBallPosition()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = initialPosition;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}