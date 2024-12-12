using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.Rendering.LookDev;
using UnityEngine;


public enum Tiles
{
    EmptyTile, MinigameTile, ChallengeTile, ShopTile
}
public class BoardPath : NetworkBehaviour
{
    public static BoardPath instance;
    public List<GameObject> tiles;

    private void Awake()
    {
        tiles = new();
        if (instance == null)
        {
            instance = this;
        }
        InitTiles();
    }

    private void Start()
    {
        if (GameManager.instance.tilesIndex.Count < 1)
        {
            AddNetworkTiles();
        }
        else
        {
            ResetTiles();
        }
        GameManager.instance.tilesIndex.OnListChanged += UpdateTiles;
    }



    public void InitTiles()
    {
        tiles.Clear();
        foreach (Transform tile in transform)
        {
            tiles.Add(tile.gameObject);
        }
    }

    private void AddNetworkTiles()
    {
        foreach (var tile in tiles)
        {
            GameManager.instance.tilesIndex.Add(GetTileIndex(tile.GetComponent<BoardTile>()));
        }
    }

    public void ResetTiles()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            BoardTile tileComponent = tiles[i].GetComponent<BoardTile>();
            if (GetTileIndex(tileComponent) == GameManager.instance.tilesIndex[i])
            {
                continue;
            }

            ReplaceTile(i, GetIndexTile(GameManager.instance.tilesIndex[i]));
        }
    }

    private void OnDisable()
    {
        GameManager.instance.tilesIndex.OnListChanged -= UpdateTiles;
    }

    public void UpdateTiles(NetworkListEvent<int> changed)
    {
        // mul kesti 6 tuntii tajuu et se triggeraa eventin joka vaihetusta numerosta
        // (korjasin joka ikisen osan täst koodist sen takia)
        ReplaceTile(changed.Index, GetIndexTile(changed.Value));
    }
    private void ReplaceTile(int currentTileIndex, BoardTile newTile)
    {
        var oldComponent = tiles[currentTileIndex].GetComponent<BoardTile>();
        DestroyImmediate(oldComponent);
        tiles[currentTileIndex].AddComponent(newTile.GetType());
    }
    public int GetTileIndex(BoardTile tile)
    {
        switch (tile)
        {
            case EmptyTile:
                return 0;
            case MinigameTile:
                return 1;
            case ChallengeTile:
                return 2;
            case ShopTile:
                return 3;
        }

        Debug.LogError("Couldn't find tile, returning empty");
        return 0;
    }
    public BoardTile GetIndexTile(int index)
    {
        switch ((Tiles)index)
        {
            case Tiles.EmptyTile:
                return GameManager.instance.emptyTile;
            case Tiles.MinigameTile:
                return GameManager.instance.minigameTile;
            case Tiles.ChallengeTile:
                return GameManager.instance.challengeTile;
            case Tiles.ShopTile:
                return GameManager.instance.shopTile;
        }

        Debug.LogError("Couldn't find from given index, returning empty");
        return GameManager.instance.emptyTile;
    }
    [ServerRpc(RequireOwnership = false)]
    public void ChangeTileIndexServerRpc(int index, int newNumber)
    {
        GameManager.instance.tilesIndex[index] = newNumber;
    }

    [Rpc(SendTo.Everyone)]
    public void SetCameraPositionAndActiveRpc(bool disable, int tileIndex = -5)
    {
        Transform cameraFollow = tileIndex == -5 ? null : tiles[tileIndex].transform;
        BoardUIManager.instance.virtualCamera.ChangeCameraFollow(disable, cameraFollow);
    }

    public void TileAnimation(int thisIndex, int newIndex)
    {
        StartCoroutine(TileAnimationCoroutine(thisIndex, newIndex));
    }

    private IEnumerator TileAnimationCoroutine(int thisIndex, int newIndex)
    {
        SetCameraPositionAndActiveRpc(true, newIndex);
        yield return new WaitForSeconds(0.1f);
        ChangeTileIndexServerRpc(newIndex, (int)Tiles.ChallengeTile);
        yield return new WaitForSeconds(2.2f);
        SetCameraPositionAndActiveRpc(false);
        yield return new WaitForSeconds(0.1f);
        ChangeTileIndexServerRpc(thisIndex, (int)Tiles.EmptyTile);
        PlayTileAnimationRpc(thisIndex, "EmptyTile_Switch");
        yield return new WaitForSeconds(tiles[thisIndex].GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length + 0.5f);
        GameManager.instance.challengeTile.GetComponent<ChallengeTile>().SelectRandomChallenge(thisIndex);
    }

    [Rpc(SendTo.Everyone)]
    public void PlayTileAnimationRpc(int tileIndex, string animation)
    {
        tiles[tileIndex].GetComponent<Animator>().Play(animation);
    }

    public void SplitPlayersOnTiles()
    {
        List<MainPlayerInfo> playerInfos = new();
        List<int> alreadyMatchingPositions = new();
        foreach (var player in GameManager.instance.availablePlayers)
        {
            playerInfos.Add(player.GetComponentInParent<MainPlayerInfo>());
        }

        for (int i = 0; i < playerInfos.Count; i++)
        {
            if (alreadyMatchingPositions.Contains(playerInfos[i].currentBoardPosition.Value))
            {
                Debug.Log("Already matching");
                continue;
            }

            var matching = playerInfos.FindAll(player => player.currentBoardPosition.Value == playerInfos[i].currentBoardPosition.Value);

            if (matching.Count > 1)
            {
                SplitPlayers(matching);
                alreadyMatchingPositions.Add(playerInfos[i].currentBoardPosition.Value);
            }
        }
    }
    private void SplitPlayers(List<MainPlayerInfo> players)
    {
        Vector2[] fourSplit = PlayerTilePositions(tiles[players[0].currentBoardPosition.Value].transform);
        for (int i = 0; i < players.Count; i++)
        {
            players[i].GetComponentInChildren<BoardPlayerInfo>().UpdatePlayerPositionClientRpc(fourSplit[i]);
        }
    }

    private Vector2[] PlayerTilePositions(Transform tile)
    {
        float playerWidth = 0.385f;
        float playerHeight = 0.5f;
        var positions = new Vector2[4];
        positions[0] = new Vector2(tile.transform.position.x - playerWidth, tile.transform.position.y);
        positions[1] = new Vector2(tile.transform.position.x + playerWidth, tile.transform.position.y);
        positions[2] = new Vector2(tile.transform.position.x - playerWidth, tile.transform.position.y - playerHeight);
        positions[3] = new Vector2(tile.transform.position.x - playerWidth, tile.transform.position.y - playerHeight);
        return positions;
    }
    private void OnDrawGizmos()
    {
        tiles.Clear();
        foreach (Transform tile in transform)
        {
            tiles.Add(tile.gameObject);
        }

        for (int i = 0; i < tiles.Count; i++)
        {
            Gizmos.DrawLine(tiles[i].transform.position, tiles[(i + 1) % tiles.Count].transform.position);
        }
    }
}
