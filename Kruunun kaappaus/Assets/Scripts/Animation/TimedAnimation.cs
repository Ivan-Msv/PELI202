using UnityEngine;

public class TimedAnimation : MonoBehaviour
{
    [SerializeField] private Animator anim;
    private void Start()
    {
        Destroy(gameObject, anim.GetCurrentAnimatorClipInfo(0).Length);
    }
}
