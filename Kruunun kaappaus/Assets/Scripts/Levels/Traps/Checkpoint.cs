using UnityEngine;

public class Checkpoint : MonoBehaviour
{

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

        playerMovement.UpdatePlayerSpawnClientRpc(transform.position, false);
    }
}
