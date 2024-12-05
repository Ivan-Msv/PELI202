using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;

public enum PlayerMovementState
{
    Idle, Moving, Jumping, Falling
}

public class PlayerMovement2D : NetworkBehaviour
{
    public bool isGhost;
    private bool canJump;
    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float ghostMoveSpeed;
    [SerializeField] private float xAxisDrag;
    [Header("Jump")]
    [SerializeField] private LayerMask ground;
    [SerializeField] private float minJumpHeight;
    [SerializeField] private float maxJumpHeight;
    [SerializeField] private float maxCoyoteTime;
    [SerializeField] private float maxJumpBufferTime;
    [SerializeField] private float maxJumpTime;
    public Vector2 spawnPoint;
    private Rigidbody2D rb;
    private BoxCollider2D playerCollider;
    private Animator animatorComponent;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float jumpTimer;
    private Transform spawnParent;
    public PlayerMovementState currentPlayerState { get; private set; }
    void Start()
    {
        if (!NetworkObject.IsOwner)
        {
            return;
        }
        animatorComponent = GetComponent<Animator>();
        spawnParent = transform.parent;
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<BoxCollider2D>();
    }

    void Update()
    {
        if (!NetworkObject.IsOwner || LevelManager.instance.CurrentGameState != LevelState.InProgress)
        {
            return;
        }
        CoyoteCheck();
        JumpBuffer();
    }

    private void FixedUpdate()
    {
        if (!NetworkObject.IsOwner)
        {
            return;
        }

        if (LevelManager.instance.CurrentGameState != LevelState.InProgress)
        {
            rb.linearVelocity = new Vector2(0, 0);
            rb.gravityScale = 0;
            return;
        }

        PlayerStateManager();
        if (isGhost)
        {
            GhostMovement();
        }
        else
        {
            StuckCheck();
            Jump();
            PlayerMovement();
        }
    }

