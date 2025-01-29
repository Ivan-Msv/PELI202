using Unity.Netcode;
using UnityEngine;

public class PushPr : NetworkBehaviour
{
    [Header("Speed and Lifetime")]
    [Range(1,10)]
    [SerializeField] private float speed = 1.0f;

    [Range(1,10)]
    [SerializeField] private float lifetime = 1.0f;
    [Range(100, 1000)]
    [SerializeField] private float pushDistance;
    private Rigidbody2D targetrb;
    private Rigidbody2D rb;
    
    void Start()
    {
        rb  = GetComponent<Rigidbody2D>();
        Destroy(gameObject,lifetime);
    }
    private void FixedUpdate()
    {
        if (!IsServer) 
        {
            return;
        }
        rb.linearVelocity = transform.right * speed;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) 
        {
            
            /*targetrb = collision.GetComponent<Rigidbody2D>();
            targetrb.AddForce(transform.right * pushDistance);*/
            collision.attachedRigidbody.linearVelocity = new Vector2(pushDistance, 0) ;
        }
        Destroy(gameObject);
    }
 

}
