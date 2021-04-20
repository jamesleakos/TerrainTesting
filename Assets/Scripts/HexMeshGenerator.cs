using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(MeshFilter))]

public class HexMeshGenerator : MonoBehaviour
{
    Mesh mesh;
    MeshMaterials meshMaterials = new MeshMaterials();
    List<LandTile> landTiles = new List<LandTile>();
    List<WorldTile> worldTiles = new List<WorldTile>();

    [Header("Grid Size")]
    public int xSize = 5;
    public int zSize = 5;

    [Header("Tile Attributes")]
    // how far apart the tiles are
    public float spacing = 1.0f;
    [Range(0, 1)]
    public float innerHexProportion = 0.5f;

    [Header("Prefabs")]
    public GameObject worldTilePrefab;
    WorldTile selectedTile;

    [Header("Adjusting the Mesh")]
    public float heightAdjustment = 1f;


    // Start is called before the first frame update
    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        CreateLandTiles();

        // more efficient to do it here, but in update we can see them be generated
        GenerateTriangles();
        UpdateMesh();

        GenerateHexColliders();
    }

    private void Update()
    {
        GetAdjustmentInput();
    }

    void GetAdjustmentInput()
    {
        if ((Input.GetKeyDown("f") || Input.GetKeyDown("c")) && selectedTile != null)
        {
            float adjustment = 0f;
            if (Input.GetKeyDown("f"))
            {
                adjustment = heightAdjustment;
            }
            if (Input.GetKeyDown("c"))
            {
                adjustment = heightAdjustment * -1;
            }
            var landTile = landTiles.Find(c => c.id == selectedTile.id);
            Vector3 pos = landTile.center;
            pos.y += adjustment;
            landTile.center = pos;
            landTile.GenerateInnerHexVertices(spacing);
            selectedTile.transform.position = pos;

            GenerateTriangles();
            UpdateMesh();
        }        
    }

    void CreateLandTiles()
    {

        int index = 0;
        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                LandTile landTile = new LandTile();
                landTile.id = index;
                landTile.row = z;
                landTile.innerHexProportion = innerHexProportion;

                // calculate center of the landtile
                float y = Mathf.PerlinNoise(x * .3f, z * .3f) * 2f;
                //float y = 0;
                landTile.center = new Vector3(x * spacing + spacing * (z % 2 == 0 ? 0f : 0.5f), y, z * 0.5f * spacing * Mathf.Sqrt(3));
                
                // calculate vertices, save them in landtile, and add them to the vertex list;
                landTile.GenerateInnerHexVertices(spacing);
                //foreach (var v in landTile.innerHexVertices) vertices.Add(v);

                landTiles.Add(landTile);
                index++;
            }
        }

        foreach (var landTile in landTiles)
        {
            landTile.CalculateNeighbors(landTiles, xSize);
        }
    }

    void GenerateTriangles()
    {
        meshMaterials.triangles.Clear();
        meshMaterials.vertices.Clear();

        var coveredTiles = new List<LandTile>();
        foreach (var landTile in landTiles)
        {
            meshMaterials = landTile.AddTriangles(meshMaterials, coveredTiles);
        }
    }

    void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = meshMaterials.vertices.ToArray();
        mesh.triangles = meshMaterials.triangles.ToArray();

        mesh.RecalculateNormals();
    }

    void GenerateHexColliders()
    {
        foreach (var landTile in landTiles)
        {
            var newTile = Instantiate(worldTilePrefab, landTile.center, new Quaternion (0,0,0,0));
            var newWorldTile = newTile.GetComponent<WorldTile>();
            newWorldTile.id = landTile.id;
            newWorldTile.gen = this;
            worldTiles.Add(newWorldTile);            
        }
    }

    public void SelectWorldTile (int id)
    {
        selectedTile = worldTiles.Find(c => c.id == id);
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
        innerHexVertices.Clear();
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
        neighbors.Clear();
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
            neighbors.Add(neighbor);
        }
    }

    public MeshMaterials AddTriangles (MeshMaterials meshMaterials, List<LandTile> coveredTiles)
    {
        coveredTiles.Add(this);

        // add interior triangles
        meshMaterials = AddSingleTriangle(meshMaterials, innerHexVertices[0], innerHexVertices[1], innerHexVertices[2]);
        meshMaterials = AddSingleTriangle(meshMaterials, innerHexVertices[0], innerHexVertices[2], innerHexVertices[5]);
        meshMaterials = AddSingleTriangle(meshMaterials, innerHexVertices[5], innerHexVertices[2], innerHexVertices[3]);
        meshMaterials = AddSingleTriangle(meshMaterials, innerHexVertices[5], innerHexVertices[3], innerHexVertices[4]);


        for (int i = 0; i < 6; i++)
        {
            // add neighbor box
            if (neighbors[i].exists && !coveredTiles.Contains(neighbors[i].landTile)) {
                meshMaterials = AddSingleTriangle(meshMaterials, GetVertex((0 + i) % 6), neighbors[i].landTile.GetVertex((4 + i) % 6), GetVertex((1 + i) % 6));
                meshMaterials = AddSingleTriangle(meshMaterials, GetVertex((1 + i) % 6), neighbors[i].landTile.GetVertex((4 + i) % 6), neighbors[i].landTile.GetVertex((3 + i) % 6));
            }
            // add thruple triangle
            if (neighbors[i].exists && !coveredTiles.Contains(neighbors[i].landTile) && neighbors[Mod(i - 1, 6)].exists && !coveredTiles.Contains(neighbors[Mod(i - 1, 6)].landTile))
            {
                meshMaterials = AddSingleTriangle(meshMaterials, GetVertex((0 + i) % 6), neighbors[Mod(i - 1, 6)].landTile.GetVertex((2 + i) % 6), neighbors[(0 + i) % 6].landTile.GetVertex((4 + i) % 6));
            }
        }

        return meshMaterials;
    }

    public MeshMaterials ComplexAddTriangles(MeshMaterials meshMaterials, List<LandTile> coveredTiles)
    {
        coveredTiles.Add(this);

        // add interior triangles
        meshMaterials = AddSingleTriangle(meshMaterials, innerHexVertices[0], innerHexVertices[1], innerHexVertices[2]);
        meshMaterials = AddSingleTriangle(meshMaterials, innerHexVertices[0], innerHexVertices[2], innerHexVertices[5]);
        meshMaterials = AddSingleTriangle(meshMaterials, innerHexVertices[5], innerHexVertices[2], innerHexVertices[3]);
        meshMaterials = AddSingleTriangle(meshMaterials, innerHexVertices[5], innerHexVertices[3], innerHexVertices[4]);


        for (int i = 0; i < 6; i++)
        {
            // add neighbor box
            if (neighbors[i].exists && !coveredTiles.Contains(neighbors[i].landTile))
            {
                meshMaterials = AddSingleTriangle(meshMaterials, GetVertex((0 + i) % 6), neighbors[i].landTile.GetVertex((4 + i) % 6), GetVertex((1 + i) % 6));
                meshMaterials = AddSingleTriangle(meshMaterials, GetVertex((1 + i) % 6), neighbors[i].landTile.GetVertex((4 + i) % 6), neighbors[i].landTile.GetVertex((3 + i) % 6));
            }
            // add thruple triangle
            if (neighbors[i].exists && !coveredTiles.Contains(neighbors[i].landTile) && neighbors[Mod(i - 1, 6)].exists && !coveredTiles.Contains(neighbors[Mod(i - 1, 6)].landTile))
            {
                meshMaterials = AddSingleTriangle(meshMaterials, GetVertex((0 + i) % 6), neighbors[Mod(i - 1, 6)].landTile.GetVertex((2 + i) % 6), neighbors[(0 + i) % 6].landTile.GetVertex((4 + i) % 6));
            }
        }

        return meshMaterials;
    }

    public MeshMaterials AddSingleTriangle (MeshMaterials meshMaterials, Vector3 one, Vector3 two, Vector3 three)
    {
        meshMaterials.vertices.Add(one);
        meshMaterials.triangles.Add(meshMaterials.vertices.Count - 1);

        meshMaterials.vertices.Add(two);
        meshMaterials.triangles.Add(meshMaterials.vertices.Count - 1);

        meshMaterials.vertices.Add(three);
        meshMaterials.triangles.Add(meshMaterials.vertices.Count - 1);

        return meshMaterials;
    }
    public Vector3 GetVertex(int localVertex) {
        return innerHexVertices[localVertex]; 
    }
    int Mod(int x, int m)
    {
        return (x % m + m) % m;
    }
}

public class LandTileNeighbor
{
    public bool exists;
    public int neighborPos;
    public LandTile landTile;
    

}

public class MeshMaterials
{
    public List<int> triangles = new List<int>();
    public List<Vector3> vertices = new List<Vector3>();
}
