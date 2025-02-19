using UnityEngine;

public class CannonBulletExplosion : MonoBehaviour
{
    [SerializeField] private ParticleSystem explosionParticles;
    [SerializeField] private Animator anim;

    private void Start()
    {
        Destroy(gameObject, anim.GetCurrentAnimatorStateInfo(0).length);
    }

    // Being used in BulletShell_Explosion animation
    private void StartParticles()
    {
        Instantiate(explosionParticles, transform.position, explosionParticles.transform.rotation);
    }
}
