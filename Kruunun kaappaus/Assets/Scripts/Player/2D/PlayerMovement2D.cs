using System.Collections;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;
using System.ComponentModel;
using Unity.Netcode.Components;
using System.Runtime.CompilerServices;

public enum PlayerMovementState
{
    Idle, Moving, Jumping, Falling
}

public class PlayerMovement2D : NetworkBehaviour
{
    [SerializeField] private PlayerInfo2D playerInfo;
    [SerializeField] private NetworkTransform netTransform;
    public bool isGhost;
    public Vector2 spawnPoint;
    private bool canJump;
    private bool isShooting;
    public bool CanUsePortal { get; private set; } = true;
    public bool IsUsingPortal { get; set; }

    [Header("External Multipliers")]
    public bool isWallSticking;
    public Vector2 wallNormal;
    public bool gravityNullified;
    public float slowMultiplier;

    [Header("External Force Settings")]
    [SerializeField] private float portalCooldown;
    [SerializeField] private Vector2 forceDampSpeed;
    [SerializeField] private Vector2 forceDampCap;

    [ReadOnlyInspector]
    [SerializeField] private Vector2 externalForce;
    [ReadOnlyInspector]
    [SerializeField] private Vector2 platformForces;
    [ReadOnlyInspector]
    [SerializeField] private bool onPlatform;

    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float ghostMoveSpeed;
    [SerializeField] private float wallJumpForce;

    [Header("Jump")]
    [SerializeField] private LayerMask ground;
    [SerializeField] private LayerMask transparentFx;
    [SerializeField] private ContactFilter2D groundFilter;
    [SerializeField] private float maxFloatRadius;
    [SerializeField] private float jumpHeight;
    [SerializeField] private float maxTimeInAir;
    [SerializeField] private float maxCoyoteTime;
    [SerializeField] private float maxJumpBufferTime;
    private float airTime;
    private float jumpAirTime;
    [field: SerializeField] public float DefaultGravity { get; private set; }

    [Header("Animation stuff")]
    [SerializeField] private float jumpAnimationThreshold;
    [SerializeField] private GameObject deathSprite;

    [Header("Shootpoint And Projectile Settings")]
    [SerializeField] private Transform shootPoint;
    [SerializeField] private GameObject projectile;
    public float maxChargeTime;

