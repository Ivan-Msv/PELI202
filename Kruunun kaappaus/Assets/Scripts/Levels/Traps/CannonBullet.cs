using Unity.Netcode;
using UnityEngine;

public class CannonBullet : NetworkBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] private float moveDirectionX, moveDirectionY;
    [SerializeField] private GameObject explosionObject;
    private Transform parent;
    private float speed;
    private float lifeTime;

    void Start()
    {
        if (!IsServer)
        {
            return;
        }
        
        Invoke(nameof(DespawnBullet), lifeTime);
    }

    public void SetupProjectile(float givenSpeed, float givenLifetime, Transform givenParent)
    {
        speed = givenSpeed;
        lifeTime = givenLifetime;
        parent = givenParent;
    }

    private void FixedUpdate()
    {
        if (!IsServer)
        {
            return;
        }

        transform.position = Vector2.MoveTowards(transform.position, transform.position + new Vector3(moveDirectionX, moveDirectionY), speed * Time.fixedDeltaTime);
    }

    [Rpc(SendTo.Everyone)]
    private void InstantiateExplosionRpc()
    {
        Instantiate(explosionObject, transform.position, explosionObject.transform.rotation);
    }

    private void DespawnBullet()
    {
        if (!IsServer)
        {
            return;
        }
        AudioManager.instance.PlaySoundAtPositionRpc(SoundType.Explosion, NetworkObjectId, false);
        InstantiateExplosionRpc();
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
}
