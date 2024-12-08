﻿using Unity.Netcode;
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

        collision.attachedRigidbody.linearVelocity = new Vector2(xDirection * xBounce, yDirection * yBounce);
        PlayAnimationServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayAnimationServerRpc()
    {
        anim.Play("Trampoline_Used");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        float xHeightPosition = xDirection * xBounce / 5 * 0.2f * 2; // 5 on gravity scale, 0.2f on mun xDrag pelaajan skriptissä.
        float yHeightPosition = yDirection * (0.5f * yBounce - 6); // Voi olla täysin väärä, sillä testasin sitä pelissä ja yritin verrata siihen.
        Vector3 jumpHeightPosition = transform.position + new Vector3(xHeightPosition, yHeightPosition, 0);
        Gizmos.DrawLine(transform.position, jumpHeightPosition);
        Gizmos.DrawSphere(jumpHeightPosition, 0.1f);
    }
}
