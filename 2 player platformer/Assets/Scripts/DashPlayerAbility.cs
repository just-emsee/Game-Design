
using UnityEngine;

[RequireComponent(typeof(Player))]
[RequireComponent(typeof(Rigidbody2D))]
public class DashPlayerAbility : PlayerAbility {
    bool usable = false;
    float inDashTimer = 0;
    Player player;
    Rigidbody2D rb;

    [SerializeField] float dashPower = 35;
    [SerializeField] float dashTime = .2f;

    public override bool CanUseNow() {
        return usable && player.lastInputValue != 0;
    }

    protected override void Activate() {
        usable = false;
        inDashTimer = dashTime;
        if (player.lastInputValue > 0) rb.linearVelocityX = dashPower;
        else if (player.lastInputValue < 0) rb.linearVelocityX = -dashPower;
        rb.linearVelocityY = 0;
    }

    private void Start() {
        player = GetComponent<Player>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update() {
        inDashTimer -= Time.deltaTime;
        if (inDashTimer > 0) {
            //rb.gravityScale = 0;
            rb.linearVelocityY = 0;
        }
        if (player.IsTouchingGroundOrGroundedPlayer()) {
            usable = true;
            inDashTimer = 0;
        }
    }

    public override bool overridingMaxSpeed() {
        return inDashTimer > 0;
    }
}