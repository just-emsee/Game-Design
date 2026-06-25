using System;
using UnityEngine;

public class GlobalStateManager : MonoBehaviour
{
    [SerializeField]
    private StateManager playerState;

    [Header("State Terrains")]
    [SerializeField]
    private CompositeCollider2D rockCol;
    [SerializeField]
    private CompositeCollider2D paperCol;
    [SerializeField]
    private CompositeCollider2D scissorsCol;

    InputActions input;

    public event Action<State> PlayerStateChanged;

    private void Awake()
    {
        input = new InputActions();
    }

    private void OnEnable()
    {
        input.Enable();
    }

    private void OnDisable()
    {
        input.Disable();
    }

    void Start()
    {
        SetStateToRock();
    }

    void Update()
    {
        if (input.Player.ChangeRock.WasPressedThisFrame())
        {
            Debug.Log("rock");
            SetStateToRock();
        }
        else if (input.Player.ChangePaper.WasPressedThisFrame())
        {
            Debug.Log("paper");
            SetStateToPaper();
        }
        else if (input.Player.ChangeScissors.WasPressedThisFrame())
        {
            Debug.Log("scissors");
            SetStateToScissors();
        }
    }

    private void SetStateToRock()
    {
        playerState.SetState(State.ROCK);
        PlayerStateChanged?.Invoke(State.ROCK);

        rockCol.isTrigger = false;
        paperCol.isTrigger = false;
        scissorsCol.isTrigger = true;
    }

    private void SetStateToPaper()
    {
        playerState.SetState(State.PAPER);
        PlayerStateChanged?.Invoke(State.PAPER);

        rockCol.isTrigger = true;
        paperCol.isTrigger = false;
        scissorsCol.isTrigger = false;
    }

    private void SetStateToScissors()
    {
        playerState.SetState(State.SCISSORS);
        PlayerStateChanged?.Invoke(State.SCISSORS);

        rockCol.isTrigger = false;
        paperCol.isTrigger = true;
        scissorsCol.isTrigger = false;
    }
}
