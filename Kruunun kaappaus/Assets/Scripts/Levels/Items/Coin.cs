using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Coin : NetworkBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private Collider2D coinCollider;
    [SerializeField] private float animDelaySeconds;
    [Header("Movement")]
    [SerializeField] private float maxHeight;
    [SerializeField] private float maxFrequency;
    private Vector2 startPosition;
    private void Start()
    {
        startPosition = transform.position;
        maxFrequency -= Random.Range(0, 0.15f);
        InvokeRepeating(nameof(PlayAnimation), Random.Range(0f, 0.90f), animDelaySeconds);
    }

    // Update is called once per frame
    void Update()
    {
        Movement();
    }

    [Rpc(SendTo.Everyone)]
    public void CollectCoinRpc()
    {
        StartCoroutine(CoinDestroyCoroutine());
    }

    private IEnumerator CoinDestroyCoroutine()
    {
        AudioManager.instance.PlaySoundRpc(SoundType.CoinPickUp);
        coinCollider.enabled = false;
        anim.Play("Coin_Destroy");

        if (!IsServer)
        {
            yield break;
        }

        yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length);
        GetComponent<NetworkObject>().Despawn(true);
    }

    private void PlayAnimation()
    {
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Coin_Destroy"))
        {
            return;
        }

        anim.Play("Coin_Idle");
    }

    private void Movement()
    {
        float offset = maxHeight * Mathf.Sin(2 * Mathf.PI * maxFrequency * Time.time);
        transform.position = startPosition + Vector2.up * offset;
    }
}
