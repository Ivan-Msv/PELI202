using System.Collections;
using Unity.Netcode;
using UnityEngine;

public abstract class PressurePlate : NetworkBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private bool onlyAffectPlayer;

    [Header("Automation")]
    [SerializeField] private bool autoReset;
    [SerializeField] private float timeToResetSeconds;
    public abstract void PressurePlateEvent();

    [Rpc(SendTo.Server)]
    public virtual void PressurePlateCallbackRpc()
    {
        anim.Play("PressurePlate_Activate");

        if (autoReset)
        {
            StartCoroutine(ResetPlate());
        }

        PressurePlateEvent();
    }

    private IEnumerator ResetPlate()
    {
        yield return new WaitForSeconds(timeToResetSeconds);
        anim.Play("PressurePlate_Deactivate");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) { return; }

        collision.TryGetComponent(out PlayerMovement2D playerMovement);

        if (onlyAffectPlayer && playerMovement == null) { return; }

        if (playerMovement.isGhost) { return; }

        PressurePlateCallbackRpc();
    }
}
