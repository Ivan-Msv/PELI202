using Unity.Netcode;
using UnityEngine;

public class CannonShoot : NetworkBehaviour
{
    
    [SerializeField] private Animator anim;

    [Header("Cannon Settings")]
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float shootCooldownSeconds = 1f;
    private float shootTimer;

    [Header("Projectile Settings")]
    [SerializeField] private GameObject projectile;
    [SerializeField] private ParticleSystem cloudParticle;
    [SerializeField] private float bulletSpeed;
    [SerializeField] private float bulletLifeTime;


    
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
            shootTimer = shootCooldownSeconds;
        }
        else
        {
            shootTimer -= Time.deltaTime;
        }
    }

    private void Shoot()
    {
        anim.Play("Cannon_Shoot");
    }
   
    public void ShootEvent()
    {
        // This spawns local projectile, otherwise clients might have bullets despawning too early
        cloudParticle.Play();

        if (!IsServer) { return; }

        AudioManager.instance.PlaySoundAtPositionRpc(SoundType.Cannon, NetworkObjectId, true);
        var spawnObject = NetworkObject.InstantiateAndSpawn(projectile, NetworkManager, position: shootPoint.position, rotation: shootPoint.rotation);
        spawnObject.GetComponent<CannonBullet>().SetupProjectile(bulletSpeed, bulletLifeTime, transform);
    }
}