    private bool IsGrounded()
    {
        return Physics2D.BoxCast(playerCollider.bounds.center, playerCollider.bounds.size, 0, Vector2.down, 0.1f, ground);
    }
    private bool InsideGround()
    {
        return Physics2D.BoxCast(playerCollider.bounds.center, new Vector2(0.2f, 0.2f), 0, Vector2.zero, 0, ground);
    }
    private void StuckCheck()
    {
        if (InsideGround())
        {
            transform.position = spawnPoint;
        }
    }
    private void PlayerMovement()
    {
        // pelaajan asetukset
        rb.gravityScale = 5;
        rb.excludeLayers = default;

        float horizontal = Input.GetAxisRaw("Horizontal");
        rb.linearVelocityX = rb.linearVelocity.x / (xAxisDrag + 1);
        rb.linearVelocityX += horizontal * moveSpeed;
    }
    private void GhostMovement()
    {
        // haamu asetukset
        rb.excludeLayers = ground;
        rb.gravityScale = 0;


        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        rb.linearVelocity = new Vector2(horizontal * ghostMoveSpeed, vertical * ghostMoveSpeed);
    }
    private void Jump()
    {
        if (jumpBufferTimer > 0 && canJump)
        {
            // min jump
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, minJumpHeight);
        }
        MaxJumpTimer();
    }

    private void MaxJumpTimer()
    {
        jumpTimer -= Time.deltaTime;
        bool falling = rb.linearVelocity.y <= 0;

        if (Input.GetKeyUp(KeyCode.Space))
        {
            jumpTimer = -1;
        }

        if (jumpTimer >= 0 && !falling && Input.GetKey(KeyCode.Space))
        {
            rb.AddRelativeForce(Vector3.up * maxJumpHeight * Time.deltaTime);
        }
    }
    private void CoyoteCheck()
    {
        if (!IsGrounded())
        {
            //tarkistaa että coyotetime toimii vaan jos yrität hyppää tippumisen jälkeen (ton koko pointti)
            bool falling = rb.linearVelocity.y <= 0;
            switch (falling)
            {
                case true:
                    coyoteTimer += Time.deltaTime;
                    if (coyoteTimer > maxCoyoteTime)
                    {
                        canJump = false;
                    }
                    break;
                case false:
                    canJump = false;
                    break;
            }
        }
        else
        {
            canJump = true;
            jumpTimer = maxJumpTime;
            coyoteTimer = 0;
        }
    }
    private void JumpBuffer()
    {
        jumpBufferTimer -= Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Space) && currentPlayerState != PlayerMovementState.Jumping)
        {
            jumpBufferTimer = maxJumpBufferTime;
        }
    }
    private void PlayerStateManager()
    {
        UpdateAnimation();

        if (!IsGrounded())
        {
            currentPlayerState = JumpOrFallCheck();
            return;
        }

        if (Input.GetAxisRaw("Horizontal") != 0)
        {
            currentPlayerState = PlayerMovementState.Moving;
            return;
        }
        else
        {
            currentPlayerState = PlayerMovementState.Idle;
        }
    }
    private void UpdateAnimation()
    {
        animatorComponent.SetBool("IsMoving", currentPlayerState == PlayerMovementState.Moving);
        animatorComponent.SetBool("IsJumping", currentPlayerState == PlayerMovementState.Jumping);
        animatorComponent.SetBool("IsFalling", currentPlayerState == PlayerMovementState.Falling);
    }
    private PlayerMovementState JumpOrFallCheck()
    {
        return rb.linearVelocity.y > 0 ? PlayerMovementState.Jumping : PlayerMovementState.Falling;
    }

    [ServerRpc]
    private void DestroyCoinServerRpc(ulong collisionObjectId)
    {
        var collision = NetworkManager.SpawnManager.SpawnedObjectsList.FirstOrDefault(collision => collision.NetworkObjectId == collisionObjectId);
        // Jotta se katoisi kaikilla pelaajilla, poistetaan sen networkobjectin kautta
        collision.gameObject.GetComponent<NetworkObject>().Despawn(true);
    }

    [ServerRpc]
    private void DestroyCrownServerRpc(ulong collisionObjectId)
    {
        var collision = NetworkManager.SpawnManager.SpawnedObjectsList.FirstOrDefault(collision => collision.NetworkObjectId == collisionObjectId);
        // Jotta se katoisi kaikilla pelaajilla, poistetaan sen networkobjectin kautta
        collision.gameObject.GetComponent<NetworkObject>().Despawn(true);
        LevelManager.instance.UpdateLevelStateClientRpc();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Jos on haamu tai ei ole pelaaja objektin omistaja, niin ei mennä eteenpäin.
        if (isGhost || !NetworkObject.IsOwner) { return; }

        collision.TryGetComponent(out NetworkObject containsNetworkObject);
        var collisionObjectId = containsNetworkObject.IsUnityNull() ? 0 : containsNetworkObject.NetworkObjectId;

        if (collision.CompareTag("Death Trigger"))
        {
            transform.position = spawnPoint;
        }
        if (collision.CompareTag("Coin"))
        {
            spawnParent.GetComponent<MainPlayerInfo>().coinAmount.Value += 1;
            DestroyCoinServerRpc(collisionObjectId);
        }
        if (collision.CompareTag("Crown"))
        {
            spawnParent.GetComponent<MainPlayerInfo>().crownAmount.Value += 1;
            DestroyCrownServerRpc(collisionObjectId);
        }
        if (collision.CompareTag("Platform"))
        {
            NetworkObject.TrySetParent(collision.transform);
            rb.interpolation = RigidbodyInterpolation2D.Extrapolate;
            collision.GetComponent<Platform>().SwitchStateServerRpc();
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!NetworkObject.IsOwner) { return; }
        if (collision.CompareTag("Platform"))
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            NetworkObject.TrySetParent(spawnParent);
        }
    }
}
