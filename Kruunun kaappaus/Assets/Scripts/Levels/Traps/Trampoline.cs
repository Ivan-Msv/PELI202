using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class Trampoline : NetworkBehaviour
{
    [Range(-1, 1)]
    [SerializeField] private int xDirection, yDirection;
    [SerializeField] private float xBounce;
    [SerializeField] private float yBounce;
    private Animator anim;
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<PlayerMovement2D>().isGhost)
        {
            return;
        }

        collision.GetComponent<PlayerMovement2D>().AddExternalForce(new(xDirection * xBounce, yDirection * yBounce));
        PlayAnimationServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void PlayAnimationServerRpc()
    {
        anim.Play("Trampoline_Used");
    }

    private void OnDrawGizmos()
    {
        // I dont know why 10 works, but this is the closest I got to visualizing
        // the peak of the height
        Gizmos.color = Color.green;
        float xHeightPosition = xDirection * xBounce / 10;
        float yHeightPosition = (yDirection * yBounce) / 10;

        Vector3 jumpHeightPosition = transform.position + new Vector3(xHeightPosition, yHeightPosition, 0);
        Gizmos.DrawLine(transform.position, jumpHeightPosition);
        Gizmos.DrawSphere(jumpHeightPosition, 0.1f);
    }
}
