using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


enum PlatformState
{
    Idle, Starting, Moving, Cooldown, Returning
}

public class Platform : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private bool autonomousMovement;
    [SerializeField] private Vector2 endPosition;
    [SerializeField] private float timeTillMoveSeconds;
    [SerializeField] private float cooldownSeconds;
    [SerializeField] private float moveSpeed;
    private Animator animatorComponent;
    private Vector2 startPosition;
    [SerializeField] private float startingTimer;
    [SerializeField] private float cooldownTimer;
    private PlatformState currentState;
    private void Start()
    {
        animatorComponent = GetComponent<Animator>();
        startPosition = transform.position;
        currentState = autonomousMovement ? PlatformState.Starting : PlatformState.Idle;
    }

    private void Update()
    {
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
        // Tyhjä koska en keksinyt tänne mitään, eikä rehellisesti tarvi
    }
    private void StartingState()
    {
        startingTimer += Time.deltaTime;

        if (startingTimer > timeTillMoveSeconds)
        {
            currentState = PlatformState.Moving;
            startingTimer = 0;
        }
    }
    private void FallingState()
    {
        transform.position = Vector2.MoveTowards(transform.position, endPosition, moveSpeed * Time.deltaTime);
        if (Vector2.Distance(transform.position, endPosition) <= 0)
        {
            currentState = PlatformState.Cooldown;
        }
    }
    private void CooldownState()
    {
        cooldownTimer += Time.deltaTime;
        if (cooldownTimer > cooldownSeconds)
        {
            currentState = PlatformState.Returning;
            cooldownTimer = 0;
        }
    }
    private void ReturningState()
    {
        transform.position = Vector2.MoveTowards(transform.position, startPosition, moveSpeed * Time.deltaTime);
        if (Vector2.Distance(transform.position, startPosition) <= 0)
        {
            currentState = autonomousMovement ? PlatformState.Starting : PlatformState.Idle;
        }
    }
    [ServerRpc(RequireOwnership = false)]
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
}
