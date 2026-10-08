using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.UI.GridLayoutGroup;

public class KnockoutPlayer : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Punch")]
    [SerializeField] private float punchCooldown = 0.5f;
    [SerializeField] private float punchRange = 1.5f;
    [SerializeField] private float punchRadius = 0.8f;

    [Header("Knockback")]
    [SerializeField] private float knockbackDistance = 2.5f;
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("Jump")]
    [SerializeField] private float jumpAnimationDuration = 0.8f;
    [SerializeField] private float jumpForwardDistance = 1.5f;

    [Header("Got Hit")]
    [SerializeField] private float gotHitDuration = 0.9f;

    [Header("References")]
    [SerializeField] private Animator animator;

    private float lastPunchTime = -999f;

    private bool canMove = true;
    private bool isPunching = false;
    private bool isJumping = false;
    private bool isKnockedBack = false;
    private bool isGotHit = false;
    private bool isEliminated = false;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        KeepPlayerUpright();

        if (!canMove)
            return;

        if (isEliminated)
            return;

        if (isKnockedBack)
            return;

        if (isGotHit)
            return;

        HandleMovement();
        HandlePunch();
        HandleJump();
    }

    private void LateUpdate()
    {
        if (!IsOwner)
            return;

        KeepPlayerUpright();
    }
    private void KeepPlayerUpright()
    {
        Vector3 rotation = transform.eulerAngles;

        transform.rotation = Quaternion.Euler(
            0f,
            rotation.y,
            0f
        );
    }
    private void HandleMovement()
    {
        if (Keyboard.current == null)
            return;

        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed)
            input.y += 1f;

        if (Keyboard.current.sKey.isPressed)
            input.y -= 1f;

        if (Keyboard.current.aKey.isPressed)
            input.x -= 1f;

        if (Keyboard.current.dKey.isPressed)
            input.x += 1f;

        if (input.sqrMagnitude <= 0.01f)
        {
            SetWalkingAnimation(false);
            return;
        }

        input = input.normalized;

        Vector3 movement = new Vector3(
            input.x,
            0f,
            input.y
        );

        Vector3 newPosition =
            transform.position +
            movement *
            moveSpeed *
            Time.deltaTime;

        newPosition.y = transform.position.y;

        transform.position = newPosition;

        Quaternion targetRotation =
            Quaternion.LookRotation(movement);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

        KeepPlayerUpright();

        SetWalkingAnimation(true);
    }
    private void HandlePunch()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (isJumping || isGotHit || isEliminated)
            return;

        if (Time.time < lastPunchTime + punchCooldown)
            return;

        lastPunchTime = Time.time;

        PlayPunchAnimation();

        PunchServerRpc();
    }

    [ServerRpc]
    private void PunchServerRpc(
        ServerRpcParams rpcParams = default)
    {
        if (isEliminated)
            return;

        CheckForPunchHit();
    }

    private void CheckForPunchHit()
    {
        Vector3 punchCenter =
            transform.position +
            transform.forward *
            punchRange;

        punchCenter.y = transform.position.y;

        Collider[] hits =
            Physics.OverlapSphere(
                punchCenter,
                punchRadius
            );

        foreach (Collider hit in hits)
        {
            KnockoutPlayer target =
                hit.GetComponentInParent<KnockoutPlayer>();

            if (target == null)
                continue;

            if (target == this)
                continue;

            if (!target.IsSpawned)
                continue;

            if (target.isEliminated)
                continue;

            Vector3 knockbackDirection =
                target.transform.position -
                transform.position;

            knockbackDirection.y = 0f;

            if (knockbackDirection.sqrMagnitude <= 0.01f)
            {
                knockbackDirection = transform.forward;
                knockbackDirection.y = 0f;
            }

            knockbackDirection.Normalize();

            target.ReceiveHit(
                knockbackDirection
            );

            break;
        }
    }

    private void PlayPunchAnimation()
    {
        if (animator == null)
            return;

        animator.SetBool(
            "IsMoving",
            false
        );

        animator.ResetTrigger(
            "Punch"
        );

        animator.SetTrigger(
            "Punch"
        );

        isPunching = true;

        CancelInvoke(
            nameof(FinishPunch)
        );

        Invoke(
            nameof(FinishPunch),
            0.4f
        );
    }

    private void FinishPunch()
    {
        isPunching = false;

        UpdateMovementAnimation();
    }
    private void ReceiveHit(
        Vector3 knockbackDirection)
    {
        if (!IsServer)
            return;

        if (isEliminated)
            return;

        ClientRpcParams rpcParams =
            new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds =
                        new[] { OwnerClientId }
                }
            };

        ReceiveHitClientRpc(
            knockbackDirection,
            rpcParams
        );
    }

    [ClientRpc]
    private void ReceiveHitClientRpc(
        Vector3 knockbackDirection,
        ClientRpcParams rpcParams = default)
    {
        if (isEliminated)
            return;

        isGotHit = true;

        if (animator != null)
        {
            animator.SetBool(
                "IsMoving",
                false
            );

            animator.ResetTrigger(
                "GotHit"
            );

            animator.SetTrigger(
                "GotHit"
            );

            CancelInvoke(
                nameof(FinishGotHit)
            );

            Invoke(
                nameof(FinishGotHit),
                gotHitDuration
            );
        }

        StartCoroutine(
            KnockbackCoroutine(
                knockbackDirection
            )
        );
    }

    private void FinishGotHit()
    {
        if (isEliminated)
            return;

        isGotHit = false;

        KeepPlayerUpright();

        if (animator != null)
        {
            animator.SetBool(
                "IsMoving",
                false
            );
        }

        UpdateMovementAnimation();
    }

    private IEnumerator KnockbackCoroutine(
        Vector3 direction)
    {
        if (isEliminated)
            yield break;

        isKnockedBack = true;

        float fixedY =
            transform.position.y;

        Vector3 startPosition =
            transform.position;

        Vector3 targetPosition =
            startPosition +
            direction *
            knockbackDistance;

        targetPosition.y = fixedY;

        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            if (isEliminated)
                yield break;

            elapsed += Time.deltaTime;

            float progress =
                elapsed /
                knockbackDuration;

            Vector3 newPosition =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    progress
                );

            newPosition.y = fixedY;

            transform.position =
                newPosition;

            KeepPlayerUpright();

            yield return null;
        }

        targetPosition.y = fixedY;

        transform.position =
            targetPosition;

        KeepPlayerUpright();

        isKnockedBack = false;
    }
    private void HandleJump()
    {
        if (Keyboard.current == null)
            return;

        if (!Keyboard.current.spaceKey.wasPressedThisFrame)
            return;

        if (isPunching)
            return;

        if (isGotHit)
            return;

        if (isEliminated)
            return;

        if (isJumping)
            return;

        PlayJumpAnimation();
    }

    private void PlayJumpAnimation()
    {
        if (animator == null)
            return;

        isJumping = true;

        animator.SetBool(
            "IsMoving",
            false
        );

        animator.ResetTrigger(
            "Jump"
        );

        animator.SetTrigger(
            "Jump"
        );

        StartCoroutine(
            JumpMovementCoroutine()
        );

        CancelInvoke(
            nameof(FinishJump)
        );

        Invoke(
            nameof(FinishJump),
            jumpAnimationDuration
        );
    }

    private IEnumerator JumpMovementCoroutine()
    {
        float elapsed = 0f;

        Vector3 jumpDirection =
            transform.forward;

        while (elapsed < jumpAnimationDuration)
        {
            if (!canMove ||
                isGotHit ||
                isEliminated)
            {
                yield break;
            }

            elapsed += Time.deltaTime;

            Vector3 movement =
                jumpDirection *
                (jumpForwardDistance /
                jumpAnimationDuration) *
                Time.deltaTime;

            transform.position += movement;

            KeepPlayerUpright();

            yield return null;
        }
    }

    private void FinishJump()
    {
        if (isEliminated)
            return;

        isJumping = false;

        KeepPlayerUpright();

        UpdateMovementAnimation();
    }
    private void SetWalkingAnimation(
        bool walking)
    {
        if (animator == null)
            return;

        if (isPunching)
            return;

        if (isJumping)
            return;

        if (isKnockedBack)
            return;

        if (isGotHit)
            return;

        if (isEliminated)
            return;

        animator.SetBool(
            "IsMoving",
            walking
        );
    }

    private void UpdateMovementAnimation()
    {
        if (animator == null)
            return;

        if (isPunching)
            return;

        if (isJumping)
            return;

        if (isKnockedBack)
            return;

        if (isGotHit)
            return;

        if (isEliminated)
            return;

        if (Keyboard.current == null)
        {
            animator.SetBool(
                "IsMoving",
                false
            );

            return;
        }

        bool moving =
            Keyboard.current.wKey.isPressed ||
            Keyboard.current.sKey.isPressed ||
            Keyboard.current.aKey.isPressed ||
            Keyboard.current.dKey.isPressed;

        animator.SetBool(
            "IsMoving",
            moving
        );
    }
    public void Eliminate()
    {
        if (isEliminated)
            return;

        if (IsServer)
        {
            EliminateOnServer();
        }
        else
        {
            RequestEliminationServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestEliminationServerRpc(
        ServerRpcParams rpcParams = default)
    {
        EliminateOnServer();
    }

    private void EliminateOnServer()
    {
        if (isEliminated)
            return;

        isEliminated = true;


        if (KnockoutGameManager.Instance != null)
        {
            KnockoutGameManager.Instance.PlayerEliminated(
                OwnerClientId
            );
        }

        EliminateClientRpc();
    }

    [ClientRpc]
    private void EliminateClientRpc()
    {
        ApplyElimination();
    }

    private void ApplyElimination()
    {
        isEliminated = true;

        canMove = false;
        isPunching = false;
        isJumping = false;
        isKnockedBack = false;
        isGotHit = false;

        StopAllCoroutines();
        CancelInvoke();

        if (animator != null)
        {
            animator.SetBool(
                "IsMoving",
                false
            );

            animator.ResetTrigger(
                "Punch"
            );

            animator.ResetTrigger(
                "Jump"
            );

            animator.ResetTrigger(
                "GotHit"
            );
        }
    }

    public bool IsEliminated()
    {
        return isEliminated;
    }
    public void SetCanMove(
        bool value)
    {
        canMove = value;

        if (!value)
        {
            isPunching = false;
            isJumping = false;
            isKnockedBack = false;
            isGotHit = false;

            CancelInvoke();

            if (animator != null)
            {
                animator.SetBool(
                    "IsMoving",
                    false
                );

                animator.ResetTrigger(
                    "Punch"
                );

                animator.ResetTrigger(
                    "Jump"
                );

                animator.ResetTrigger(
                    "GotHit"
                );
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Vector3 punchCenter =
            transform.position +
            transform.forward *
            punchRange;

        punchCenter.y =
            transform.position.y;

        Gizmos.DrawWireSphere(
            punchCenter,
            punchRadius
        );
    }

    private void OnDisable()
    {
        CancelInvoke();
    }
}
