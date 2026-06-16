using TMPro;
using UnityEngine;

public class ScoreText : MonoBehaviour {
    [SerializeField] float upwardsSpeed;
    [SerializeField] float alphaChange;

    private void FixedUpdate() {
        transform.position = transform.position + new Vector3(0, upwardsSpeed, 0);
        GetComponent<TextMeshProUGUI>().alpha -= alphaChange;
        if (GetComponent<TextMeshProUGUI>().alpha < 0) {
            Destroy(gameObject);
        }
    }

}
