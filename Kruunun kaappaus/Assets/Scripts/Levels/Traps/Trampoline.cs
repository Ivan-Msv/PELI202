using UnityEngine;
using UnityEngine.Serialization;

public class Trampoline : MonoBehaviour
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
        collision.attachedRigidbody.linearVelocity = new Vector2(xDirection * xBounce, yDirection * yBounce);
        anim.Play("Trampoline_Used");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        float xHeightPosition = xDirection * xBounce / 5 * 0.2f; // 5 on gravity scale, 0.2f on mun xDrag pelaajan skriptissä.
        float yHeightPosition = yDirection * (0.5f * yBounce - 6); // Voi olla täysin väärä, sillä testasin sitä pelissä ja yritin verrata siihen.
        Vector3 jumpHeightPosition = transform.position + new Vector3(xHeightPosition, yHeightPosition, 0);
        Gizmos.DrawLine(transform.position, jumpHeightPosition);
        Gizmos.DrawSphere(jumpHeightPosition, 0.1f);
    }
}
