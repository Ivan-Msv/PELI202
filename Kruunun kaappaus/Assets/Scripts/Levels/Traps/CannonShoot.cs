using Unity.Netcode;
using UnityEngine;

public class CannonShoot : NetworkBehaviour
{
    [SerializeField] private Animator anim;

    [Header("Shootpoint and projectile settings")]
    [SerializeField] private Transform shootPoint;
    [SerializeField] private GameObject projectile;
    [SerializeField] private float shootCooldownSeconds = 1f;
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
        AudioManager.instance.PlaySoundAtPosRpc(SoundType.Cannon,transform.position);
        var spawnObject = NetworkObject.InstantiateAndSpawn(projectile, NetworkManager, position: shootPoint.position, rotation: shootPoint.rotation);
        spawnObject.GetComponent<CannonBullet>().parent = transform;
    }
}
