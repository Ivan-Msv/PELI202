using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;
using UnityEngine;
using UnityEngine.UI;

public class MovingBackground : MonoBehaviour
{
    [Range(-1, 1)]
    [SerializeField] private int xDirection, yDirection;
    [SerializeField] private float moveSpeed = 1;
    private float offset;
    private Vector2 startPos;

    private void Awake()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        offset = xDirection + yDirection == 2 ? 1.4f : 1;
        transform.position += new Vector3(xDirection, yDirection, transform.position.z) * moveSpeed * Time.deltaTime;

        if (Vector2.Distance(startPos, transform.position) > offset)
        {
            transform.position = startPos;
        }
    }
}
