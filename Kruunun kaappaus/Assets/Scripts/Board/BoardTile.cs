﻿using Unity.Netcode;
using UnityEngine;

public abstract class BoardTile : NetworkBehaviour
{
    public Sprite tileSprite;
    public string tileName;
    public GameObject minimapSprite;
    public Animator anim;

    private void Start()
    {
        anim = GetComponent<Animator>();

        SetupTile();

        // Deletes previous minimap sprite
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        GetComponent<SpriteRenderer>().sprite = tileSprite;
        name = tileName;
        Instantiate(minimapSprite, transform);
    }
    public abstract void SetupTile();
    public abstract void InvokeTile();

    public int GetTilePosition()
    {
        return BoardPath.instance.tiles.FindIndex(tile => tile == gameObject);
    }

    public void PlayAnimation(string givenAnimation)
    {
        anim.Play(givenAnimation);
    }
}
