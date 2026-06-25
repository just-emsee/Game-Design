using UnityEngine;


//maybe events for the state change
public class StateManager : MonoBehaviour
{
    private SpriteRenderer sprite;

    [SerializeField]
    private State playerState;

    [Header("State Colors")]
    [SerializeField]
    private Color rockColor;
    [SerializeField]
    private Color paperColor;
    [SerializeField]
    private Color scissorColor;

    [Header("State Terrains")]
    [SerializeField]
    private CompositeCollider2D rockTerrain;
    [SerializeField]
    private CompositeCollider2D paperTerrain;
    [SerializeField]
    private CompositeCollider2D scissorsTerrain;

    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        ColorSelector();
    }

    private void ColorSelector()
    {
        switch (playerState)
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

    public void SetState(State newState)
    {
        playerState = newState;
    }

    public State GetState()
    {
        return playerState;
    }
}
