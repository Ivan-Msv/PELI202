using UnityEngine;

public class Trampoline : MonoBehaviour
{
    [SerializeField] private float jumpHeight;
    private Animator anim;
    void Start()
    {
        anim = GetComponent<Animator>();
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        collision.attachedRigidbody.linearVelocityY = jumpHeight;
        anim.Play("Trampoline_Used");
    }
}
