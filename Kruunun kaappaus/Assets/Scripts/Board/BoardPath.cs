using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;


public enum Tiles
{
    EmptyTile, MinigameTile, ChallengeTile, ShopTile
}
public class BoardPath : NetworkBehaviour
{
    public List<GameObject> tiles = new();
    public NetworkList<int> tilesIndex = new();

    private void Start()
    {
        tiles.Clear();
        foreach (Transform tile in transform)
        {
            var tileComponent = tile.GetComponent<BoardTile>();
            tiles.Add(tile.gameObject);
            if (IsServer)
            {
                tilesIndex.Add(GetTileIndex(tileComponent));
            }
        }

        tilesIndex.OnListChanged += UpdateTiles;
    }

    public void UpdateTiles(NetworkListEvent<int> changed)
    {
        // mul kesti 6 tuntii tajuu et se triggeraa eventin joka vaihetusta numerosta
        // (korjasin joka ikisen osan täst koodist sen takia)
        ReplaceTile(changed.Index, GetIndexTile(changed.Value));
    }
    private void ReplaceTile(int currentTileIndex, BoardTile newTile)
    {
        GameObject selectedTile = tiles[currentTileIndex];
        Destroy(selectedTile.GetComponent<BoardTile>());
        selectedTile.AddComponent(newTile.GetType());
    }
    private int GetTileIndex(BoardTile tile)
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
    private BoardTile GetIndexTile(int index)
    {
        switch (index)
        {
            case 0:
                return GameManager.instance.emptyTile;
            case 1:
                return GameManager.instance.minigameTile;
            case 2:
                return GameManager.instance.challengeTile;
            case 3:
                return GameManager.instance.shopTile;
        }

        Debug.LogError("Couldn't find from given index, returning empty");
        return GameManager.instance.emptyTile;
    }
    [ServerRpc(RequireOwnership = false)]
    public void ChangeTileIndexServerRpc(int[] indexArray, int[] newNumberArray)
    {
        NetworkList<int> temp = tilesIndex;
        for (int i = 0; i < indexArray.Length; i++)
        {
            temp[indexArray[i]] = newNumberArray[i];
        }

        tilesIndex = temp;
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
