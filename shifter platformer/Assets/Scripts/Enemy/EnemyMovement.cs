using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    private BoxCollider2D col;
    private Rigidbody2D rb;

    private GlobalStateManager globalStateManager;

    [SerializeField]
    private EnemyStateManager enemyStateManager;
    [SerializeField]
    private LayerMask terrain;

    [Header("Movement Speeds")]
    [SerializeField]
    private float disadvantageSpeed = 5f;
    [SerializeField]
    private float normalSpeed = 8f;
    [SerializeField]
    private float advantageSpeed = 15f;

    private State enemyState;

    private float movementDirMult = 1f;
    private float currentMoveSpeed;

    private void OnEnable()
    {
        col = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();

        globalStateManager = GameObject.Find("GlobalStateManager").GetComponent<GlobalStateManager>();
        globalStateManager.PlayerStateChanged += OnPlayerStateChange;

        enemyState = enemyStateManager.GetEnemyState();
    }

    private void OnDisable()
    {
        globalStateManager.PlayerStateChanged -= OnPlayerStateChange;
    }

    void Start()
    {
        currentMoveSpeed = normalSpeed;
    }

    void Update()
    {
        Movement();
    }

    private void Movement()
    {
        rb.linearVelocityX = currentMoveSpeed * movementDirMult;

        if (movementDirMult > 0f && HitWallRight())
        {
            movementDirMult *= -1;
        }
        else if (movementDirMult < 0f && HitWallLeft())
        {
            movementDirMult *= -1;
        }
    }

    private void OnPlayerStateChange(State newPlayerState)
    {
        switch (newPlayerState)
        {
            case State.ROCK:
                CheckRockMatchups();
                break;
            case State.PAPER:
                CheckPaperMatchups();
                break;
            case State.SCISSORS:
                CheckScissorsMatchups();
                break;
        }
    }

    private void CheckRockMatchups()
    {
        switch (enemyState)
        {
            case State.ROCK:
                currentMoveSpeed = normalSpeed;
                break;
            case State.PAPER:
                currentMoveSpeed = advantageSpeed;
                break;
            case State.SCISSORS:
                currentMoveSpeed = disadvantageSpeed;
                break;
        }
    }

    private void CheckPaperMatchups()
    {
        switch (enemyState)
        {
            case State.ROCK:
                currentMoveSpeed = disadvantageSpeed;
                break;
            case State.PAPER:
                currentMoveSpeed = normalSpeed;
                break;
            case State.SCISSORS:
                currentMoveSpeed = advantageSpeed;
                break;
        }
    }

    private void CheckScissorsMatchups()
    {
        switch (enemyState)
        {
            case State.ROCK:
                currentMoveSpeed = advantageSpeed;
                break;
            case State.PAPER:
                currentMoveSpeed = disadvantageSpeed;
                break;
            case State.SCISSORS:
                currentMoveSpeed = normalSpeed;
                break;
        }
    }

    private bool HitWallRight()
    {
        RaycastHit2D hit = Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0f, Vector2.right, .015f, terrain);
        return hit && !hit.collider.isTrigger;
    }

    private bool HitWallLeft()
    {
        RaycastHit2D hit = Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0f, Vector2.left, .015f, terrain);
        return hit && !hit.collider.isTrigger;
    }
}
