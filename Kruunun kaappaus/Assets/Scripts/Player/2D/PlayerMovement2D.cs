using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerMovement2D : NetworkBehaviour
{
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
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<BoxCollider2D>();
    }

    private void OnEnable()
    {
        // change spawnPoint = 
    }

    void Update()
    {
        if (NetworkObject.IsOwner)
        {
            Jump();
            AxisMovement();
        }
    }
    private bool IsGrounded()
    {
        return Physics2D.BoxCast(playerCollider.bounds.center, playerCollider.bounds.size, 0, Vector2.down, 0.1f, ground);
    }
    private void AxisMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");

        rb.velocity = new Vector2(horizontal * moveSpeed, rb.velocity.y);
    }
    private void Jump()
    {
        CoyoteCheck();
        JumpBuffer();
        if (jumpBufferTimer > 0 && canJump)
        {
            rb.velocity = Vector3.up * jumpHeight;
        }
    }
    private void CoyoteCheck()
    {
        if (!IsGrounded())
        {
            //tarkistaa että coyotetime toimii vaan jos yrität hyppää tippumisen jälkeen (ton koko pointti)
            bool falling = rb.velocity.y <= 0;
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Death Trigger"))
        {
            transform.position = spawnPoint;
        }
    }
}
