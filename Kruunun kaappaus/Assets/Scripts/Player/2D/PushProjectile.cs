using Unity.Netcode;
using UnityEngine;

public class PushProjectile : NetworkBehaviour
{
    [Header("Speed and Lifetime")]
    [Range(1,10)]
    [SerializeField] private float speed = 1.0f;

    [Range(1,10)]
    [SerializeField] private float lifetime = 1.0f;
    [SerializeField] private float pushDistance;
    private Rigidbody2D rb;
    public Animator animator;
    void Start()
    {
        if (!IsServer)
        {
            return;
        }

        rb = GetComponent<Rigidbody2D>();
        Invoke(nameof(DespawnProjectile), lifetime);
    }

    private void DespawnProjectile()
    {
        animator.Play("Fire_End");
        NetworkObject.Despawn(gameObject);
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
        if (!IsServer)
        {
            return;
        }

        if (collision.CompareTag("Player")) 
        {
            collision.attachedRigidbody.linearVelocity = transform.right * pushDistance;
        }

        DespawnProjectile();
    }
 

}
