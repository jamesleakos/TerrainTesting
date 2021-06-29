﻿using UnityEngine;
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

    #region Water

    int activeWaterLevel;
    bool applyWaterLevel = true;

    #endregion

    #region Walls

    public void SetWalledMode(int mode)
    {
        walledMode = (OptionalToggle)mode;
    }

    #endregion

    #region Features

    int activeUrbanLevel, activeFarmLevel, activePlantLevel;
    bool applyUrbanLevel, applyFarmLevel, applyPlantLevel;

    #endregion

    // end variables

    void Awake()
    {
        SelectColor(-1);
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
            if (applyColor)
            {
                cell.Color = activeColor;
            }
            if (applyElevation)
            {
                cell.Elevation = activeElevation;
            }
            if (applyWaterLevel)
            {
                cell.WaterLevel = activeWaterLevel;
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

    #region Color

    public void SelectColor(int index)
    {
        applyColor = index >= 0;
        if (applyColor)
        {
            activeColor = colors[index];
        }
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

}
