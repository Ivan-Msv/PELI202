using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class DefaultCannonBullet : CannonBullet
{
    [SerializeField] private GameObject explosionObject;
    public override void DespawnBullet()
    {
        InstantiateExplosionRpc();

        // If the object gets destroyed before properly spawning, play local rpc
        switch (IsSpawned)
        {
            case true:
                AudioManager.instance.PlaySoundAtObjectPositionRpc(SoundType.Explosion, NetworkObjectId, false);
                break;
            case false:
                AudioManager.instance.PlaySoundAtPositionRpc(SoundType.Explosion, transform.position);
                break;
        }

        base.DespawnBullet();
    }

    [Rpc(SendTo.Everyone)]
    private void InstantiateExplosionRpc()
    {
        Instantiate(explosionObject, transform.position, transform.rotation);
    }
}
