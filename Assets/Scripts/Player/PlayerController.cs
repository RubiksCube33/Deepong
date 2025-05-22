using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerMovementSync))]
public class PlayerController : MonoBehaviourPun
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    
    [Header("물리 설정")]
    [SerializeField] private float groundCheckDistance = 0.3f;
    [SerializeField] private LayerMask groundLayer;
    
    // 컴포넌트 캐싱
    private Rigidbody rb;
    private Animator animator;
    private PlayerMovementSync movementSync;
    
    // 상태 변수
    private bool isGrounded = true;
    private Vector3 moveDirection;
    private bool isJumping = false;
    
    // 애니메이션 파라미터 해시
    private int speedHash;
    private int isGroundedHash;
    private int isJumpingHash;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        movementSync = GetComponent<PlayerMovementSync>();
        
        // 애니메이션 해시 초기화
        speedHash = Animator.StringToHash("Speed");
        isGroundedHash = Animator.StringToHash("IsGrounded");
        isJumpingHash = Animator.StringToHash("IsJumping");
    }
    
    private void Start()
    {
        // 내 캐릭터가 아니면 Rigidbody 물리 시뮬레이션 끄기
        if (!photonView.IsMine)
        {
            rb.isKinematic = true;
        }
    }
    
    private void Update()
    {
        // 내 캐릭터만 직접 제어
        if (!photonView.IsMine)
            return;
            
        // 입력 처리
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        
        // 이동 방향 계산
        moveDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;
        
        // 점프 입력 처리
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            isJumping = true;
        }
        
        // 애니메이션 파라미터 업데이트
        UpdateAnimationParameters();
    }
    
    private void FixedUpdate()
    {
        // 내 캐릭터만 물리 적용
        if (!photonView.IsMine)
            return;
            
        // 지면 체크
        CheckGrounded();
        
        // 이동 처리
        if (moveDirection.magnitude > 0.1f)
        {
            // 이동 방향으로 회전
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            
            // 앞으로 이동
            rb.MovePosition(rb.position + transform.forward * moveSpeed * moveDirection.magnitude * Time.fixedDeltaTime);
        }
        
        // 점프 처리
        if (isJumping)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isJumping = false;
            isGrounded = false;
        }
    }
    
    // 지면 체크
    private void CheckGrounded()
    {
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
    }
    
    // 애니메이션 파라미터 업데이트
    private void UpdateAnimationParameters()
    {
        if (animator != null)
        {
            // 이동 속도 파라미터 업데이트
            animator.SetFloat(speedHash, moveDirection.magnitude * moveSpeed);
            
            // 접지 상태 파라미터 업데이트
            animator.SetBool(isGroundedHash, isGrounded);
            
            // 점프 상태 파라미터 업데이트
            animator.SetBool(isJumpingHash, isJumping);
        }
    }
    
    // 네트워크 오브젝트 초기화 완료 후 호출
    public override void OnEnable()
    {
        base.OnEnable();
        
        // 물리 충돌 감지 설정
        if (photonView.IsMine)
        {
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        else
        {
            rb.isKinematic = true;
        }
    }
} 