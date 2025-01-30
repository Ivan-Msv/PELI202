using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Animator anim;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var componentExists = collision.TryGetComponent<PlayerMovement2D>(out var playerMovement);

        if (!componentExists)
        {
            return;
        }

        if (playerMovement.isGhost)
        {
            return;
        }

        if (playerMovement.spawnPoint == (Vector2)transform.position)
        {
            return;
        }

        anim.Play("Checkpoint_Activate");
        playerMovement.UpdatePlayerSpawnClientRpc(transform.position, false);
    }
}
