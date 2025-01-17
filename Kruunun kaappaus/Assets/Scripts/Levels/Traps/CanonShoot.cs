using Unity.Netcode;
using UnityEngine;

public class CanonShoot : NetworkBehaviour
{
    [Header("Shootpoint and projectile settings")]
    [SerializeField] private Transform shootPoint;
    [SerializeField] private GameObject projectile;
    [Range(0f, 10f)]
    [SerializeField] private float shootCooldown = 1f;
    private float shootTimer;

    void Update()
    {
        if (!IsServer)
        {
            return;
        }

        if (shootTimer <= 0)
        {
            Shoot();
            shootTimer = shootCooldown;
        }
        else
        {
            shootTimer -= Time.deltaTime;
        }
    }
    private void Shoot()
    {
        NetworkObject.InstantiateAndSpawn(projectile, NetworkManager, position: shootPoint.position, rotation: shootPoint.rotation);
    }
}
