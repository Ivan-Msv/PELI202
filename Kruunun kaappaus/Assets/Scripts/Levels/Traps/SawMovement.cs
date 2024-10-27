using UnityEngine;

public class SawMovement : MonoBehaviour
{
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float slowDistance;
    [SerializeField] private float pauseTimeSeconds;
    private Vector3 currentPoint;
    private bool paused;
    private float timer;

    private void Start()
    {
        currentPoint = endPoint.position;
        transform.position = startPoint.position;
    }

    void Update()
    {
        Movement();
        OnMovementPaused();
    }

    private void Movement()
    {
        if (paused)
        {
            return;
        }


        float distance = Vector2.Distance(transform.position, currentPoint);
        bool closeToPoints = Mathf.Min(Vector2.Distance(transform.position, startPoint.position), Vector2.Distance(transform.position, endPoint.position)) < slowDistance;
        transform.position = Vector2.MoveTowards(transform.position, currentPoint, (closeToPoints ? moveSpeed / 2 : moveSpeed) * Time.deltaTime);
        if (distance < 0.1f)
        {
            paused = true;
            ChangeCurrentPoint();
        }
    }
    private void OnMovementPaused()
    {
        if (!paused)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= pauseTimeSeconds)
        {
            paused = false;
            timer = 0;
        }
    }

    private void ChangeCurrentPoint()
    {
        float distance = Vector2.Distance(transform.position, endPoint.position);
        currentPoint = distance < 0.1f ? startPoint.position : endPoint.position;
    }
}
