using System.Collections;
using UnityEngine;

public class Crown : MonoBehaviour
{
    [SerializeField] private float animDelaySeconds;
    [Header("Movement")]
    [SerializeField] private float height;
    [SerializeField] private float frequency;
    private Vector2 startPosition;
    private Animator anim;
    private void Start()
    {
        startPosition = transform.position;
        anim = GetComponent<Animator>();
        InvokeRepeating(nameof(PlayAnimation), 0, animDelaySeconds);
    }

    private void Update()
    {
        Movement();
    }

    private void PlayAnimation()
    {
        anim.Play("Crown_Idle");
    }

    // sine wave pattern
    private void Movement()
    {
        float offset = height * Mathf.Sin(2 * Mathf.PI * frequency * Time.time);
        Debug.Log(offset);
        transform.position = startPosition + Vector2.up * offset;
    }
}
