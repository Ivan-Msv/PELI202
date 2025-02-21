using UnityEngine;

public class SlimeBlock : MonoBehaviour
{
    [SerializeField] private float slowMultiplier;
    [SerializeField] private float anchorDistance;
    private PlayerMovement2D connectedPlayer;
    private Vector2 anchorPoint;

    private void ConnectPlayer(Collision2D collision)
    {
        connectedPlayer.isWallSticking = true;
        connectedPlayer.wallNormal = collision.contacts[0].normal;
        connectedPlayer.gravityNullified = true;
        anchorPoint = connectedPlayer.transform.position;
    }

    private void DisconnectPlayer()
    {
        connectedPlayer.isWallSticking = false;
        connectedPlayer.wallNormal = Vector2.zero;
        connectedPlayer.slowMultiplier = 0;
        connectedPlayer.gravityNullified = false;

        connectedPlayer = null;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Resets velocity so you don't instantly detach while moving right for example
        collision.rigidbody.linearVelocity = Vector2.zero;

        connectedPlayer = collision.collider.GetComponent<PlayerMovement2D>();
        ConnectPlayer(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (connectedPlayer == null) { return; }

        connectedPlayer.slowMultiplier = slowMultiplier;

        // Only disconnects if the player isn't grounded so the slow works properly
        bool overDistance = Vector2.Distance(connectedPlayer.transform.position, anchorPoint) > anchorDistance;
        if (overDistance && !connectedPlayer.IsGrounded())
        {
            DisconnectPlayer();
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (connectedPlayer == null) { return; }

        DisconnectPlayer();
    }
}
