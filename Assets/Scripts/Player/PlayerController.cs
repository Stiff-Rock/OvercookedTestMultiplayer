using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    // References
    private CharacterController characterController;
    private Animator animator;
    private static readonly int isWalkingHash = Animator.StringToHash("IsWalking");

    // Values
    [Header("VALUES")]
    private bool active;
    private Vector3 moveDirection;
    [SerializeField] private float movementSpeed = 5;
    [SerializeField] private float rotationSpeed = 50;
    private bool isWalking;

    // Controls
    [Header("CONTROLS")]
    [SerializeField] private Key forwardKey;
    [SerializeField] private Key leftKey;
    [SerializeField] private Key backwardKey;
    [SerializeField] private Key rightKey;


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