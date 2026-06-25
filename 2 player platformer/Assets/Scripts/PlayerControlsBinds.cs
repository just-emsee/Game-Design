using UnityEngine;

public class PlayerControlsBinds : MonoBehaviour
{
    [SerializeField] Player playerOne;
    [SerializeField] Player playerTwo;


    CustomInput input = null;

    private void Awake() {
        input = new CustomInput();
    }

    void OnEnable() {
        input.Enable();
    }

    private void OnDisable() {
        input.Disable();
    }

    void Update()
    {
        playerOne.InputAxis(input.PlayerOne.Move.ReadValue<float>());
        playerTwo.InputAxis(input.PlayerTwo.Move.ReadValue<float>());
        if (input.PlayerOne.Jump.WasPerformedThisFrame()) playerOne.InputDoubleJump();
        if (input.PlayerTwo.Jump.WasPerformedThisFrame()) playerTwo.InputDoubleJump();
        if (input.PlayerOne.Jump.IsPressed()) playerOne.InputJump();
        if (input.PlayerTwo.Jump.IsPressed()) playerTwo.InputJump();
        if (input.PlayerOne.Ability.WasPerformedThisFrame()) playerOne.AbilityPressed();
        if (input.PlayerTwo.Jump.WasPerformedThisFrame()) playerTwo.AbilityPressed();
    }
}
