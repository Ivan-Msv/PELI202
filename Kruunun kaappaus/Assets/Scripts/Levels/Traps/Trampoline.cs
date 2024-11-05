using UnityEngine;

public class Trampoline : MonoBehaviour
{
    [Range(-1, 1)]
    [SerializeField] private int xDirection, yDirection;
    [SerializeField] private float jumpHeight;
    private Animator anim;
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        collision.attachedRigidbody.linearVelocity = new Vector2(xDirection * jumpHeight, yDirection * jumpHeight);
        anim.Play("Trampoline_Used");
    }
}
