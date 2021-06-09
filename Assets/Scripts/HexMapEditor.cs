using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{
    public HexGrid hexGrid;

    #region Editing Info

    bool editOn;
    public Toggle editOnToggle;

    int brushSize;
    private HexCell selectedCell;
    
    #endregion

    #region Color and Elevation

    // Color
    public Color[] colors;

    bool applyColor;
    private Color activeColor;

    // Elevation
    bool applyElevation;
    int activeElevation;


    #endregion

    #region Rivers

    enum OptionalToggle
    {
        Ignore, Yes, No
    }
    OptionalToggle riverMode;

    // dragging for rivers
    bool isDrag;
    HexDirection dragDirection;
    HexCell previousCell;

    #endregion

    void Awake()
    {
        SelectColor(0);
    }

    void Update()
    {        
        if (Input.GetMouseButton(0) &&
            !EventSystem.current.IsPointerOverGameObject()
        )
        {
            HandleInput();
        }
        else
        {
            previousCell = null;
        }

        // toggling intput on
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetEditOn(!editOn);
            editOnToggle.isOn = editOn;
        }

        // editing height of selected cell
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
            if (editOn)
            {
                HexCell currentCell = hexGrid.GetCell(hit.point);
                if (previousCell && previousCell != currentCell)
                {
                    ValidateDrag(currentCell);
                }
                else
                {
                    isDrag = false;
                }
                EditCells(currentCell);
                previousCell = currentCell;
            }
            else selectedCell = hexGrid.GetCell(hit.point);
        }
        else
        {
            previousCell = null;
        }
    }

    void ValidateDrag(HexCell currentCell)
    {
        for (
            dragDirection = HexDirection.NE;
            dragDirection <= HexDirection.NW;
            dragDirection++
        )
        {
            if (previousCell.GetNeighbor(dragDirection) == currentCell)
            {
                isDrag = true;
                return;
            }
        }
        isDrag = false;
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
            if (riverMode == OptionalToggle.No)
            {
                cell.RemoveRiver();
            }
            else if (isDrag && riverMode == OptionalToggle.Yes)
            {
                HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
                if (otherCell)
                {
                    otherCell.SetOutgoingRiver(dragDirection);
                }
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

    public void SetRiverMode(int mode)
    {
        riverMode = (OptionalToggle)mode;
    }
}
