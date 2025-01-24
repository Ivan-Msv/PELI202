using System.Collections;
using System.Linq;
using Unity.Netcode;
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
    private bool isShooting;

    [Header("External Forces")]
    [SerializeField] private float portalCooldown;
    [SerializeField] private Vector2 externalForce;
    [SerializeField] private float forceDampSpeed;
    [field: SerializeField] public float DefaultGravity { get; private set; }
    public bool CanUsePortal { get; private set; } = true;
    public bool IsUsingPortal { get; set; }

    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float ghostMoveSpeed;

    [Header("Jump")]
    [SerializeField] private LayerMask ground, transparentFx;
    [SerializeField] private ContactFilter2D groundFilter;
    [SerializeField] private float maxFloatRadius;
    [SerializeField] private float jumpHeight;
    [SerializeField] private float maxTimeInAir;
    [SerializeField] private float maxCoyoteTime;
    [SerializeField] private float maxJumpBufferTime;
    private float timeInAir;

    [Header("Shootpoint And Projectile Settings")]
    [SerializeField] private Transform shootPoint;
    [SerializeField] private GameObject projectile;
    
    public Vector2 spawnPoint;
    [SerializeField] private PlayerInfo2D playerInfo;
    [Range (0f, 5f)]
    public float maxChargeTime;

    private Rigidbody2D rb;
    private BoxCollider2D playerCollider;
    private Animator animatorComponent;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float pushTimer;
    private Transform spawnParent;
    private float chargeTimer;
    private SpriteRenderer spritComp;
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
        spritComp = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (!NetworkObject.IsOwner || LevelManager.instance.CurrentGameState.Value != LevelState.InProgress)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            Time.timeScale = 0.1f;
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            Time.timeScale = 10f;
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            Time.timeScale = 1f;
        }

        if (Input.GetKeyDown(KeyCode.F4))
        {
            Application.targetFrameRate = 15;
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            Application.targetFrameRate = 0;
        }

        if (isGhost)
        {
            GhostMovement();
            CanPush();
            return;
        }

        if (IsUsingPortal)
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

        if (LevelManager.instance.CurrentGameState.Value != LevelState.InProgress)
        {
            rb.linearVelocity = new Vector2(0, 0);
            rb.gravityScale = 0;
            return;
        }

        PlayerStateManager();

        if (isGhost)
        {
            return;
        }

        GravityScales();
        StuckCheck();
        LimitFallSpeed();
        DampExternalForce();

        HorizontalMovement();
        Jump();
    }

    private bool IsGrounded()
    {
        //return Physics2D.BoxCast(playerCollider.bounds.center, playerCollider.bounds.size, 0, Vector2.down, 0.1f, ground);
        return rb.IsTouching(groundFilter);
    }

    private bool InsideGround()
    {
        // Using default and not "ground" because platforms are assigned as ground and you can jump through them
        return Physics2D.BoxCast(playerCollider.bounds.center, new Vector2(0.2f, 0.2f), 0, Vector2.zero, 0, LayerMask.NameToLayer("Default"));
    }

    private void StuckCheck()
    {
        if (InsideGround())
        {
            transform.position = spawnPoint;
        }
    }

    private void LimitFallSpeed()
    {
        rb.linearVelocityY = Mathf.Clamp(rb.linearVelocityY, -20, Mathf.Infinity);
    }

    public void SetPortalCooldown()
    {
        StartCoroutine(PortalCoroutine());
    }

    private IEnumerator PortalCoroutine()
    {
        CanUsePortal = false;
        yield return new WaitForSeconds(portalCooldown);
        CanUsePortal = true;
    }

    private void HorizontalMovement()
    {
        // pelaajan asetukset
        rb.excludeLayers = default;

        var playerVelocity = GetPlayerHorizontalVelocity();

        var totalVelocity = playerVelocity + externalForce.x;

        // set velocity
        rb.linearVelocityX = totalVelocity;
    }

    private void GhostMovement()
    {
        // haamu asetukset
        rb.excludeLayers = ground | transparentFx;
        rb.gravityScale = 0;

        if (isShooting)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = GetGhostVelocity();
    }

    private void Jump()
    {
        var totalVelocity = jumpHeight * Time.fixedDeltaTime + externalForce.y;

        if (jumpBufferTimer > 0 && canJump)
        {
            rb.linearVelocityY = totalVelocity;
            jumpBufferTimer = -1;
            timeInAir = 0;
        }

        if (Input.GetAxisRaw("Vertical") <= 0 && timeInAir < maxTimeInAir)
        {
            timeInAir = maxTimeInAir;
        }

        if (Input.GetKey(KeyCode.Space) && timeInAir < maxTimeInAir)
        {
            rb.linearVelocityY = totalVelocity;
        }

        if (externalForce.y != 0 && timeInAir >= maxTimeInAir)
        {
            rb.linearVelocityY = externalForce.y;
        }

        timeInAir += Time.deltaTime;
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

        if (!CanUsePortal)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) && currentPlayerState != PlayerMovementState.Jumping)
        {
            jumpBufferTimer = maxJumpBufferTime;
        }
    }
    private void CanPush()
    {
        //Color col = spritComp.color;
        //col.a = 0.5f;
        var userInput = Input.GetAxisRaw("Horizontal");
        
        if (userInput != 0)
        {
            shootPoint.SetLocalPositionAndRotation(new(userInput, 0, 0), new(0, 0, userInput * 180, shootPoint.rotation.w));
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isShooting = true;
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            isShooting = false;
            chargeTimer = 0;
            spritComp.color = Color.white;
        }
        if (Input.GetKey(KeyCode.Space))
        {
            spritComp.color = Color.red;
            
            //spritComp.color = col;
            if (chargeTimer >= maxChargeTime)
            {
                chargeTimer = 0;
                PushServerRpc();
            }
            chargeTimer += Time.deltaTime;
            Debug.Log(chargeTimer);

        }
        
    }

    [Rpc(SendTo.Server)]
    private void PushServerRpc()
    {
        NetworkObject.InstantiateAndSpawn(projectile, NetworkManager, position: shootPoint.position, rotation: shootPoint.rotation);
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
        return rb.linearVelocity.y < 0 ? PlayerMovementState.Falling : PlayerMovementState.Jumping;
    }

    private void GravityScales()
    {
        if (IsUsingPortal)
        {
            return;
        }

        // Faster falling
        if (currentPlayerState == PlayerMovementState.Falling)
        {
            rb.gravityScale = DefaultGravity * 2;
        }

        // Giving some floating time when near end of the jump
        else if (Mathf.Abs(rb.linearVelocityY) < maxFloatRadius && !IsGrounded())
        {
            rb.gravityScale = DefaultGravity / 2;
        }

        // Resets back to normal gravity scale
        else
        {
            rb.gravityScale = DefaultGravity;
        }
    }

    private float GetPlayerHorizontalVelocity()
    {
        var horizontal = Input.GetAxisRaw("Horizontal");

        return horizontal * moveSpeed * Time.fixedDeltaTime;
    }

    private Vector2 GetGhostVelocity()
    {
        // Not using raw to get that floaty "ghosty" movement
        var horizontal = Input.GetAxis("Horizontal");
        var vertical = Input.GetAxis("Vertical");

        return new(horizontal * ghostMoveSpeed * Time.fixedDeltaTime, vertical * ghostMoveSpeed * Time.fixedDeltaTime);
    }

    public void AddExternalForce(Vector2 force)
    {
        externalForce += force;
    }

    private void DampExternalForce()
    {
        // this gradually removes the external force, so it doesn't stay forever
        if (Mathf.Abs(externalForce.x) < 0.5f)
        {
            externalForce.x = 0;
        }

        if (Mathf.Abs(externalForce.y) < 3)
        {
            externalForce.y = 0;
        }

        externalForce = Vector2.Lerp(externalForce, Vector2.zero, Time.fixedDeltaTime * forceDampSpeed);
    }

    [Rpc(SendTo.Owner)]
    public void UpdatePlayerSpawnClientRpc(Vector2 newSpawnPoint)
    {
        if (!NetworkObject.IsOwner)
        {
            return;
        }

        spawnPoint = newSpawnPoint;
        transform.position = spawnPoint;
    }

    [Rpc(SendTo.Server)]
    private void DestroyCrownServerRpc(ulong collisionObjectId)
    {
        var collision = NetworkManager.SpawnManager.SpawnedObjectsList.FirstOrDefault(collision => collision.NetworkObjectId == collisionObjectId);
        // Jotta se katoisi kaikilla pelaajilla, poistetaan sen networkobjectin kautta
        collision.gameObject.GetComponent<NetworkObject>().Despawn(true);
        LevelManager.instance.CurrentGameState.Value = LevelState.Ending;
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
            playerInfo.localCoinAmount.Value++;
            spawnParent.GetComponent<MainPlayerInfo>().coinAmount.Value++;
            collision.gameObject.GetComponent<Coin>().CollectCoinRpc();
            LevelManager.instance.coinCounter.AddCollectedCoinServerRpc();
        }
        if (collision.CompareTag("Crown"))
        {
            spawnParent.GetComponent<MainPlayerInfo>().crownAmount.Value++;
            DestroyCrownServerRpc(collisionObjectId);
        }
        if (collision.CompareTag("Platform"))
        {
            NetworkObject.TrySetParent(collision.transform);
            rb.interpolation = RigidbodyInterpolation2D.Extrapolate;
            collision.GetComponent<Platform>().SwitchStateServerRpc();
        }
        if (collision.CompareTag("Bullet platform"))
        {
            NetworkObject.TrySetParent(collision.transform);
            rb.interpolation = RigidbodyInterpolation2D.Extrapolate;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!NetworkObject.IsOwner) { return; }
        if (collision.CompareTag("Platform") || collision.CompareTag("Bullet platform"))
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            NetworkObject.TrySetParent(spawnParent);
        }
    }
}
