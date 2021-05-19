using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
    [Header("Grid Size")]
    public int width = 6;
    public int height = 6;

    [Header("Tile Attributes")]
    public bool usePerlin;
    [Range(1, 20)]
    public float perlinMultiplier;
    [Range(0, 1)]
    public float innerHexProportion = 0.5f;

    [Header("Cell Prefabs")]
    public HexCell cellPrefab;

    HexCell[] cells;

    // labels
    public Text cellLabelPrefab;
    Canvas gridCanvas;

    // mesh
    HexMesh hexMesh;

    [Header("Color Settings")]
    // color
    public Color defaultColor = Color.white;

    [Header("Irregularity Settings")]
    public Texture2D noiseSource;


    private void OnEnable()
    {
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.solidFactor = innerHexProportion;
    }

    void Awake()
    {
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.solidFactor = innerHexProportion;
        gridCanvas = GetComponentInChildren<Canvas>();
        hexMesh = GetComponentInChildren<HexMesh>();
        cells = new HexCell[height * width];

        for (int z = 0, i = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    private void Start()
    {
        hexMesh.Triangulate(cells);
    }

    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2;
        return cells[index];
    }

    public void Refresh()
    {
        hexMesh.Triangulate(cells);
    }

    void CreateCell(int x, int z, int i)
    {
        Vector3 position;
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
        position.y = 0f;
        position.z = z * (HexMetrics.outerRadius * 1.5f);

        HexCell cell = cells[i] = Instantiate(cellPrefab);
        cell.transform.SetParent(transform, false);
        cell.transform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.color = defaultColor;

        // get neighbors
        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }
        if (z > 0)
        {
            if ((z & 1) == 0) // this returns true if z is even
            {
                cell.SetNeighbor(HexDirection.SE, cells[i - width]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - width - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - width]);
                if (x < width - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - width + 1]);
                }
            }
        }

        Text label = Instantiate<Text>(cellLabelPrefab);
        label.rectTransform.SetParent(gridCanvas.transform, false);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        label.text = cell.coordinates.ToStringOnSeparateLines();

        cell.uiRect = label.rectTransform;

        if (usePerlin) cell.Elevation = (int)(Mathf.PerlinNoise(x * .3f, z * .3f) * perlinMultiplier);
        else cell.Elevation = 0;
    }

}
