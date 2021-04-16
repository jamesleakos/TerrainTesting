using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(MeshFilter))]

public class HexMeshGenerator : MonoBehaviour
{
    Mesh mesh;

    List<LandTile> landTiles = new List<LandTile>();
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();

    // how far apart the tiles are
    public float spacing = 1.0f;

    public int xSize = 5;
    public int zSize = 5;


    // Start is called before the first frame update
    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        CreateShape();

        // more efficient to do it here, but in update we can see them be generated
        //UpdateMesh();
    }

    private void Update()
    {
        //UpdateMesh();
    }

    void CreateShape()
    {

        int index = 0;
        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                LandTile landTile = new LandTile();
                landTile.id = index;
                landTile.row = z;

                // calculate center of the landtile
                //float y = Mathf.PerlinNoise(x * .3f, z * .3f) * 4f;
                float y = 0;
                landTile.center = new Vector3(x * spacing + spacing * (z % 2 == 0 ? 0f : 0.5f), y, z * 0.5f * spacing * Mathf.Sqrt(3));
                
                // calculate vertices, save them in landtile, and add them to the vertex list;
                landTile.GenerateInnerHexVertices(spacing);
                foreach (var v in landTile.innerHexVertices) vertices.Add(v);

                landTiles.Add(landTile);
                index++;
            }
        }

        foreach (var landTile in landTiles)
        {
            landTile.CalculateNeighbors(landTiles, xSize);
        }

        foreach (var landTile in landTiles)
        {
            triangles = landTile.AddTriangles(triangles);
            Debug.Log("Better check to make sure this whole reference type this works");
        }
    }

    void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        mesh.RecalculateNormals();
    }


    #region Gizmos

    private void OnDrawGizmos()
    {
        if (landTiles == null)
            return;

        foreach (var landTile in landTiles)
        {
            drawString(landTile.id.ToString(), landTile.center);
            for (int i = 0; i < landTile.innerHexVertices.Count; i++)
            {
                //drawString(i.ToString(), landTile.innerHexVertices[i]);
                //Gizmos.DrawSphere(landTile.innerHexVertices[i], 0.1f);
            }
        }
    }
    static void drawString(string text, Vector3 worldPos, Color? colour = null)
    {
        UnityEditor.Handles.BeginGUI();
        if (colour.HasValue) GUI.color = colour.Value;
        var view = UnityEditor.SceneView.currentDrawingSceneView;
        Vector3 screenPos = view.camera.WorldToScreenPoint(worldPos);
        Vector2 size = GUI.skin.label.CalcSize(new GUIContent(text));
        GUI.Label(new Rect(screenPos.x - (size.x / 2), -screenPos.y + view.position.height + 4, size.x, size.y), text);
        UnityEditor.Handles.EndGUI();
    }

    #endregion
}



public class LandTile
{
    public int id;
    public Vector3 center;
    public int row;
    public float innerHexProportion = 0.7f;

    public List<Vector3> innerHexVertices = new List<Vector3>();
    public List<LandTileNeighbor> neighbors = new List<LandTileNeighbor>();

    public void GenerateInnerHexVertices(float spacing)
    {
        float innerApothem = innerHexProportion * spacing * 0.5f;
        float radius = innerApothem * 2f / Mathf.Sqrt(3);
        for (int i = 0; i < 6; i++)
        {
            Vector3 v3 = new Vector3(center.x + radius * Mathf.Sin(2 * Mathf.PI * i / 6), center.y, center.z + radius * Mathf.Cos(2 * Mathf.PI * i / 6));
            innerHexVertices.Add(v3);
        }
    }

    public void CalculateNeighbors (List<LandTile> landTiles, int xSize)
    {
        for (int i = 0; i < 6; i++)
        {
            LandTileNeighbor neighbor = new LandTileNeighbor();
            neighbor.neighborPos = i;
            LandTile n = null;

            //need to find out whether in an odd or even row
            int xAdd = xSize + (row % 2 == 0 ? 0 : 1);
            int xSub = xSize - (row % 2 == 0 ? 0 : 1);
            switch (i)
            {
                case 0:
                    n = landTiles.Find(c => c.id == id + xAdd && c.row == row + 1);
                    break;
                case 1:
                    n = landTiles.Find(c => c.id == id + 1 && c.row == row);
                    break;
                case 2:
                    n = landTiles.Find(c => c.id == id - xSub && c.row == row - 1);
                    break;
                case 3:
                    n = landTiles.Find(c => c.id == id  - xSub - 1 && c.row == row - 1);
                    break;
                case 4:
                    n = landTiles.Find(c => c.id == id - 1 && c.row == row);
                    break;
                case 5:
                    n = landTiles.Find(c => c.id == id + xAdd - 1 && c.row == row + 1);
                    break;
                default:
                    break;
            }
            neighbor.landTile = n;
            neighbor.exists = n != null;
        }
    }

    public List<int> AddTriangles (List<int> triangles)
    {



        return triangles;
    }
}

public class LandTileNeighbor
{
    public bool exists;
    public int neighborPos;
    public LandTile landTile;
    

}
