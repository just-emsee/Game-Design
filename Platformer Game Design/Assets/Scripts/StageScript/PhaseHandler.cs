using UnityEngine;
using UnityEngine.Tilemaps;

public class PhaseHandler : MonoBehaviour
{
    [Header("Values")]
    [SerializeField]
    private float phaseTime = 1.0f;
    [SerializeField]
    private float phaseCooldown = 1.0f;

    [Header("References")]
    [SerializeField]
    private CompositeCollider2D stageCollider;
    [SerializeField]
    private Tilemap stageMap;
    [SerializeField]
    private PlayerLife playerLife;

    private float phaseTimeLeft;
    private float phaseCooldownLeft;

    private bool phaseOnCooldown = false;

    private InputActions input = null;

    private void Awake() {
        input = new InputActions();
    }

    void OnEnable() {
        input.Enable();
    }

    private void OnDisable() {
        input.Disable();
    }

    private void Update() {
        if (input.Player.Phase.WasPressedThisFrame()) {
            TryPhase();
        }
    }

    private void TryPhase()
    {
        if (phaseCooldownLeft <= 0)
        {
            StartPhase();
        }
    }

    void FixedUpdate()
    {
        if (phaseOnCooldown)
        {
            if (phaseTimeLeft > 0)
            {
                phaseTimeLeft -= Time.deltaTime;
                phaseCooldownLeft -= Time.deltaTime;
                return;
            }
            else
            {
                phaseTimeLeft = 0;
                EndPhase();
            }
            if (phaseCooldownLeft > 0)
            {
                phaseCooldownLeft -= Time.deltaTime;
            }
            else
            {
                phaseCooldownLeft = 0;
                phaseOnCooldown = false;
            }
        }
    }

    private void StartPhase()
    {
        stageCollider.isTrigger = true;
        stageMap.color = Color.gray;
        phaseCooldownLeft = phaseTime + phaseCooldown;
        phaseTimeLeft = phaseTime;
        phaseOnCooldown = true;
    }

    private void EndPhase()
    {
        playerLife.CheckShouldDie();
        stageCollider.isTrigger = false;
        stageMap.color = Color.white;
    }
}
