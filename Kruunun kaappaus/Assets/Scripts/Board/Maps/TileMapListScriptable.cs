using System;
using UnityEngine;

[Serializable]
public struct TileMapSettings
{
    public int playerLimit;
    public string[] sceneList;
}

[CreateAssetMenu(menuName = "Maps/Tile Maplist")]
public class TileMapListScriptable : ScriptableObject
{
    public TileMapSettings[] mapSettings;
}
