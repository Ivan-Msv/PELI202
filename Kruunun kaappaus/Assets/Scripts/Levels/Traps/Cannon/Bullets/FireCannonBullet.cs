using UnityEngine;

public class FireCannonBullet : CannonBullet
{
    [SerializeField] private Animator bulletAnimator;

    public override void DespawnBullet()
    {
        bulletAnimator.Play("Fire_End");
        SetupProjectile(0, 60, transform);
        // Add sound of fire extinguish mby
        //AudioManager.instance.PlaySoundAtPositionRpc(SoundType.Explosion, NetworkObjectId, false);
    }

    // Despawns the object inside the animation
    private void AnimationDespawn()
    {
        if (!IsServer) { return; }
        NetworkObject.Despawn(true);
    }
}
