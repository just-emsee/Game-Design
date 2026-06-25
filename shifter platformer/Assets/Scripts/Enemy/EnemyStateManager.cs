using UnityEngine;

public class EnemyStateManager : MonoBehaviour
{
    private SpriteRenderer sprite;

    [SerializeField]
    private State enemyState;

    [Header("State Colors")]
    [SerializeField]
    private Color rockColor;
    [SerializeField]
    private Color paperColor;
    [SerializeField]
    private Color scissorColor;

    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        ColorSelector();
    }

    void Update()
    {
        
    }

    private void ColorSelector()
    {
        switch (enemyState)
        {
            case State.ROCK:
                sprite.color = rockColor;
                break;
            case State.PAPER:
                sprite.color = paperColor;
                break;
            case State.SCISSORS:
                sprite.color = scissorColor;
                break;
        }
    }

    public State GetEnemyState()
    {
        return enemyState;
    }

    public void EnemyDeath()
    {
        Destroy(gameObject);
    }
}
