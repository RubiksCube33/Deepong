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

    [Header("충돌 시 소리 설정")]
    private AudioSource sfxSource;
    
    // Trail Renderer 참조
    private TrailRenderer trailRenderer;
    
    
    [Header("사운드 재생 방식 선택")]
    [SerializeField] private bool useSoundManager = true; // true: SoundManager 사용, false: 직접 AudioSource 사용
    [SerializeField] private string wallBounceSoundName = "01_zapsplat_leisure_small_rubber_toy_ball_single_catch_002_106380";

    // 공 서브 위치
    private Vector3 player1BallPosition = new Vector3(0.045f, 1.004f, -2.57f);
    private Vector3 player2BallPosition = new Vector3(0.045f, 1.004f, 2.57f);
    
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();

        // Trail Renderer 컴포넌트 가져오기 및 비활성화
        // 처음에 비활성화 한번 명시적으로 시켜주는 이유는, 공이 2P로 가면 어색하게 트레일렌더러가 쭉 생김
        trailRenderer = GetComponent<TrailRenderer>();
        trailRenderer.enabled = false;
        
        // 초기 위치 설정 (1p, 2p에 따라 다르게 설정)
        if (Random.Range(0, 2) == 0)
        {
            transform.position = player1BallPosition;
        }
        else
        {
            transform.position = player2BallPosition;
        }

        // 현재 위치를 초기 위치로 저장
        initialPosition = transform.position;

        // 다시 트레일렌더러 켜주기
        trailRenderer.enabled = true;
        
        // AudioSource 컴포넌트 가져오기
        sfxSource = GetComponent<AudioSource>();
        
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
        
        PlayDirectAudioSource();

        // 점수 벽과의 충돌 감지
        if (collision.gameObject.CompareTag(wallBackTag))
        {

            
            // 게임이 끝났으면 점수 추가하지 않음
            if (scoreManager != null && scoreManager.IsGameEnded())
            {
                return;
            }
            
            // WallBack(플레이어1 뒤 벽)에 맞음 = 플레이어2의 점수 추가
            if (PhotonNetwork.IsConnected)
            {
                // 멀티플레이어 모드: 마스터 클라이언트가 점수 처리
                if (photonView.IsMine)
                {
                    // Player2가 득점
                    scoreManager.AddPlayer2Score();
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

            // 플레이어 1에게 서브권 제공
            StartCoroutine(ResetBallWithTrailDisable(player1BallPosition));
        }
        else if (collision.gameObject.CompareTag(wallFrontTag))
        {
            
            
            // 게임이 끝났으면 점수 추가하지 않음
            if (scoreManager != null && scoreManager.IsGameEnded())
            {
                return;
            }
            
            // WallFront(플레이어2 뒤 벽)에 맞음 = 플레이어1의 점수 추가
            if (PhotonNetwork.IsConnected)
            {
                // 멀티플레이어 모드: 마스터 클라이언트가 점수 처리
                if (photonView.IsMine)
                {
                    // Player1이 득점
                    scoreManager.AddPlayer1Score();
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

            // 플레이어 2에게 서브권 제공
            StartCoroutine(ResetBallWithTrailDisable(player2BallPosition));
        }
        
        // 패들 종류 검출
        else if (collision.gameObject.CompareTag("Paddle_Racket"))
        {
            HitBallByPaddle(collision, contact, hitDirection, forceDirection, forceMagnitude);
        }

        else if (collision.gameObject.CompareTag("Paddle_Sword"))
        {
            HitBallByPaddle(collision, contact, hitDirection, forceDirection, forceMagnitude);
        }

        else if (collision.gameObject.CompareTag("Paddle_Glove"))
        {
            HitBallByPaddle(collision, contact, hitDirection, forceDirection, forceMagnitude);
        }
        
        // 색상 변경
        Color newColor = new Color(Random.value, Random.value, Random.value);
        rend.material.color = newColor;
    }
    
    //공 충돌 움직임 처리 함수
    private void HitBallByPaddle(Collision collision, ContactPoint contact, Vector3 hitDirection, Vector3 forceDirection, float forceMagnitude)
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
    
    /// <summary>
    /// 직접 AudioSource를 사용하여 사운드를 재생합니다.
    /// </summary>
    private void PlayDirectAudioSource()
    {
        if (sfxSource != null)
        {
            sfxSource.Play();
        }
        else
        {
            Debug.LogWarning("AudioSource 또는 AudioClip이 설정되지 않았습니다.");
        }
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
    
    /// <summary>
    /// Trail Renderer를 올바르게 처리하면서 공 위치를 리셋하는 코루틴
    /// </summary>
    private IEnumerator ResetBallWithTrailDisable(Vector3 newPosition)
    {
        // Trail Renderer 비활성화 및 기존 궤적 제거
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
            trailRenderer.enabled = false;
        }
        
        // 물리 정지
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        // 위치 변경
        transform.position = newPosition;
        
        // 한 프레임 대기 (위치가 완전히 적용되도록)
        yield return null;
        
        // Trail Renderer 다시 활성화
        if (trailRenderer != null)
        {
            trailRenderer.enabled = true;
        }
    }
}