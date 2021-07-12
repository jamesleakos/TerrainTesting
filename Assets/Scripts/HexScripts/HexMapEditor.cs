using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;

public class HexMapEditor : MonoBehaviour
{
    #region Vars

    // start vars
    public HexGrid hexGrid;

    // editing info vars
    #region Editing Info

    bool editOn;
    public Toggle editOnToggle;

    int brushSize;
    private HexCell selectedCell;

    #endregion

    // elevation vars
    #region Elevation

    // Elevation
    bool applyElevation;
    int activeElevation;

    #endregion

    // terrain type vars
    #region Terrain Type

    int activeTerrainTypeIndex;

    #endregion

    // optional toggle vars
    #region Optional Toggles

    enum OptionalToggle
    {
        Ignore, Yes, No
    }
    OptionalToggle riverMode, roadMode, walledMode;

    // dragging for rivers
    bool isDrag;
    HexDirection dragDirection;
    HexCell previousCell;

    #endregion

    // water vars
    #region Water

    int activeWaterLevel;
    bool applyWaterLevel = true;

    #endregion

    // walls vars
    #region Walls

    public void SetWalledMode(int mode)
    {
        walledMode = (OptionalToggle)mode;
    }

    #endregion

    // features vars
    #region Features and Special Features

    int activeUrbanLevel, activeFarmLevel, activePlantLevel, activeSpecialIndex;
    bool applyUrbanLevel, applyFarmLevel, applyPlantLevel, applySpecialIndex;



    #endregion

    // end variables
    #endregion

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

    #region Input

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

    #endregion

    #region Edit Cells and Generic Editor Functions

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
            if(activeTerrainTypeIndex >= 0)
            {
                cell.TerrainTypeIndex = activeTerrainTypeIndex;
            }
            if (applyElevation)
            {
                cell.Elevation = activeElevation;
            }
            if (applyWaterLevel)
            {
                cell.WaterLevel = activeWaterLevel;
            }
            if (applySpecialIndex)
            {
                cell.SpecialIndex = activeSpecialIndex;
            }
            if (applyUrbanLevel)
            {
                cell.UrbanLevel = activeUrbanLevel;
            }
            if (applyFarmLevel)
            {
                cell.FarmLevel = activeFarmLevel;
            }
            if (applyPlantLevel)
            {
                cell.PlantLevel = activePlantLevel;
            }
            if (riverMode == OptionalToggle.No)
            {
                cell.RemoveRiver();
            }
            if (roadMode == OptionalToggle.No)
            {
                cell.RemoveRoads();
            }
            if (walledMode != OptionalToggle.Ignore)
            {
                cell.Walled = walledMode == OptionalToggle.Yes;
            }
            if (isDrag)
            {
                HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
                if (otherCell)
                {
                    if (riverMode == OptionalToggle.Yes)
                    {
                        otherCell.SetOutgoingRiver(dragDirection);
                    }
                    if (roadMode == OptionalToggle.Yes)
                    {
                        otherCell.AddRoad(dragDirection);
                    }
                }
            }
        }
    }

    public void SetEditOn(bool set)
    {
        editOn = set;
        Camera.main.GetComponent<CameraController>().movementOn = !set;
    }

    public void SetBrushSize(float size)
    {
        brushSize = (int)size;
    }

    public void ShowUI(bool visible)
    {
        hexGrid.ShowUI(visible);
    }

    #endregion

    #region Elevation

    void EditElevation(HexCell cell, bool adjustUp)
    {
        if (adjustUp) cell.Elevation = cell.Elevation + 1;
        else cell.Elevation = cell.Elevation - 1;
    }

    public void SetElevation(float elevation)
    {
        activeElevation = (int)elevation;
    }

    public void SetApplyElevation(bool toggle)
    {
        applyElevation = toggle;
    }

    #endregion

    #region TerrainType

    public void SetTerrainTypeIndex(int index)
    {
        activeTerrainTypeIndex = index;
    }

    #endregion

    #region River

    public void SetRiverMode(int mode)
    {
        riverMode = (OptionalToggle)mode;
    }

    public void SetRoadMode(int mode)
    {
        roadMode = (OptionalToggle)mode;
    }

    #endregion

    #region Water Level

    public void SetApplyWaterLevel(bool toggle)
    {
        applyWaterLevel = toggle;
    }

    public void SetWaterLevel(float level)
    {
        activeWaterLevel = (int)level;
    }

    #endregion

    #region Features

    public void SetApplyUrbanLevel(bool toggle)
    {
        applyUrbanLevel = toggle;
    }

    public void SetUrbanLevel(float level)
    {
        activeUrbanLevel = (int)level;
    }

    public void SetApplyFarmLevel(bool toggle)
    {
        applyFarmLevel = toggle;
    }

    public void SetFarmLevel(float level)
    {
        activeFarmLevel = (int)level;
    }

    public void SetApplyPlantLevel(bool toggle)
    {
        applyPlantLevel = toggle;
    }

    public void SetPlantLevel(float level)
    {
        activePlantLevel = (int)level;
    }

    #endregion

    #region Special Features

    public void SetApplySpecialIndex(bool toggle)
    {
        applySpecialIndex = toggle;
    }

    public void SetSpecialIndex(float index)
    {
        activeSpecialIndex = (int)index;
    }


    #endregion

}
