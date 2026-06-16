using Unity.Mathematics;
using UnityEngine;

public class PlayerFollow : MonoBehaviour
{
    [SerializeField] GameObject player;
    [SerializeField] float lerpValue =.5f;
    [SerializeField] float cameraZValue =-10;
    void Update()
    {
        Vector3 targetPos = player.transform.position;
        targetPos.z = cameraZValue;
        transform.position = Vector3.Lerp(transform.position, targetPos, lerpValue);
    }
}
