using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{
    public Color[] colors;

    public HexGrid hexGrid;

    bool editOn;
    public Toggle editOnToggle;

    bool applyColor;
    bool applyElevation;

    private Color activeColor;
    int activeElevation;

    private HexCell selectedCell;

    int brushSize;


    void Awake()
    {
        SelectColor(0);
    }

    private void Start()
    {
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetEditOn(!editOn);
            editOnToggle.isOn = editOn;
        }
        if (Input.GetMouseButton(0) &&
            !EventSystem.current.IsPointerOverGameObject()
        )
        {
            HandleInput();
        } 
        if (Input.GetKeyDown(KeyCode.UpArrow)) {
            if (selectedCell == null) return;
            EditElevation(selectedCell, true);
        }
        if (Input.GetKeyDown(KeyCode.DownArrow)) {
            if (selectedCell == null) return;
            EditElevation(selectedCell, false);
        }
    }

    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
        {
            if (editOn) EditCells(hexGrid.GetCell(hit.point));
            else selectedCell = hexGrid.GetCell(hit.point);
        }
    }

    void EditCells(HexCell center)
    {
        int centerX = center.coordinates.X;
        int centerZ = center.coordinates.Z;

        for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++)
        {
            for (int x = centerX - r; x <= centerX + brushSize; x++)
            {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
        for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++)
        {
            for (int x = centerX - brushSize; x <= centerX + r; x++)
            {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
    }

    void EditCell(HexCell cell)
    {
        if (cell)
        {
            if (applyColor)
            {
                cell.Color = activeColor;
            }
            if (applyElevation)
            {
                cell.Elevation = activeElevation;
            }
        }        
    }

    void EditElevation(HexCell cell, bool adjustUp)
    {
        if (adjustUp) cell.Elevation = cell.Elevation + 1;
        else cell.Elevation = cell.Elevation - 1;
    }

    public void SetEditOn(bool set)
    {
        editOn = set;
        Camera.main.GetComponent<CameraController>().movementOn = !set;
    }

    public void SelectColor(int index)
    {
        applyColor = index >= 0;
        if (applyColor)
        {
            activeColor = colors[index];
        }
    }

    public void SetElevation(float elevation)
    {
        activeElevation = (int)elevation;
    }

    public void SetApplyElevation(bool toggle)
    {
        applyElevation = toggle;
    }

    public void SetBrushSize(float size)
    {
        brushSize = (int)size;
    }

    public void ShowUI(bool visible)
    {
        hexGrid.ShowUI(visible);
    }
}
