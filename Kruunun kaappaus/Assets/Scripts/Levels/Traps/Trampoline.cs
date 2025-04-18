﻿using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class Trampoline : NetworkBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private ParticleSystem bounceParticle;
    [Header("Editor")]
    [SerializeField] private bool EnableGizmoText;
    [Header("Settings")]
    [SerializeField] private float bounceAmount;
    [SerializeField] private float velocityCap;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        var playerMovement = collision.collider.GetComponent<PlayerMovement2D>();
        if (playerMovement.isGhost)
        {
            return;
        }

        var newSpeed = collision.relativeVelocity.magnitude / Mathf.Sqrt(2);
        var newVelocity = transform.up * Mathf.Clamp(Mathf.Round(newSpeed + bounceAmount), -velocityCap, velocityCap);

        collision.rigidbody.linearVelocityY += newVelocity.y;
        // No reason to multiply by two but it feels way better that way...
        playerMovement.AddExternalForceRpc(new(newVelocity.x * 2, 0));
        PlayAnimationServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void PlayAnimationServerRpc()
    {
        anim.Play("Trampoline_Used", 0, 0);
    }

    // Used in animation "Trampoline_Used"
    private void PlayParticle()
    {
        bounceParticle.Play();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        // Both divided by 3 because of the player default gravity
        // But for some reason it's way shorter than it should be
        Vector2 capPosition = transform.position + transform.up * (velocityCap / 3);
        Vector2 startingVelocityPosition = transform.position + transform.up * (bounceAmount / 3);

        Gizmos.DrawLine(transform.position, capPosition);
        Gizmos.DrawSphere(capPosition, 0.05f);
        Gizmos.DrawSphere(startingVelocityPosition, 0.05f);

        if (EnableGizmoText)
        {
            Handles.Label(capPosition, "Velocity Cap \n{INACCURATE}");
            Handles.Label(startingVelocityPosition, "Minimum Bounce");
        }
    }
#endif
}
