using UnityEngine;

public class TimedAnimation : MonoBehaviour
{
    [SerializeField] private Animator anim;
    private void Start()
    {
        anim = GetComponent<Animator>();
        Destroy(gameObject, anim.GetCurrentAnimatorStateInfo(0).length);
    }
}
