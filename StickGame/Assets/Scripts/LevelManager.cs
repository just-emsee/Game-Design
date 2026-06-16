using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    [Header("Prefabs")]
    [SerializeField] GameObject targetPrefab;

    [Header("Objects")]
    [SerializeField] GameObject player;
    [SerializeField] GameObject startNode;
    [SerializeField] GameObject nextTarget;

    [Header("Variables")]
    [SerializeField] float maxDistanceFromCenter = .5f;
    [SerializeField] float maxAngleRandomness = 0.1f;
    [SerializeField] float maxAngle = 0.25f;
    [SerializeField] int maxSpawnedNode = 15;

    [Header("Debug")]
    [SerializeField] float currentAngle = 0;
    [SerializeField] Queue<GameObject> destroyQueue = new Queue<GameObject>();

    void Start()
    {
        if (instance != null) throw new System.Exception("Multiple instances of LevelManager created");
        instance = this;
        destroyQueue.Enqueue(startNode);
        destroyQueue.Enqueue(nextTarget);
    }

    private void OnDestroy() {
        instance = null;
    }

    /// <returns>The distance to the center of the point</returns>
    public float PlayerMoved() {
        float dist = (player.transform.position - new Vector3(nextTarget.transform.position.x, nextTarget.transform.position.y,0)).magnitude;
        CreateNextTarget();
        CheckDestroyQueue();
        return dist;
    }

    void CreateNextTarget() {

        float randomness = -maxAngleRandomness + (Random.value * maxAngleRandomness * 2);
        currentAngle += randomness;

        currentAngle = Mathf.Clamp(currentAngle, -maxAngle, maxAngle);

        Quaternion rotation = Quaternion.AngleAxis(currentAngle*360, Vector3.forward);
        Vector3 newPos = rotation * new Vector3(0,4,1) + player.transform.position;
        GameObject spawned = Instantiate(targetPrefab, newPos, Quaternion.identity);
        nextTarget = spawned;
        destroyQueue.Enqueue(spawned);
    }

    void CheckDestroyQueue() {
        if (maxSpawnedNode == 0) return;
        if (destroyQueue.Count > maxSpawnedNode) {
            Destroy(destroyQueue.Dequeue()); // kill node in cold blood
        }
    }




    public bool IsValidMove(Vector3 position) {
        float dist = (position - new Vector3(nextTarget.transform.position.x, nextTarget.transform.position.y, 0)).magnitude;
        return dist <= maxDistanceFromCenter;
    }

    internal float GetMaxDistance() {
        return maxDistanceFromCenter;
    }
}
