using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

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
    public Vector2 spawnPoint;
    private float coyoteTimer;
    private float jumpBufferTimer;
    void Start()
    {
        spawnPoint = new Vector2(-7, -4);
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isGhost) { return; }

        if (collision.CompareTag("Death Trigger"))
        {
            transform.position = spawnPoint;
        }
    }
}
