using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;


enum PlatformState
{
    Idle, Starting, Moving, Cooldown, Returning
}

public class Platform : NetworkBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [Header("Movement")]
    [SerializeField] private bool autonomousMovement;
    [SerializeField] private GameObject endPosition;
    [SerializeField] private float timeTillMoveSeconds;
    [SerializeField] private float cooldownSeconds;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float startingTimer;
    [SerializeField] private float cooldownTimer;
    private Vector2 startPosition;
    private Animator animatorComponent;
    private PlatformState currentState;
    private void Start()
    {
        animatorComponent = GetComponent<Animator>();

        if (!IsServer)
        {
            return;
        }

        startPosition = transform.position;
        currentState = autonomousMovement ? PlatformState.Starting : PlatformState.Idle;
    }


    private void FixedUpdate()
    {
        if (!IsServer)
        {
            return;
        }

        SendLinearVelocityRpc(rb.linearVelocity);
        StateMovement();
    }

    private void StateMovement()
    {
        switch (currentState)
        {
            case PlatformState.Idle:
                IdleState();
                break;
            case PlatformState.Starting:
                animatorComponent.Play("Platform_Used");
                StartingState();
                break;
            case PlatformState.Moving:
                animatorComponent.Play("Platform_Used");
                FallingState();
                break;
            case PlatformState.Cooldown:
                animatorComponent.Play("Platform_Used");
                CooldownState();
                break;
            case PlatformState.Returning:
                animatorComponent.Play("Platform_Used");
                ReturningState();
                break;
        }
    }
    private void IdleState()
    {
        rb.linearVelocity = Vector2.zero;
    }
    private void StartingState()
    {
        rb.linearVelocity = Vector2.zero;
        startingTimer += Time.deltaTime;

        if (startingTimer > timeTillMoveSeconds)
        {
            currentState = PlatformState.Moving;
            startingTimer = 0;
        }
    }
    private void FallingState()
    {
        var moveDirection = (endPosition.transform.position - transform.position).normalized;
        rb.linearVelocity = moveDirection * moveSpeed;

        if (Vector2.Distance(transform.position, endPosition.transform.position) <= 0.1f)
        {
            currentState = PlatformState.Cooldown;
        }
    }
    private void CooldownState()
    {
        rb.linearVelocity = Vector2.zero;
        cooldownTimer += Time.deltaTime;
        if (cooldownTimer > cooldownSeconds)
        {
            currentState = PlatformState.Returning;
            cooldownTimer = 0;
        }
    }
    private void ReturningState()
    {
        var moveDirection = (startPosition - (Vector2)transform.position).normalized;
        rb.linearVelocity = moveDirection * moveSpeed;

        if (Vector2.Distance(transform.position, startPosition) <= 0.1f)
        {
            currentState = autonomousMovement ? PlatformState.Starting : PlatformState.Idle;
        }
    }
    [Rpc(SendTo.Server)]
    public void SwitchStateServerRpc()
    {
        if (autonomousMovement)
        {
            return;
        }

        switch (currentState)
        {
            case PlatformState.Idle:
                currentState = PlatformState.Starting;
                break;
            case PlatformState.Returning:
                currentState = PlatformState.Starting;
                break;
        }
    }

    [Rpc(SendTo.Everyone)]
    private void SendLinearVelocityRpc(Vector2 newVelocity)
    {
        rb.linearVelocity = newVelocity;
    }
}
