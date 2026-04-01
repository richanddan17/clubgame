using UnityEngine;

/// <summary>
/// 플레이어 애니메이션 파라미터 제어 담당
/// PlayerMovement에서 값을 읽어와 Animator에 전달
/// </summary>
public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private PlayerMovement movement;

    void Start()
    {
        animator = GetComponent<Animator>();

        // 같은 오브젝트에 붙은 PlayerMovement 참조
        movement = GetComponent<PlayerMovement>();
    }

    /// <summary>
    /// 매 프레임 이동 상태를 Animator에 전달
    /// animstate : 0 = Idle, 1 = Run, 2 = Jump
    /// Grounded  : 지면 여부
    /// </summary>
    void LateUpdate()
    {
        if (movement == null || animator == null)
        {
            Debug.Log("Error: movement or animator is null!");
            return;
        }
        
        float moveInputValue = movement.MoveInput;
        float speedValue = movement.Speed;
        bool isGroundedValue = movement.IsGrounded;
        float currentSpeed = Mathf.Abs(moveInputValue) * speedValue;
        
        Debug.Log("MoveInput: " + moveInputValue + " / Speed: " + speedValue + " / CurrentSpeed: " + currentSpeed + " / IsGrounded: " + isGroundedValue);
        
        int animState = 0;
        
        if (!isGroundedValue)
        {
            animState = 2; // Jump
        }
        else if (currentSpeed >= 5f)
        {
            animState = 1; // Run
        }
        else
        {
            animState = 0; // Idle
        }
        
        animator.SetInteger("animstate", animState);
        animator.SetBool("Grounded", isGroundedValue);

        Debug.Log("AnimState: " + animState);
    }
}