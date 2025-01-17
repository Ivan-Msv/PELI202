using Unity.Netcode;
using UnityEngine;

public class CanonBullet : NetworkBehaviour
{
    [Header("Speed and Lifetime")]
    [Range(0, 100)]
    [SerializeField] private float speed = 1.0f;

    [Range(0, 100)]
    [SerializeField] private float lifetime = 1.0f;
    [SerializeField] private Transform movePoint;

    void Start()
    {
        Invoke(nameof(DespawnBullet), lifetime);
    }
    private void Update()
    {
        transform.position = Vector2.MoveTowards(transform.position, movePoint.position, speed * Time.deltaTime);
    }

    private void DespawnBullet()
    {
        NetworkObject.Despawn(gameObject);
    }
}
