using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Portal : NetworkBehaviour
{
    [SerializeField] private Collider2D currentPortal;
    [SerializeField] private Collider2D targetPortal;
    [SerializeField] private float exitOffset;
    [SerializeField] private ParticleSystem portalIdleParticle;
    public ParticleSystem portalInwardTrigger, portalOutwardTrigger;

    private ParticleSystem targetPortalOutwardTrigger;

    private void Awake()
    {
        targetPortalOutwardTrigger = targetPortal.GetComponent<Portal>().portalOutwardTrigger;
        SetParticleColors();
    }

    private void SetParticleColors()
    {
        var currentColor = GetComponent<SpriteRenderer>().color;
        var idleMain = portalIdleParticle.main;
        var inwardMain = portalInwardTrigger.main;
        var outwardMain = portalOutwardTrigger.main;
        inwardMain.startColor = currentColor;
        outwardMain.startColor = currentColor;
        idleMain.startColor = currentColor;
    }

    [Rpc(SendTo.Everyone)]
    private void PlayInwardParticleRpc()
    {
        portalInwardTrigger.Play();
    }
    [Rpc(SendTo.Everyone)]
    private void PlayOutwardParticleRpc()
    {
        targetPortalOutwardTrigger.Play();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        var playerMovement = collision.collider.GetComponent<PlayerMovement2D>();
        if (playerMovement.isGhost)
        {
            return;
        }

        if (!playerMovement.CanUsePortal)
        {
            return;
        }

        // Play particle | sound here
        PlayInwardParticleRpc();

        // So that the gravity doesn't change during this
        playerMovement.IsUsingPortal = true;

        // Reset gravity so it doesn't affect the new velocity too early
        collision.rigidbody.gravityScale = 0;

        var lastVelocity = collision.relativeVelocity;

        // This was the hardest part to find info on, since the player gravity changes based on the velocity
        var newSpeed = lastVelocity.magnitude / Mathf.Sqrt(2);

        // Give minimum speed so you don't get "stuck" in portal
        if (newSpeed < 3)
        {
            newSpeed = 3;
        }

        // Round the speed magnitude so it doesn't sway around too much and decrease/increase velocity
        var newVelocity = targetPortal.transform.up * Mathf.Round(newSpeed);


        // Teleport the player and the camera
        var newPosition = targetPortal.transform.up * exitOffset + targetPortal.transform.position;
        var colliderCenter = collision.transform.position - collision.collider.bounds.center;

        collision.transform.GetComponent<NetworkTransform>().Teleport(newPosition + colliderCenter, collision.transform.rotation, collision.transform.localScale);
        LevelManager.instance.TeleportCamera();

        // Setting horizontal as external force due to playermovement script resetting it otherwise
        collision.rigidbody.linearVelocityY = newVelocity.y;
        playerMovement.AddExternalForce(new(newVelocity.x, 0));
        playerMovement.SetPortalCooldown();
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        var playerMovement = collision.collider.GetComponent<PlayerMovement2D>();
        if (playerMovement.isGhost)
        {
            return;
        }

        if (!playerMovement.IsUsingPortal)
        {
            return;
        }

        // Play particle | sound here
        PlayOutwardParticleRpc();

        // Reset everything, and put the portal on cooldown (otherwise it would spam teleport)

        playerMovement.IsUsingPortal = false;
        collision.rigidbody.gravityScale = playerMovement.DefaultGravity;
    }
}
