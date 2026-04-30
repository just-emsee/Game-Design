using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    [Header("Player Sub Objects")]
    [SerializeField] GameObject secondarySphere;

    [Header("Settings")]
    [SerializeField] float rotationSpeed = 1;

    CustomInput input = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        input = new CustomInput();
    }

    void OnEnable() {
        input.Enable();
    }

    private void OnDisable() {
        input.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        PreformRotation();
        if(input.Player.SubmitMove.WasPressedThisFrame()) {
            SubmitMove();
        }
    }

    void PreformRotation() {
        gameObject.transform.Rotate(new Vector3(0, 0, rotationSpeed), Space.Self);
    }

    void SubmitMove() {
        if (IsMoveOnCorrectSpace(secondarySphere.transform.position)) {
            gameObject.transform.localPosition = secondarySphere.transform.position;
            gameObject.transform.Rotate(new Vector3(0, 0, 180), Space.Self);
            rotationSpeed *= -1;
        }
    }

    private bool IsMoveOnCorrectSpace(Vector3 position) {
        return true;
    }
}
