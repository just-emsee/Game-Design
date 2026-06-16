using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerControl : MonoBehaviour
{
    enum SpeedChangeMode {
        Addition,
        Multiplication
    }

    [Header("Player Sub Objects")]
    [SerializeField] GameObject secondarySphere;

    [Header("Settings")]
    [SerializeField] float rotationSpeed = 1;
    [SerializeField] SpeedChangeMode speedChangeMode = SpeedChangeMode.Addition;
    [SerializeField] float speedChangeValue = .05f;

    CustomInput input = null;
    float startingSpeed = 0;

    void Awake()
    {
        input = new CustomInput();
        startingSpeed = rotationSpeed;
    }

    void OnEnable() {
        input.Enable();
    }

    private void OnDisable() {
        input.Disable();
    }

    void Update()
    {
        if(input.Player.SubmitMove.WasPressedThisFrame()) {
            SubmitMove();
        }
        if (input.Player.Restart.WasPressedThisFrame()) {
            Scene scene = SceneManager.GetActiveScene(); 
            SceneManager.LoadScene(scene.name);
        }
    }

    private void FixedUpdate() {
        PreformRotation();
    }

    void PreformRotation() {
        gameObject.transform.Rotate(new Vector3(0, 0, rotationSpeed), Space.Self);
    }

    void SubmitMove() {
        if (IsMoveOnCorrectSpace(secondarySphere.transform.position)) {
            gameObject.transform.localPosition = secondarySphere.transform.position;
            gameObject.transform.Rotate(new Vector3(0, 0, 180), Space.Self);
            float distanceToCenter = LevelManager.instance.PlayerMoved();
            ScoreManager.instance.SubmitMove(distanceToCenter, Mathf.Abs(rotationSpeed) / startingSpeed, false);
            ApplySpeedChange();
            rotationSpeed *= -1;
        } else {
            Failure();
        }
    }

    void ApplySpeedChange() {
        switch (speedChangeMode) {
            case SpeedChangeMode.Addition: {
                if (rotationSpeed >= 0)
                    rotationSpeed += speedChangeValue;
                else 
                    rotationSpeed -= speedChangeValue;
                break;
            }

            case SpeedChangeMode.Multiplication: {
                rotationSpeed *= speedChangeValue;
                break;
            }
        };
    }

    private bool IsMoveOnCorrectSpace(Vector3 position) {
        return LevelManager.instance.IsValidMove(position);
    }

    void Failure() {
        rotationSpeed = 0;
    }
}
