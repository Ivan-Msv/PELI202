using Unity.Netcode;
using UnityEngine;

public class CannonShoot : NetworkBehaviour
{
    [Header("Shootpoint and projectile settings")]
    [SerializeField] private Transform shootPoint;
    [SerializeField] private GameObject projectile;
    [Range(0f, 10f)]
    [SerializeField] private float shootCooldown = 1f;
    private float shootTimer;
    [SerializeField] private GameObject parentObject;
    // Update is called once per frame
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
        AudioManager.instance.PlaySoundRpc(SoundType.Cannon);
        var spawnObject = NetworkObject.InstantiateAndSpawn(projectile, NetworkManager, position: shootPoint.position, rotation: shootPoint.rotation);
        spawnObject.GetComponent<CannonBullet>().parent = transform;
    }
}
