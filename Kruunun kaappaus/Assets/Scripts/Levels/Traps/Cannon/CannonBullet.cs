using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class CannonBullet : NetworkBehaviour
{
    [SerializeField] private Vector2 moveDirection;
    [SerializeField] private Rigidbody2D rb;
    private Transform parent;
    private float speed;
    private float lifeTime;

    void Start()
    {
        if (!IsServer)
        {
            return;
        }

        //netTransform.SetMaxInterpolationBound
        
        Invoke(nameof(DespawnBullet), lifeTime);
    }

    public void SetupProjectile(float givenSpeed, float givenLifetime, Transform givenParent)
    {
        speed = givenSpeed;
        lifeTime = givenLifetime;
        parent = givenParent;
        moveDirection = givenParent.transform.right;
    }

    private void Update()
    {
        if (IsServer)
        {
            rb.linearVelocity = moveDirection * speed;
            SendLinearVelocityRpc(rb.linearVelocity);
        }
    }

    public virtual void DespawnBullet()
    {
        // I don't think this matters since it only gets invoked
        // from start which already has it but whatever
        if (!IsServer)
        {
            return;
        }

        NetworkObject.Despawn(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer)
        {
            return;
        }

        if (collision.CompareTag("Player") || collision.transform == parent || collision.CompareTag("Ghost projectile"))
        {
            return;
        }

        DespawnBullet();
    }

    [Rpc(SendTo.Everyone)]
    private void SendLinearVelocityRpc(Vector2 newVelocity)
    {
        rb.linearVelocity = newVelocity;
    }
}
