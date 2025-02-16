using Unity.Netcode;
using UnityEngine;

public class CannonShoot : NetworkBehaviour
{
    
    [SerializeField] private Animator anim;

    [Header("Shootpoint and projectile settings")]
    [SerializeField] private Transform shootPoint;
    [SerializeField] private GameObject projectile;
    [SerializeField] private float shootCooldownSeconds = 1f;
    [SerializeField] private ParticleSystem cloudParticle;
    [SerializeField] public float bulletSpeed;
    private float shootTimer;


    
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
        spawnObject.GetComponent<CannonBullet>().parent = transform;
        spawnObject.GetComponent<CannonBullet>().speed = bulletSpeed;
    }
}
