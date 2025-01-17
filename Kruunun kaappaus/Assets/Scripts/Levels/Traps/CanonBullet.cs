using UnityEngine;

public class CanonBullet : MonoBehaviour
{
    [Header("Speed and Lifetime")]
    [Range(0, 100)]
    [SerializeField] private float speed = 1.0f;

    [Range(0, 100)]
    [SerializeField] private float lifetime = 1.0f;

    [SerializeField] private Transform movePoint;

    private Vector2 moveTowards;

    void Start()
    {
        
        Destroy(gameObject, lifetime);
        moveTowards = movePoint.position;
    }
    private void FixedUpdate()
    {
        moveTowards = movePoint.position;
        transform.position = Vector2.MoveTowards(transform.position, moveTowards, speed * Time.deltaTime);
           
        
    }
}
