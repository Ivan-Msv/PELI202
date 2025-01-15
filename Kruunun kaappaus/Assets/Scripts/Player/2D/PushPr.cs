using UnityEngine;

public class PushPr : MonoBehaviour
{
    [Header("Speed and Lifetime")]
    [Range(1,10)]
    [SerializeField] private float speed = 1.0f;

    [Range(1,10)]
    [SerializeField] private float lifetime = 1.0f;

    private Rigidbody2D rb;
    void Start()
    {
        rb  = GetComponent<Rigidbody2D>();
        Destroy(gameObject,lifetime);
    }
    private void FixedUpdate()
    {
        rb.linearVelocity = transform.right * speed;
    }

}
