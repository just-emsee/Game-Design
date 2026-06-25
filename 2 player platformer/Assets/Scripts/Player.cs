using System;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]

public class Player : MonoBehaviour
{
    private enum GroundedType {
        None,
        Ground,
        Player
    }

    [Header("Movement Values")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float airMoveForce = 5f;
    [SerializeField] private float coyoteTime = .02f;
    [SerializeField] private float jumpForce = 7f;

    [Header("Ground Layer")]
    [SerializeField] private LayerMask jumpableLayer;
    [SerializeField] private Collider2D otherPlayer;

    [Header("Ability")]
    [SerializeField] private PlayerAbility ability;
    [SerializeField] private int totalExtraJumps = 1;

    private Rigidbody2D rb;
    private BoxCollider2D col;
    private float lastGroundTime = 0;
    private Vector3 startPos = Vector3.zero;
    private int extraJumps = 0;
    public float lastInputValue { get; private set; }

    private void Start() {
        startPos = transform.position;
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
    }

    private void Update() {
        GroundedCheck();
    }

    public void InputAxis(float value) {
        lastInputValue = value;
        if (lastGroundTime > 0 || IsTouchingGroundOrGroundedPlayer())
            rb.linearVelocityX = value * moveSpeed;
        else {
            rb.AddForceX(value * airMoveForce);
            if (ability == null || !ability.overridingMaxSpeed()) rb.linearVelocityX = Mathf.Clamp(rb.linearVelocityX,-moveSpeed,moveSpeed);
        }
    }

    public void InputJump() {
        GroundedType groundType = IsGrounded();
        if (CanJump(groundType)) {
            float calculatedJumpForce = jumpForce;

            if (
                lastGroundTime <= 0 &&
                groundType == GroundedType.Player &&
                !otherPlayer.GetComponent<Player>().IsTouchingGround()
                ) {
                calculatedJumpForce /= 2;
            }


            rb.linearVelocityY = calculatedJumpForce;
            lastGroundTime = 0;
        }
    }


    internal void InputDoubleJump() {
        GroundedType groundType = IsGrounded();
        if (CanExtraJump(groundType)) {
            extraJumps--;
            rb.linearVelocityY = jumpForce;
            rb.linearVelocityX = lastInputValue*moveSpeed;
            lastGroundTime = 0;
        }
    }



    private bool CanJump(GroundedType type) {
        return type == GroundedType.Ground || type == GroundedType.Player || lastGroundTime + coyoteTime >= Time.time;
    }

    private bool CanExtraJump(GroundedType type) {
        return extraJumps > 0 && type == GroundedType.None && !(lastGroundTime + coyoteTime >= Time.time);
    }

    private void GroundedCheck() {
        if (IsTouchingGroundOrGroundedPlayer()) {
            lastGroundTime = Time.time;
            extraJumps = totalExtraJumps;
        }
    }

    private GroundedType IsGrounded() {
        if (rb.linearVelocityY > .001) return GroundedType.None;
        RaycastHit2D hit = Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0f, Vector2.down, .015f, jumpableLayer);
        if (hit) {
            if (hit.collider == col)
                return GroundedType.None;
            if (hit.collider == otherPlayer)
                return GroundedType.Player;
            if (!hit.collider.isTrigger)
                return GroundedType.Ground;
        }
        return GroundedType.None;
    }

    public bool IsTouchingGround () {
        return IsGrounded() == GroundedType.Ground;
    }

    public bool IsTouchingGroundOrPlayer() {
        GroundedType groundType = IsGrounded();

        return
            groundType == GroundedType.Ground ||
            groundType == GroundedType.Player
            ;
    }

    public bool IsTouchingGroundOrGroundedPlayer() {
        GroundedType groundType = IsGrounded();

        return
            groundType == GroundedType.Ground ||
            (
            groundType == GroundedType.Player &&
            otherPlayer.GetComponent<Player>().IsTouchingGround()
            );
    }

    private void SetPosition(Vector2 newPos) {
        transform.position = newPos;
    }

    public void AbilityPressed() {
        ability?.TryActivate();
    }

    public void ResetPos() {
        transform.position = startPos;
    }

}
