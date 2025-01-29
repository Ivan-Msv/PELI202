using Unity.Netcode;
using UnityEngine;

public class CannonBullet : NetworkBehaviour
{
    [Header("Speed and Lifetime")]
    [Range(0, 100)]
    [SerializeField] private float speed = 1.0f;

    [Range(0, 100)]
    [SerializeField] private float lifetime = 1.0f;
    [SerializeField] private Transform movePoint;
    public Transform parent;

    void Start()
    {
        if (!IsServer)
        {
            return;
        }

        Invoke(nameof(DespawnBullet), lifetime);
    }
    private void Update()
    {
        if (!IsServer)
        {
            return;
        }

        transform.position = Vector2.MoveTowards(transform.position, movePoint.position, speed * Time.deltaTime);
    }

    private void DespawnBullet()
    {
        if (!IsServer)
        {
            return;
        }
        AudioManager.instance.PlaySoundRpc(SoundType.Explosion);
        NetworkObject.Despawn(gameObject);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer)
        {
            return;
        }

        if (collision.CompareTag("Player") || collision.transform == parent)
        {
            return;
        }

        DespawnBullet();
    }
}
