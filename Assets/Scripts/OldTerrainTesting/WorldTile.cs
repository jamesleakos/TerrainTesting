using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldTile : MonoBehaviour
{
    public int id;
    public HexMeshGenerator gen;

    private void OnMouseDown()
    {
        Debug.Log("Tile " + id.ToString() + " was clicked");
        gen.SelectWorldTile(id);
    }
}
