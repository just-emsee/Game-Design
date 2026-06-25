using UnityEngine;

[RequireComponent (typeof(Collider2D))]
public class Killzone : MonoBehaviour
{
    Collider2D col;
    private void Start() {
        col = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other) {
        Player player = other.GetComponent<Player>();
        if (player != null) {
            player.ResetPos();
        }
    }
}
