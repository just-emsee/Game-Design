using Mono.Cecil.Cil;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private BoxCollider2D col;
    private float lastGroundTime = 0;

    [Header("Movement Values")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float coyoteTime = .02f;
    [SerializeField] private float jumpForce = 7f;

    [Header("Ground Layer")]
    [SerializeField] private LayerMask groundLayer;

    private InputActions input = null;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
    }

    private void Awake() {
        input = new InputActions();
    }

    void OnEnable() {
        input.Enable();
    }

    private void OnDisable() {
        input.Disable();
    }

    private void FixedUpdate()
    {
        Movement();
    }

    void Update()
    {
        GroundedCheck();
        if (input.Player.Jump.IsPressed()) {
            Jump();
        }
    }

    private void Movement()
    {
        rb.linearVelocityX = input.Player.Move.ReadValue<float>() * moveSpeed;
    }

    private void Jump()
    {
        if (CanJump())
        {
            rb.linearVelocityY = jumpForce;
            lastGroundTime = 0;
        }
    }

    private bool CanJump() {
        return IsGrounded() || lastGroundTime + coyoteTime >= Time.time;
    }

    private void GroundedCheck() { 
        if (IsGrounded()) {
            lastGroundTime = Time.time;
        }
    }

    private bool IsGrounded()
    {
        if (rb.linearVelocityY > .001) return false;
        RaycastHit2D hit = Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0f, Vector2.down, .015f, groundLayer);
        return hit && !hit.collider.isTrigger;
    }
}