    private Animator animatorComponent;
    private Transform spawnParent;
    private Rigidbody2D rb;
    private BoxCollider2D playerCollider;
    private SpriteRenderer spriteComponent;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float pushTimer;
    private float chargeTimer;
    
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
        spriteComponent = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
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
            LevelManager.instance.LevelTimer.Value = Mathf.Infinity;
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            playerInfo.playerIsGhost.Value = !playerInfo.playerIsGhost.Value;
        }

        if (!NetworkObject.IsOwner || LevelManager.instance.CurrentGameState.Value != LevelState.InProgress)
        {
            return;
        }

        if (isGhost)
        {
            GhostMovement();
            CanPush();
            return;
        }

        RestartHotkey();

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
        UpdateAirTimer();
        DampExternalForce();

        HorizontalMovement();
        Jump();
    }

    public bool IsGrounded()
    {
        //return Physics2D.BoxCast(playerCollider.bounds.center, playerCollider.bounds.size, 0, Vector2.down, 0.1f, ground);
        return rb.IsTouching(groundFilter);
    }

    public bool InsideGround()
    {
        // Using default and not "ground" because platforms are assigned as ground and you can jump through them
        return Physics2D.BoxCast(playerCollider.bounds.center, new Vector2(0.2f, 0.2f), 0, Vector2.zero, 0, LayerMask.GetMask("Default"));
    }

    private void StuckCheck()
    {
        if (InsideGround())
        {
            transform.position = spawnPoint;
        }
    }

    private void UpdateAirTimer()
    {
        if (IsGrounded())
        {
            airTime = 0;
            return;
        }

        airTime += Time.deltaTime;
    }

    private void RestartHotkey()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RespawnPlayer();
        }
    }

    private void RespawnPlayer()
    {
        // This prevents an issue, where you might get teleported while still parented
        // which would teleport you from local position of a parent to a wrong position
        NetworkObject.TrySetParent(spawnParent);

        // Also reset velocity
        rb.linearVelocity = Vector2.zero;
        externalForce = Vector2.zero;
        platformForces = Vector2.zero;

        // To prevent spamming particles
        if (Vector2.Distance(spawnPoint, transform.position) > 1f)
        {
            PlayDeathAnimationRpc();
        }

        netTransform.Teleport(spawnPoint, transform.rotation, transform.localScale);
    }

    [Rpc(SendTo.Everyone)]
    private void PlayDeathAnimationRpc()
    {
        Instantiate(deathSprite, transform.position, deathSprite.transform.rotation);
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

        var totalVelocity = playerVelocity + externalForce.x + platformForces.x;

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
        var externalForces = externalForce.y + platformForces.y;
        var totalVelocity = jumpHeight * (1f - slowMultiplier) * Time.fixedDeltaTime + externalForces;

        if (jumpBufferTimer > 0 && canJump)
        {
            AudioManager.instance.PlaySoundAtPositionRpc(SoundType.Jump, NetworkObjectId, true);
            rb.linearVelocityY = totalVelocity;
            jumpBufferTimer = -1;
            jumpAirTime = 0;

            if (isWallSticking)
            {
                AddExternalForce(new(-wallNormal.x * wallJumpForce, 0));
            }
        }

        if (Input.GetAxisRaw("Vertical") <= 0 && jumpAirTime < maxTimeInAir)
        {
            jumpAirTime = maxTimeInAir;
        }

        if (Input.GetKey(KeyCode.Space) && jumpAirTime < maxTimeInAir && !isWallSticking)
        {
            rb.linearVelocityY = totalVelocity;
        }

        if (externalForce.y != 0 && jumpAirTime >= maxTimeInAir)
        {
            rb.linearVelocityY = externalForce.y;
        }

        jumpAirTime += Time.deltaTime;
    }

    private void CoyoteCheck()
    {
        if (IsUsingPortal)
        {
            return;
        }

        if (!IsGrounded() && !isWallSticking)
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

        if (IsUsingPortal)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) && currentPlayerState != PlayerMovementState.Jumping || Input.GetKeyDown(KeyCode.Space) && isWallSticking)
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
            if (userInput == 1)
            {
                shootPoint.SetLocalPositionAndRotation(new(userInput, 0, 0), new(0, 0, userInput * 0, shootPoint.rotation.w));
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isShooting = true;
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            isShooting = false;
            chargeTimer = 0;
            spriteComponent.color = new Color(1,1,1,0.5f);
        }
        if (Input.GetKey(KeyCode.Space))
        {
            spriteComponent.color = new Color(1, 0, 0, 0.5f);  
            
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

        if (!IsGrounded() && airTime > jumpAnimationThreshold)
        {
            currentPlayerState = JumpOrFallCheck();
            return;
        }

        if (Input.GetAxisRaw("Horizontal") != 0)
        {
            //AudioManager.PlaySound(SoundType.FootSteps);
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

        // In case you want to nullify the gravity
        if (gravityNullified)
        {
            rb.gravityScale = 0;
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
        var remainingSpeed = 1f - slowMultiplier;

        return horizontal * moveSpeed * remainingSpeed * Time.fixedDeltaTime;
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
        if (Mathf.Abs(externalForce.x) < forceDampCap.x)
        {
            externalForce.x = 0;
        }

        if (Mathf.Abs(externalForce.y) < forceDampCap.y)
        {
            externalForce.y = 0;
        }

        externalForce.x = Mathf.Lerp(externalForce.x, 0, Time.fixedDeltaTime * forceDampSpeed.x);
        externalForce.y = Mathf.Lerp(externalForce.y, 0, Time.fixedDeltaTime * forceDampSpeed.y);
    }

    [Rpc(SendTo.Owner)]
    public void UpdatePlayerSpawnClientRpc(Vector2 newSpawnPoint, bool updatePosition = true)
    {
        if (!NetworkObject.IsOwner)
        {
            return;
        }

        spawnPoint = newSpawnPoint;

        if (!updatePosition)
        {
            return;
        }

        transform.position = spawnPoint;
    }

    [Rpc(SendTo.Server)]
    private void DestroyCrownServerRpc(ulong collisionObjectId)
    {
        AudioManager.instance.PlaySoundAtPositionRpc(SoundType.CrownPickUp, NetworkObjectId, false);
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

        if (collision.CompareTag("Death Trigger") || collision.CompareTag("Bullet Platform"))
        {
            RespawnPlayer();
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
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Platform") && IsGrounded())
        {
            collision.GetComponent<Platform>().SwitchStateServerRpc();
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isGhost || !NetworkObject.IsOwner) { return; }

        if (collision.collider.CompareTag("Bullet Platform") || collision.collider.CompareTag("Platform"))
        {
            if (!IsGrounded()) { return; }

            if (!onPlatform)
            {
                // Set parent because network tick sync is only applied to children for some reason...
                NetworkObject.TrySetParent(collision.transform);
                //rb.interpolation = RigidbodyInterpolation2D.None;
                onPlatform = true;
            }

            platformForces = collision.rigidbody.linearVelocity;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (isGhost || !NetworkObject.IsOwner) { return; }

        if (collision.collider.CompareTag("Platform") || collision.collider.CompareTag("Bullet Platform"))
        {
            if (!onPlatform) { return; }

            NetworkObject.TrySetParent(spawnParent);
            //rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            onPlatform = false;
            AddExternalForce(platformForces);
            platformForces = Vector2.zero;
        }
    }
}
