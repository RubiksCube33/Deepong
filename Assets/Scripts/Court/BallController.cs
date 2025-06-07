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
    
    [Header("패들별 타격음 설정")]
    [SerializeField] private AudioClip defaultRacketSound;   // 기본 라켓 타격음
    [SerializeField] private AudioClip swordSound;           // 칼 타격음  
    [SerializeField] private AudioClip boxingGloveSound;     // 복싱 글러브 타격음
    
    
    [Header("사운드 재생 방식 선택")]
    [SerializeField] private bool useSoundManager = true; // true: SoundManager 사용, false: 직접 AudioSource 사용
    [SerializeField] private string wallBounceSoundName = "01_zapsplat_leisure_small_rubber_toy_ball_single_catch_002_106380";
    
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();
        
        // 현재 위치를 초기 위치로 저장
        initialPosition = transform.position;
        
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
        }
        
        // VR 컨트롤러 검출
        else if (collision.gameObject.CompareTag("VRController"))
        {
            // 패들 타입에 따른 사운드 재생
            PlayPaddleSound(collision.gameObject);
            
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
        else
        {
            // 일반 벽이나 다른 오브젝트와의 충돌
            PlayDirectAudioSource();
        }
        
        // 색상 변경
        Color newColor = new Color(Random.value, Random.value, Random.value);
        rend.material.color = newColor;
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
    
    /// <summary>
    /// 패들 타입에 따라 적절한 타격음을 재생합니다.
    /// </summary>
    /// <param name="paddleObject">충돌한 패들 오브젝트</param>
    private void PlayPaddleSound(GameObject paddleObject)
    {
        if (sfxSource == null) return;
        
        AudioClip soundToPlay = null;
        
        // 패들 오브젝트의 이름이나 태그를 통해 타입 감지
        string paddleName = paddleObject.name;
        
        if (paddleName.Contains("Paddle_Sword") || paddleName.Contains("칼"))
        {
            soundToPlay = swordSound;
        }
        else if (paddleName.Contains("boxing") || paddleName.Contains("Paddle_Glove") || paddleName.Contains("글러브"))
        {
            soundToPlay = boxingGloveSound;
        }
        else if (paddleName.Contains("Paddle_Racket") || paddleName.Contains("라켓") || paddleName.Contains("default"))
        {
            soundToPlay = defaultRacketSound;
        }
        else
        {
            // 기본값으로 라켓 사운드 사용
            soundToPlay = defaultRacketSound;
        }
        
        // 사운드 재생
        if (soundToPlay != null)
        {
            sfxSource.clip = soundToPlay;
            sfxSource.Play();
            Debug.Log($"패들 타격음 재생: {soundToPlay.name}");
        }
        else
        {
            // AudioClip이 설정되지 않은 경우 기본 사운드 재생
            PlayDirectAudioSource();
            Debug.LogWarning($"패들 타입 '{paddleName}'에 대한 AudioClip이 설정되지 않았습니다. 기본 사운드를 재생합니다.");
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
}