using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public enum PlayerMovementState
{
    Idle, Moving, Jumping, Falling
}

public class PlayerMovement2D : NetworkBehaviour
{
    public bool isGhost;
    private Rigidbody2D rb;
    private BoxCollider2D playerCollider;
    private bool canJump;
    [SerializeField] private LayerMask ground;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpHeight;
    [SerializeField] private float maxCoyoteTime;
    [SerializeField] private float maxJumpBufferTime;
    private Animator animatorComponent;
    public Vector2 spawnPoint;
    private float coyoteTimer;
    private float jumpBufferTimer;
    public int coinCount;
    public PlayerMovementState currentPlayerState { get; private set; }
    void Start()
    {
        animatorComponent = GetComponent<Animator>();
        spawnPoint = LevelManager.instance.playerSpawnPoint[0];
        transform.position = spawnPoint;
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<BoxCollider2D>();
    }

    void Update()
    {
        if (!NetworkObject.IsOwner)
        {
            return;
        }

        ClampSpeed();
        PlayerStateManager();
        if (isGhost)
        {
            GhostMovement();
        }
        else
        {
            Jump();
            PlayerMovement();
        }
    }
    private bool IsGrounded()
    {
        return Physics2D.BoxCast(playerCollider.bounds.center, playerCollider.bounds.size, 0, Vector2.down, 0.1f, ground);
    }
    private void PlayerMovement()
    {
        // pelaajan asetukset
        rb.gravityScale = 5;
        rb.excludeLayers = default;

        float horizontal = Input.GetAxisRaw("Horizontal");

        rb.linearVelocity = new Vector2(horizontal * moveSpeed, rb.linearVelocity.y);
    }
    private void GhostMovement()
    {
        // haamu asetukset
        rb.excludeLayers = ground;
        rb.gravityScale = 0;


        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        rb.linearVelocity = new Vector2(horizontal * moveSpeed, vertical * moveSpeed);
    }
    private void Jump()
    {
        CoyoteCheck();
        JumpBuffer();
        if (jumpBufferTimer > 0 && canJump)
        {
            rb.linearVelocity = Vector3.up * jumpHeight;
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
            coyoteTimer = 0;
        }
    }
    private void JumpBuffer()
    {
        jumpBufferTimer -= Time.deltaTime;
        if (Input.GetKey(KeyCode.Space))
        {
            jumpBufferTimer = maxJumpBufferTime;
        }
    }
    private void ClampSpeed()
    {
        float horizontalSpeed = Mathf.Clamp(rb.linearVelocity.x, -moveSpeed, moveSpeed);
        float verticalSpeed = Mathf.Clamp(rb.linearVelocity.y, -jumpHeight, jumpHeight);
        rb.linearVelocity = new Vector2(horizontalSpeed, verticalSpeed);
    }
    private void PlayerStateManager()
    {
        UpdateAnimation();

        if (!IsGrounded())
        {
            currentPlayerState = PlayerMovementState.Jumping;
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
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Jos on haamu tai ei ole pelaaja objektin omistaja, niin ei mennä eteenpäin.
        if (isGhost || !NetworkObject.IsOwner) { return; }

        if (collision.CompareTag("Death Trigger"))
        {
            transform.position = spawnPoint;
            if (LevelManager.instance.currentLevelType == LevelType.Challenge)
            {
                LevelManager.instance.LoseHeartServerRpc();
            }
        }
        if (collision.CompareTag("Coin"))
        {
            GetComponent<PlayerInfo2D>().coinAmount.Value += 1;
            // Jotta se katoisi kaikilla pelaajilla, poistetaan sen networkobjectin kautta
            collision.gameObject.GetComponent<NetworkObject>().Despawn(true);
        }
        if (collision.CompareTag("Crown"))
        {
            GetComponent<PlayerInfo2D>().crownAmount.Value += 1;
            collision.gameObject.GetComponent<NetworkObject>().Despawn(true);
        }
    }
}
