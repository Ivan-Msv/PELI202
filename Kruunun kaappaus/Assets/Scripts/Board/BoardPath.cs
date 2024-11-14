using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class BoardPath : MonoBehaviour
{
    public List<GameObject> tiles = new List<GameObject>();

    private void UpdateTiles()
    {
        // Jos ei poista, niin gizmo lisää niitä ikuisesti
        tiles.Clear();
        // Lisää uusii tilei listalle
        foreach (Transform child in transform)
        {
            tiles.Add(child.gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        UpdateTiles();

        for (int i = 0; i < tiles.Count; i++)
        {
            Gizmos.DrawLine(tiles[i].transform.position, tiles[(i + 1) % tiles.Count].transform.position);
        }
    }
}
