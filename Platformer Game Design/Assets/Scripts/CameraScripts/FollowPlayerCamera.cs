using UnityEngine;

public class FollowPlayerCamera : MonoBehaviour
{
    [SerializeField]
    private Transform playerTransform;

    private void FixedUpdate()
    {
        transform.position = new Vector3(playerTransform.position.x, playerTransform.position.y, transform.position.z);
    }
}
