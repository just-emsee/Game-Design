using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerLife : MonoBehaviour
{
    [SerializeField]
    private StateManager playerState;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("DeathZone"))
        {
            Die();
        }

        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyStateManager enemyStateManager = collision.gameObject.GetComponent<EnemyStateManager>();
            State enemyState = enemyStateManager.GetEnemyState();
            State state = playerState.GetState();

            switch (state)
            {
                case State.ROCK:
                    CheckRockMatchups(enemyState, enemyStateManager);
                    break;
                case State.PAPER:
                    CheckPaperMatchups(enemyState, enemyStateManager);
                    break;
                case State.SCISSORS:
                    CheckScissorsMatchups(enemyState, enemyStateManager);
                    break;
            }
        }
    }

    private void CheckRockMatchups(State enemyState, EnemyStateManager enemyStateManager)
    {
        switch (enemyState)
        {
            case State.ROCK:
                break;
            case State.PAPER:
                Die();
                break;
            case State.SCISSORS:
                Debug.Log("Enemy dies");
                enemyStateManager.EnemyDeath();
                break;
        }
    }

    private void CheckPaperMatchups(State enemyState, EnemyStateManager enemyStateManager)
    {
        switch (enemyState)
        {
            case State.ROCK:
                Debug.Log("Enemy dies");
                enemyStateManager.EnemyDeath();
                break;
            case State.PAPER:
                break;
            case State.SCISSORS:
                Die();
                break;
        }
    }

    private void CheckScissorsMatchups(State enemyState, EnemyStateManager enemyStateManager)
    {
        switch (enemyState)
        {
            case State.ROCK:
                Die();
                break;
            case State.PAPER:
                Debug.Log("Enemy dies");
                enemyStateManager.EnemyDeath();
                break;
            case State.SCISSORS:
                break;
        }
    }

    private void Die()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
