using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class HexGrid : MonoBehaviour
{
    #region Variables

    [Header("Grid Size")]
    public int chunkCountX = 4, chunkCountZ = 3;
    public int cellCountX, cellCountZ;

    [Header("Tile Attributes")]
    public bool usePerlin;
    [Range(1, 50)]
    public float perlinMultiplier;
    [Range(0, 1)]
    public float innerHexProportion = 0.5f;

    [Header("Elevation and Terrace Settings")]
    [Range(0, 10)]
    public int elevationStep;
    [Range(0, 5)]
    public int terracesPerSlope;
    public bool flatTerraces;
    public bool upwardTrisForSoloBottomTerrace;

    [Header("Irregularity Settings")]
    public Texture2D noiseSource;
    [Range(0, 5)]
    public float cellPerturbStrength;
    [Range(0, 3)]
    public float elevationPerturbStrength;
    public int seed;


    [Header("Prefabs")]
    public HexCell cellPrefab;
    public HexGridChunk chunkPrefab;

    HexCell[] cells;
    HexGridChunk[] chunks;

    // labels
    public Text cellLabelPrefab;

    [Header("Color Settings")]
    // color
    public Color[] colors;

    #endregion

    private void OnEnable()
    {
        if (!HexMetrics.noiseSource)
        {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed); 
            HexMetrics.colors = colors;
        }
        HexMetrics.solidFactor = innerHexProportion;
        HexMetrics.elevationStep = elevationStep;
        HexMetrics.terracesPerSlope = terracesPerSlope;
        HexMetrics.cellPerturbStrength = cellPerturbStrength;
        HexMetrics.elevationPerturbStrength = elevationPerturbStrength;
    }

    void Awake()
    {
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);
        HexMetrics.colors = colors;

        HexMetrics.solidFactor = innerHexProportion;

        cellCountX = chunkCountX * HexMetrics.chunkSizeX;
        cellCountZ = chunkCountZ * HexMetrics.chunkSizeZ;

        CreateChunks();
        CreateCells();
    }

    void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for (int x = 0; x < chunkCountX; x++)
            {
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(transform);
                chunk.GetComponent<HexGridChunk>().flatTerraces = flatTerraces;
                chunk.GetComponent<HexGridChunk>().upwardTrisForSoloBottomTerrace = upwardTrisForSoloBottomTerrace;
            }
        }
    }

    void CreateCells()
    {
        cells = new HexCell[cellCountZ * cellCountX];

        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        return cells[index];
    }

    void CreateCell(int x, int z, int i)
    {
        Vector3 position;
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
        position.y = 0f;
        position.z = z * (HexMetrics.outerRadius * 1.5f);

        HexCell cell = cells[i] = Instantiate(cellPrefab);
        cell.transform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);

        // get neighbors
        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }
        if (z > 0)
        {
            if ((z & 1) == 0) // this returns true if z is even
            {
                cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                if (x < cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
                }
            }
        }

        Text label = Instantiate<Text>(cellLabelPrefab);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        label.text = cell.coordinates.ToStringOnSeparateLines();

        cell.uiRect = label.rectTransform;

        if (usePerlin) cell.Elevation = (int)(Mathf.PerlinNoise(x * .3f, z * .3f) * perlinMultiplier);
        else cell.Elevation = 0;

        AddCellToChunk(x, z, cell);
    }

    void AddCellToChunk(int x, int z, HexCell cell)
    {
        // find the right chunk
        int chunkX = x / HexMetrics.chunkSizeX;
        int chunkZ = z / HexMetrics.chunkSizeZ;
        HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

        // determine cell's local index to its chunk
        int localX = x - chunkX * HexMetrics.chunkSizeX;
        int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        int z = coordinates.Z;
        if (z < 0 || z >= cellCountZ)
        {
            return null;
        }
        int x = coordinates.X + z / 2;
        if (x < 0 || x >= cellCountX)
        {
            return null;
        }
        return cells[x + z * cellCountX];
    }

    public void ShowUI(bool visible)
    {
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].ShowUI(visible);
        }
    }

    public void Save(BinaryWriter writer)
    {
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Save(writer);
        }
    }

    public void Load(BinaryReader reader)
    {
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Load(reader);
        }
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].Refresh();
        }
    }
}
