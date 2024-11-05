using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class BoardPath : MonoBehaviour
{
    public List<GameObject> tiles = new List<GameObject>();
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void UpdatePath()
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
        UpdatePath();
    }
}
