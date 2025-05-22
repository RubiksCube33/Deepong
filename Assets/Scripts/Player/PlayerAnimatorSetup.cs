using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimatorSetup : MonoBehaviour
{
    [Header("애니메이션 동기화 설정")]
    [Tooltip("동기화할 애니메이션 파라미터 설정")]
    [SerializeField] private string[] animationParameters = new string[] { "Speed", "IsGrounded", "IsJumping" };
    
    [Header("애니메이터 컨트롤러")]
    [Tooltip("사용할 애니메이터 컨트롤러 설정")]
    [SerializeField] private RuntimeAnimatorController animatorController;
    
    private Animator animator;
    private PlayerMovementSync movementSync;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        movementSync = GetComponent<PlayerMovementSync>();
        
        // 애니메이터 컨트롤러 설정
        if (animatorController != null)
        {
            animator.runtimeAnimatorController = animatorController;
        }
        
        // 동기화할 애니메이션 파라미터 설정
        if (movementSync != null)
        {
            movementSync.SetAnimationParameters(animationParameters);
        }
    }
    
    // 애니메이션 파라미터 값 수동 설정 (디버깅용)
    public void SetAnimationParameter(string paramName, float value)
    {
        if (animator != null)
        {
            animator.SetFloat(paramName, value);
        }
    }
    
    public void SetAnimationParameter(string paramName, bool value)
    {
        if (animator != null)
        {
            animator.SetBool(paramName, value);
        }
    }
    
    public void SetAnimationParameter(string paramName, int value)
    {
        if (animator != null)
        {
            animator.SetInteger(paramName, value);
        }
    }
    
    public void TriggerAnimation(string triggerName)
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerName);
        }
    }
} 