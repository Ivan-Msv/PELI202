using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class DefaultCannonBullet : CannonBullet
{
    [SerializeField] private GameObject explosionObject;
    public override void DespawnBullet()
    {
        InstantiateExplosionRpc();
        AudioManager.instance.PlaySoundAtPositionRpc(SoundType.Explosion, NetworkObjectId, false);
        base.DespawnBullet();
    }

    [Rpc(SendTo.Everyone)]
    private void InstantiateExplosionRpc()
    {
        Instantiate(explosionObject, transform.position, transform.rotation);
    }
}
