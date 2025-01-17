using Unity.Netcode;
using UnityEngine;

public class CanonShoot : MonoBehaviour
{
    [Header("Shootpoint and projectile settings")]
    [SerializeField] private Transform shootPoint;
    [SerializeField] private GameObject projectile;
    [Range(0f, 10f)]
    [SerializeField] private float shootCooldown = 1f;
    private float shootTimer ;
    [SerializeField] private GameObject parentObject;
    // Update is called once per frame
    void Update()
    {
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
     var instance = Instantiate(projectile, shootPoint.position, shootPoint.rotation,parentObject.transform);
        var instanceNetworkObject = instance.GetComponent<NetworkObject>();
        instanceNetworkObject.Spawn();
    }
}
