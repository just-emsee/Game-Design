using UnityEngine;

public class CameraScript : MonoBehaviour
{
    [SerializeField] GameObject playerOne = null;
    [SerializeField] GameObject playerTwo = null;

    [SerializeField] float cameraMinY = 6.5f;
    [SerializeField] float lerpValue = .2f;

    void Update() {
        Vector2 p1 = playerOne.transform.position;
        Vector2 p2 = playerTwo.transform.position;

        Vector2 midPoint = (p1 + p2) / 2;
        midPoint.y = Mathf.Max(midPoint.y, cameraMinY);
        transform.position = Vector3.Lerp(transform.position, new Vector3(midPoint.x, midPoint.y, -10), lerpValue);
    }
}
