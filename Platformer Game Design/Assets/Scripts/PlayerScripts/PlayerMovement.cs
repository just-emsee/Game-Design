using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private BoxCollider2D col;

    [Header("Movement Values")]
    [SerializeField]
    private float moveSpeed = 10f;
    [SerializeField]
    private float jumpForce = 7f;

    [Header("Ground Layer")]
    [SerializeField]
    private LayerMask groundLayer;

    private InputAction walkInput;
    private InputAction moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();

        InitializeInputs();
    }

    private void InitializeInputs()
    {
        walkInput = InputSystem.actions.FindAction("Move");
    }

    private void FixedUpdate()
    {
        Movement();
    }

    void Update()
    {
        
    }

    private void Movement()
    {
        rb.linearVelocityX = walkInput.ReadValue<Vector2>().x * moveSpeed;
    }

    public void Jump()
    {
        Debug.Log("jump");
        if (IsGrounded())
        {
            rb.linearVelocityY = jumpForce;
        }
    }

    private bool IsGrounded()
    {
        return Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0f, Vector2.down, .01f, groundLayer);
    }
}
