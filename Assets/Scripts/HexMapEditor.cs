using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{
    public Color[] colors;

    public HexGrid hexGrid;

    bool editOn;
    public Toggle editOnToggle;
    private Color activeColor;
    int activeElevation;

    private HexCell selectedCell;


    void Awake()
    {
        SelectColor(0);
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
            if (editOn) EditCell(hexGrid.GetCell(hit.point));
            else selectedCell = hexGrid.GetCell(hit.point);
        }
    }

    void EditCell(HexCell cell)
    {
        cell.color = activeColor;
        cell.Elevation = activeElevation;
        hexGrid.Refresh();
    }

    void EditElevation(HexCell cell, bool adjustUp)
    {
        if (adjustUp) cell.Elevation = cell.Elevation + 1;
        else cell.Elevation = cell.Elevation - 1;
        hexGrid.Refresh();
    }

    public void SetEditOn(bool set)
    {
        editOn = set;
        Camera.main.GetComponent<CameraController>().movementOn = !set;
    }

    public void SelectColor(int index)
    {
        activeColor = colors[index];
    }

    public void SetElevation(float elevation)
    {
        activeElevation = (int)elevation;
    }
}
