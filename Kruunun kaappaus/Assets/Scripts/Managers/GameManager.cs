using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public BoardPath currentPath;
    public BoardPlayerInfo currentPlayer;
    [Header("Tiles")]
    public BoardTile emptyTile;
    public BoardTile minigameTile;
    public BoardTile challengeTile;
    public BoardTile shopTile;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        
    }

    // Kaikki liittyen peliin tulee tähän
}
