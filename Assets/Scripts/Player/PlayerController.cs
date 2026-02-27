using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(ClientNetworkAnimator))]
public class PlayerController : NetworkBehaviour
{
    // References
    private CharacterController characterController;
    private Animator animator;
    private static readonly int isWalkingHash = Animator.StringToHash("IsWalking");

    // Values
    [Header("VALUES")]
    [SerializeField] private float movementSpeed = 5;
    [SerializeField] private float rotationSpeed = 50;
    [SerializeField] private float fixedYPos = 1.08f;
    private Vector3 moveDirection;

    // Controls
    [Header("CONTROLS")]
    [SerializeField] private Key forwardKey;
    [SerializeField] private Key leftKey;
    [SerializeField] private Key backwardKey;
    [SerializeField] private Key rightKey;

    // Flags
    private bool active;
    private bool isWalking;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        active = true;
    }

    private void Update()
    {
        if (!IsOwner || !active) return;

        GatherInputs();
        ApplyMovement();
    }

    private void LateUpdate()
    {
        SetVerticalPosition();
    }

    private void GatherInputs()
    {
        moveDirection = Vector3.zero;

        if (Keyboard.current[forwardKey].isPressed)
            moveDirection += Vector3.forward;

        if (Keyboard.current[leftKey].isPressed)
            moveDirection += -Vector3.right;

        if (Keyboard.current[backwardKey].isPressed)
            moveDirection += -Vector3.forward;

        if (Keyboard.current[rightKey].isPressed)
            moveDirection += Vector3.right;
    }

    private void ApplyMovement()
    {
        bool isCurrentlyMoving = moveDirection != Vector3.zero;
        if (isWalking != isCurrentlyMoving)
        {
            isWalking = isCurrentlyMoving;
            animator.SetBool(isWalkingHash, isWalking);
        }

        if (!isWalking) return;

        Vector3 moveDirectionNormalized = moveDirection.normalized;

        // Rotate to movement direction
        Quaternion rotationTarget = Quaternion.LookRotation(moveDirectionNormalized);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rotationTarget, rotationSpeed * Time.deltaTime);

        // Move the player
        characterController.SimpleMove(moveDirectionNormalized * movementSpeed);
    }

    private void SetVerticalPosition()
    {
        Vector3 pos = transform.position;
        pos.y = fixedYPos;
        transform.position = pos;
    }

    public void ToggleActive(bool active)
    {
        if (!active)
        {
            animator.SetBool(isWalkingHash, false);
            moveDirection = Vector3.zero;
        }

        this.active = active;
    }
}