using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 500f;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private Vector3 groundCheckOffset;
    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private Camera playerCamera; // Cámara del jugador

    private bool isGrounded;
    private float ySpeed;
    private Quaternion targetRotation;
    private Animator animator;
    private CharacterController characterController;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();

        if (animator == null)
        {
            Debug.LogError("Animator no está asignado en el objeto del jugador.");
        }
        if (characterController == null)
        {
            Debug.LogError("CharacterController no está asignado en el objeto del jugador.");
        }
    }

    private void Start()
    {
        if (!IsOwner && playerCamera != null)
        {
            playerCamera.gameObject.SetActive(false); // Desactiva la cámara para jugadores remotos
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleMovement();
    }

    private void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        float moveAmount = Mathf.Clamp01(Mathf.Abs(h) + Mathf.Abs(v));
        Vector3 moveInput = new Vector3(h, 0, v).normalized;
        Vector3 moveDir = playerCamera.transform.TransformDirection(moveInput);
        moveDir.y = 0;

        GroundCheck();
        if (isGrounded)
        {
            ySpeed = -0.5f;
        }
        else
        {
            ySpeed += Physics.gravity.y * Time.deltaTime;
        }

        Vector3 velocity = moveDir * moveSpeed;
        velocity.y = ySpeed;

        characterController.Move(velocity * Time.deltaTime);

        if (moveAmount > 0)
        {
            targetRotation = Quaternion.LookRotation(moveDir);
        }

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        animator.SetFloat("moveAmount", moveAmount, 0.2f, Time.deltaTime);
    }

    private void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(transform.TransformPoint(groundCheckOffset), groundCheckRadius, groundLayer);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.DrawSphere(transform.TransformPoint(groundCheckOffset), groundCheckRadius);
    }

    public void InitializeCamera()
    {
        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);
        }
    }
}

