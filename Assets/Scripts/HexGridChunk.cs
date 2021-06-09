using UnityEngine;
using UnityEngine.UI;

public class HexGridChunk : MonoBehaviour
{
    HexCell[] cells;

    public HexMesh terrain;
    Canvas gridCanvas;
    [HideInInspector]
    public bool flatTerraces;
    [HideInInspector]
    public bool upwardTrisForSoloBottomTerrace;

    void Awake()
    {
        gridCanvas = GetComponentInChildren<Canvas>();

        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
        ShowUI(false);
    }

    void Start()
    {
        terrain.flatTerraces = flatTerraces;
        terrain.upwardTrisForSoloBottomTerrace = upwardTrisForSoloBottomTerrace;
        //hexMesh.Triangulate(cells);
    }

    public void AddCell(int index, HexCell cell)
    {
        cells[index] = cell;
        cell.chunk = this;
        cell.transform.SetParent(transform, false);
        cell.uiRect.SetParent(gridCanvas.transform, false);
    }

    public void Refresh()
    {
        enabled = true;
    }

    void LateUpdate()
    {
        Triangulate();
        enabled = false;
    }

    public void ShowUI(bool visible)
    {
        gridCanvas.gameObject.SetActive(visible);
    }

    #region Triangulation Starters

    public void Triangulate()
    {
        terrain.Clear();

        for (int i = 0; i < cells.Length; i++)
        {
            Triangulate(cells[i]);
        }
        terrain.Apply();
    }

    void Triangulate(HexCell cell)
    {
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            Triangulate(d, cell);
        }
    }

    void Triangulate(HexDirection direction, HexCell cell)
    {
        Vector3 center = cell.Position;
        EdgeVertices e = new EdgeVertices(
            center + HexMetrics.GetFirstSolidCorner(direction),
            center + HexMetrics.GetSecondSolidCorner(direction)
        );

        if (cell.HasRiver)
        {
            if (cell.HasRiverThroughEdge(direction))
            {
                e.v3.y = cell.StreamBedY;
                if (cell.HasRiverBeginOrEnd)
                {
                    TriangulateWithRiverBeginOrEnd(direction, cell, center, e);
                }
                else
                {
                    Debug.Log("triangulate with river");
                    TriangulateWithRiver(direction, cell, center, e);
                }
            }
            else
            {
                TriangulateAdjacentToRiver(direction, cell, center, e);
            }
        }
        else
        {
            TriangulateEdgeFan(center, e, cell.Color);
        }

        if (direction <= HexDirection.SE)
        {
            TriangulateConnection(direction, cell, e);
        }
    }

    void TriangulateWithRiver(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
    )
    {
        Vector3 centerL, centerR;
        if (cell.HasRiverThroughEdge(direction.Opposite()))
        {
            centerL = center + HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
            centerR = center + HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;
        }
        else if (cell.HasRiverThroughEdge(direction.Next()))
        {
            centerL = center;
            centerR = Vector3.Lerp(center, e.v5, 2f / 3f);
        }
        else if (cell.HasRiverThroughEdge(direction.Previous()))
        {
            centerL = Vector3.Lerp(center, e.v1, 2f / 3f);
            centerR = center;
        }
        else if (cell.HasRiverThroughEdge(direction.Next2()))
        {
            centerL = center;
            centerR = center + HexMetrics.GetSolidEdgeMiddle(direction.Next()) * (0.5f * HexMetrics.innerToOuter);

        }
        else
        {
            centerL = center + HexMetrics.GetSolidEdgeMiddle(direction.Previous()) * (0.5f * HexMetrics.innerToOuter);
            centerR = center;
        }
        center = Vector3.Lerp(centerL, centerR, 0.5f);

        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(centerL, e.v1, 0.5f),
            Vector3.Lerp(centerR, e.v5, 0.5f),
            1f / 6f
        );

        terrain.AddTriangle(centerL, m.v1, m.v2);
        terrain.AddTriangleColor(cell.Color);
        terrain.AddQuad(centerL, center, m.v2, m.v3);
        terrain.AddQuadColor(cell.Color);
        terrain.AddQuad(center, centerR, m.v3, m.v4);
        terrain.AddQuadColor(cell.Color);
        terrain.AddTriangle(centerR, m.v4, m.v5);
        terrain.AddTriangleColor(cell.Color);

        m.v3.y = center.y = e.v3.y;
        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
    }

    void TriangulateWithRiverBeginOrEnd(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
    )
    {
        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f)
        );

        m.v3.y = e.v3.y;

        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
        TriangulateEdgeFan(center, m, cell.Color);

    }

    void TriangulateAdjacentToRiver(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
    )
    {
        if (cell.HasRiverThroughEdge(direction.Next()))
        {
            if (cell.HasRiverThroughEdge(direction.Previous()))
            {
                center += HexMetrics.GetSolidEdgeMiddle(direction) *
                    (HexMetrics.innerToOuter * 0.5f);
            }
            else if (
                cell.HasRiverThroughEdge(direction.Previous2())
            )
            {
                center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
            }
        }
        else if (
           cell.HasRiverThroughEdge(direction.Previous()) &&
           cell.HasRiverThroughEdge(direction.Next2())
        )
        {
            center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
        }

        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f)
        );

        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
        TriangulateEdgeFan(center, m, cell.Color);
    }

    void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
    {
        terrain.AddTriangle(center, edge.v1, edge.v2);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(center, edge.v2, edge.v3);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(center, edge.v3, edge.v4);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(center, edge.v4, edge.v5);
        terrain.AddTriangleColor(color);
    }

    void TriangulateEdgeStrip(
        EdgeVertices e1, Color c1,
        EdgeVertices e2, Color c2
    )
    {
        terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
        terrain.AddQuadColor(c1, c2);
    }

    #endregion

    #region Triangulate Between Cells

    void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices e1)
    {
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor == null)
        {
            return;
        }

        Vector3 bridge = HexMetrics.GetBridge(direction);
        bridge.y = neighbor.Position.y - cell.Position.y;
        EdgeVertices e2 = new EdgeVertices(
            e1.v1 + bridge,
            e1.v5 + bridge
        );

        if (cell.HasRiverThroughEdge(direction))
        {
            e2.v3.y = neighbor.StreamBedY;
        }

        if (cell.GetEdgeType(direction) == HexEdgeType.Terrace)
        {
            TriangulateEdgeTerraces(e1, cell, e2, neighbor);
        }
        else
        {
            TriangulateEdgeStrip(e1, cell.Color, e2, neighbor.Color);
        }

        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (direction <= HexDirection.E && nextNeighbor != null)
        {
            Vector3 v5 = e1.v5 + HexMetrics.GetBridge(direction.Next());
            v5.y = nextNeighbor.Position.y;

            if (cell.Elevation <= neighbor.Elevation)
            {
                if (cell.Elevation <= nextNeighbor.Elevation)
                {
                    TriangulateCorner(
                        e1.v5, cell, e2.v5, neighbor, v5, nextNeighbor
                    );
                }
                else
                {
                    TriangulateCorner(
                        v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor
                    );
                }
            }
            else if (neighbor.Elevation <= nextNeighbor.Elevation)
            {
                TriangulateCorner(
                    e2.v5, neighbor, v5, nextNeighbor, e1.v5, cell
                );
            }
            else
            {
                TriangulateCorner(
                    v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor
                );
            }
        }
    }

    void TriangulateEdgeTerraces(
        EdgeVertices begin, HexCell beginCell,
        EdgeVertices end, HexCell endCell
    )
    {
        EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, 1);

        TriangulateEdgeStrip(begin, beginCell.Color, e2, c2);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            EdgeVertices e1 = e2;
            Color c1 = c2;
            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, i);
            TriangulateEdgeStrip(e1, c1, e2, c2);
        }

        TriangulateEdgeStrip(e2, c2, end, endCell.Color);
    }

    #endregion

    #region Triangulate Corner

    void TriangulateCorner(
    Vector3 bottom, HexCell bottomCell,
    Vector3 left, HexCell leftCell,
    Vector3 right, HexCell rightCell
)
    {
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

        if (leftEdgeType == HexEdgeType.Terrace)
        {
            if (rightEdgeType == HexEdgeType.Terrace)
            {
                TriangulateCornerTerraces(
                    bottom, bottomCell, left, leftCell, right, rightCell
                );
            }
            else if (rightEdgeType == HexEdgeType.Flat)
            {
                TriangulateCornerTerraces(
                    left, leftCell, right, rightCell, bottom, bottomCell
                );
            }
            else
            {
                TriangulateCornerTerracesCliff(
                    bottom, bottomCell, left, leftCell, right, rightCell
                );
            }
        }
        else if (rightEdgeType == HexEdgeType.Terrace)
        {
            if (leftEdgeType == HexEdgeType.Flat)
            {
                TriangulateCornerTerraces(
                    right, rightCell, bottom, bottomCell, left, leftCell
                );
            }
            else
            {
                TriangulateCornerCliffTerraces(
                    bottom, bottomCell, left, leftCell, right, rightCell
                );
            }
        }
        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Terrace)
        {
            if (leftCell.Elevation < rightCell.Elevation)
            {
                TriangulateCornerCliffTerraces(
                    right, rightCell, bottom, bottomCell, left, leftCell
                );
            }
            else
            {
                TriangulateCornerTerracesCliff(
                    left, leftCell, right, rightCell, bottom, bottomCell
                );
            }
        }
        else
        {
            terrain.AddTriangle(bottom, left, right);
            terrain.AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
        }
    }

    void TriangulateCornerTerracesCliff(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    )
    {
        // my custom upward tri code
        if (upwardTrisForSoloBottomTerrace && leftCell.GetEdgeType(rightCell) != HexEdgeType.Terrace)
        {
            Vector3 v2 = HexMetrics.TerraceLerp(begin, left, 1);
            Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);

            terrain.AddTriangle(begin, v2, right);
            terrain.AddTriangleColor(beginCell.Color, c2, rightCell.Color);

            for (int i = 1; i <= HexMetrics.TerraceSteps; i++)
            {
                Vector3 v1 = v2;
                Color c1 = c2;
                v2 = HexMetrics.TerraceLerp(begin, left, i);
                c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
                terrain.AddTriangle(v1, v2, right);
                terrain.AddTriangleColor(c1, c2, rightCell.Color);
            }
            return;
        }

        // CC code
        float b = 1f / (rightCell.Elevation - beginCell.Elevation);
        if (b < 0)
        {
            b = -b;
        }
        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);
        Color boundaryColor = Color.Lerp(beginCell.Color, rightCell.Color, b);

        // Triangulate Boundary Triangle Code Starts Here...
        if (flatTerraces)
        {
            TriangulateBoundaryTriangleFlatTerracesTerraceOnLeft(
                begin, beginCell, left, leftCell, boundary, boundaryColor
            );
        }
        else
        {
            TriangulateBoundaryTriangleMeetAtPoint(
                begin, beginCell, left, leftCell, boundary, boundaryColor
            );
        }
        // ... and ends here

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Terrace)
        {
            // Triangulate Boundary Triangle Code Starts Here...
            if (flatTerraces)
            {
                TriangulateBoundaryTriangleFlatTerracesTerraceOnRight(
                    right, rightCell, left, leftCell, boundary, boundaryColor
                );
            }
            else
            {
                TriangulateBoundaryTriangleMeetAtPoint(
                    left, leftCell, right, rightCell, boundary, boundaryColor
                );
            }
            // ... and ends here
        }
        else
        {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    void TriangulateCornerCliffTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    )
    {
        // my custom upward tri code
        if (upwardTrisForSoloBottomTerrace && rightCell.GetEdgeType(leftCell) != HexEdgeType.Terrace)
        {
            Vector3 v2 = HexMetrics.TerraceLerp(begin, right, 1);
            Color c2 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, 1);

            terrain.AddTriangle(begin, left, v2);
            terrain.AddTriangleColor(beginCell.Color, leftCell.Color, c2);

            for (int i = 1; i <= HexMetrics.TerraceSteps; i++)
            {
                Vector3 v1 = v2;
                Color c1 = c2;
                v2 = HexMetrics.TerraceLerp(begin, right, i);
                c2 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);
                terrain.AddTriangle(v1, left, v2);
                terrain.AddTriangleColor(c1, leftCell.Color, c2);
            }
            return;
        }

        float b = 1f / (leftCell.Elevation - beginCell.Elevation);
        if (b < 0)
        {
            b = -b;
        }
        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);
        Color boundaryColor = Color.Lerp(beginCell.Color, leftCell.Color, b);

        // Triangulate Boundary Triangle Code Starts Here...
        if (flatTerraces)
        {
            TriangulateBoundaryTriangleFlatTerracesTerraceOnLeft(
                right, rightCell, begin, beginCell, boundary, boundaryColor
            );
        }
        else
        {
            TriangulateBoundaryTriangleMeetAtPoint(
                right, rightCell, begin, beginCell, boundary, boundaryColor
            );
        }
        // ... and ends here

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Terrace)
        {
            // Triangulate Boundary Triangle Code Starts Here...
            if (flatTerraces)
            {
                TriangulateBoundaryTriangleFlatTerracesTerraceOnLeft(
                    left, leftCell, right, rightCell, boundary, boundaryColor
                );
            }
            else
            {
                TriangulateBoundaryTriangleMeetAtPoint(
                    left, leftCell, right, rightCell, boundary, boundaryColor
                );
            }
            // ... and ends here
        }
        else
        {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    void TriangulateBoundaryTriangleFlatTerracesTerraceOnRight(
        Vector3 begin, HexCell beginCell,
        Vector3 right, HexCell rightCell,
        Vector3 boundary, Color boundaryColor
    )
    {
        Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, right, 1));
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, 1);

        // calcuate some heights for lerping with boundary
        float pointHeight = v2.y - HexMetrics.Perturb(begin).y;
        float boundaryHeight = boundary.y - HexMetrics.Perturb(begin).y;
        var ratio = pointHeight / boundaryHeight;
        var wallPoint = Vector3.Lerp(HexMetrics.Perturb(begin), boundary, ratio);

        // add first tri
        terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), wallPoint, v2);
        terrain.AddTriangleColor(beginCell.Color, boundaryColor, c2);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, right, i));
            c2 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);

            pointHeight = v2.y - HexMetrics.Perturb(begin).y;
            ratio = pointHeight / boundaryHeight;

            if (i % 2 == 0)
            {
                terrain.AddTriangleUnperturbed(v1, wallPoint, v2);
                terrain.AddTriangleColor(c1, boundaryColor, c2);
            }
            else
            {
                var oldPoint = wallPoint;
                wallPoint = Vector3.Lerp(HexMetrics.Perturb(begin), boundary, ratio);
                terrain.AddQuadUnperturbed(oldPoint, v1, wallPoint, v2);
                terrain.AddQuadColor(boundaryColor, c1, boundaryColor, c2);
            }
        }

        terrain.AddQuadUnperturbed(wallPoint, v2, boundary, HexMetrics.Perturb(right));
        terrain.AddQuadColor(boundaryColor, c2, boundaryColor, rightCell.Color);
    }

    void TriangulateBoundaryTriangleFlatTerracesTerraceOnLeft(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 boundary, Color boundaryColor
    )
    {
        Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);

        // calcuate some heights for lerping with boundary
        float pointHeight = v2.y - HexMetrics.Perturb(begin).y;
        float boundaryHeight = boundary.y - HexMetrics.Perturb(begin).y;
        var ratio = pointHeight / boundaryHeight;
        var wallPoint = Vector3.Lerp(HexMetrics.Perturb(begin), boundary, ratio);

        // add first tri
        terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, wallPoint);
        terrain.AddTriangleColor(beginCell.Color, c2, boundaryColor);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);

            pointHeight = v2.y - HexMetrics.Perturb(begin).y;
            ratio = pointHeight / boundaryHeight;

            if (i % 2 == 0)
            {
                terrain.AddTriangleUnperturbed(v1, v2, wallPoint);
                terrain.AddTriangleColor(c1, c2, boundaryColor);
            }
            else
            {
                var oldPoint = wallPoint;
                wallPoint = Vector3.Lerp(HexMetrics.Perturb(begin), boundary, ratio);
                terrain.AddQuadUnperturbed(v1, oldPoint, v2, wallPoint);
                terrain.AddQuadColor(c1, boundaryColor, c2, boundaryColor);
            }

        }

        terrain.AddQuadUnperturbed(v2, wallPoint, HexMetrics.Perturb(left), boundary);
        terrain.AddQuadColor(c2, boundaryColor, leftCell.Color, boundaryColor);
    }

    void TriangulateBoundaryTriangleMeetAtPoint(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 boundary, Color boundaryColor
    )
    {
        Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);

        terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
        terrain.AddTriangleColor(beginCell.Color, c2, boundaryColor);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            terrain.AddTriangleUnperturbed(v1, v2, boundary);
            terrain.AddTriangleColor(c1, c2, boundaryColor);
        }

        terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
        terrain.AddTriangleColor(c2, leftCell.Color, boundaryColor);
    }

    void TriangulateCornerTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    )
    {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);
        Color c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, 1);

        terrain.AddTriangle(begin, v3, v4);
        terrain.AddTriangleColor(beginCell.Color, c3, c4);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c3;
            Color c2 = c4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);
            terrain.AddQuad(v1, v2, v3, v4);
            terrain.AddQuadColor(c1, c2, c3, c4);
        }

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Terrace)
        {
            if (leftCell.Elevation > rightCell.Elevation)
            {
                terrain.AddQuad(v3, v4, HexMetrics.TerraceLerp(right, left, HexMetrics.TerraceSteps - 1), right);
                terrain.AddQuadColor(c3, c4, c3, c4);

                terrain.AddTriangle(v3, left, HexMetrics.TerraceLerp(right, left, HexMetrics.TerraceSteps - 1));
                terrain.AddTriangleColor(c3, leftCell.Color, HexMetrics.TerraceLerp(rightCell.Color, leftCell.Color, HexMetrics.TerraceSteps - 1));

                for (int i = 0; i < HexMetrics.TerraceSteps - 2; i += 2)
                {
                    terrain.AddTriangle(
                        HexMetrics.TerraceLerp(right, left, i),
                        HexMetrics.TerraceLerp(right, left, i + 2),
                        HexMetrics.TerraceLerp(right, left, i + 1)
                    );
                    terrain.AddTriangleColor(
                        HexMetrics.TerraceLerp(rightCell.Color, leftCell.Color, i),
                        HexMetrics.TerraceLerp(rightCell.Color, leftCell.Color, i + 2),
                        HexMetrics.TerraceLerp(rightCell.Color, leftCell.Color, i + 1)
                    );
                }

                // fill gaps between smoothed steps and quad
                if (HexMetrics.terracesPerSlope <= 1) return;

                // add first tri
                terrain.AddTriangleUnperturbed(
                    HexMetrics.Perturb(right),
                    Vector3.Lerp(HexMetrics.Perturb(right), HexMetrics.Perturb(HexMetrics.TerraceLerp(right, left, HexMetrics.TerraceSteps - 1)), (float)2 / (float)(HexMetrics.TerraceSteps - 1)),
                    HexMetrics.Perturb(HexMetrics.TerraceLerp(right, left, 2))
                );
                terrain.AddTriangleColor(
                    leftCell.Color,
                    HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, 2),
                    HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, 2)
                );
                // add last tri
                terrain.AddTriangleUnperturbed(
                    HexMetrics.Perturb(HexMetrics.TerraceLerp(right, left, HexMetrics.TerraceSteps - 3)),
                    Vector3.Lerp(HexMetrics.Perturb(right), HexMetrics.Perturb(HexMetrics.TerraceLerp(right, left, HexMetrics.TerraceSteps - 1)), (float)(HexMetrics.TerraceSteps - 3) / (float)(HexMetrics.TerraceSteps - 1)),
                    HexMetrics.Perturb(HexMetrics.TerraceLerp(right, left, HexMetrics.TerraceSteps - 1))

                );
                terrain.AddTriangleColor(
                    HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, HexMetrics.TerraceSteps - 3),
                    HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, HexMetrics.TerraceSteps - 1),
                    HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, HexMetrics.TerraceSteps - 1)
                );

                // fill quads in situations where there are more steps than 2
                if (HexMetrics.terracesPerSlope <= 2) return;

                for (int i = 2; i < HexMetrics.TerraceSteps - 3; i += 2)
                {
                    terrain.AddQuadUnperturbed(
                        Vector3.Lerp(HexMetrics.Perturb(right), HexMetrics.Perturb(HexMetrics.TerraceLerp(right, left, HexMetrics.TerraceSteps - 1)), (float)(i + 2) / (float)(HexMetrics.TerraceSteps - 1)),
                        Vector3.Lerp(HexMetrics.Perturb(right), HexMetrics.Perturb(HexMetrics.TerraceLerp(right, left, HexMetrics.TerraceSteps - 1)), (float)(i) / (float)(HexMetrics.TerraceSteps - 1)),
                        HexMetrics.Perturb(HexMetrics.TerraceLerp(right, left, i + 2)),
                        HexMetrics.Perturb(HexMetrics.TerraceLerp(right, left, i))

                    );
                    terrain.AddQuadColor(
                        HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, i),
                        HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, i + 2),
                        HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, i),
                        HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, i + 2)
                    );
                }
            }
            else
            {
                // add quad to get to steps
                terrain.AddQuad(v3, v4, left, HexMetrics.TerraceLerp(left, right, HexMetrics.TerraceSteps - 1));
                terrain.AddQuadColor(c3, c4, c3, c4);

                // add large final triangle
                terrain.AddTriangle(v4, HexMetrics.TerraceLerp(left, right, HexMetrics.TerraceSteps - 1), right);
                terrain.AddTriangleColor(c4, HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, HexMetrics.TerraceSteps - 1), rightCell.Color);

                // fill in step gaps between steps
                for (int i = 0; i < HexMetrics.TerraceSteps - 2; i += 2)
                {
                    terrain.AddTriangle(
                        HexMetrics.TerraceLerp(left, right, i),
                        HexMetrics.TerraceLerp(left, right, i + 1),
                        HexMetrics.TerraceLerp(left, right, i + 2)
                    );
                    terrain.AddTriangleColor(
                        HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, i),
                        HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, i + 1),
                        HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, i + 2)
                    );
                }

                // fill gaps between smoothed steps and quad
                if (HexMetrics.terracesPerSlope <= 1) return;

                // add first tri
                terrain.AddTriangleUnperturbed(
                    HexMetrics.Perturb(left),
                    HexMetrics.Perturb(HexMetrics.TerraceLerp(left, right, 2)),
                    Vector3.Lerp(HexMetrics.Perturb(left), HexMetrics.Perturb(HexMetrics.TerraceLerp(left, right, HexMetrics.TerraceSteps - 1)), (float)2 / (float)(HexMetrics.TerraceSteps - 1))
                );
                terrain.AddTriangleColor(
                    leftCell.Color,
                    HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, 2),
                    HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, 2)
                );
                // add last tri
                terrain.AddTriangleUnperturbed(
                    HexMetrics.Perturb(HexMetrics.TerraceLerp(left, right, HexMetrics.TerraceSteps - 3)),
                    HexMetrics.Perturb(HexMetrics.TerraceLerp(left, right, HexMetrics.TerraceSteps - 1)),
                    Vector3.Lerp(HexMetrics.Perturb(left), HexMetrics.Perturb(HexMetrics.TerraceLerp(left, right, HexMetrics.TerraceSteps - 1)), (float)(HexMetrics.TerraceSteps - 3) / (float)(HexMetrics.TerraceSteps - 1))
                );
                terrain.AddTriangleColor(
                    HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, HexMetrics.TerraceSteps - 3),
                    HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, HexMetrics.TerraceSteps - 1),
                    HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, HexMetrics.TerraceSteps - 1)
                );

                // fill quads in situations where there are more steps than 2
                if (HexMetrics.terracesPerSlope <= 2) return;

                for (int i = 2; i < HexMetrics.TerraceSteps - 3; i += 2)
                {
                    terrain.AddQuadUnperturbed(
                        Vector3.Lerp(HexMetrics.Perturb(left), HexMetrics.Perturb(HexMetrics.TerraceLerp(left, right, HexMetrics.TerraceSteps - 1)), (float)(i) / (float)(HexMetrics.TerraceSteps - 1)),
                        Vector3.Lerp(HexMetrics.Perturb(left), HexMetrics.Perturb(HexMetrics.TerraceLerp(left, right, HexMetrics.TerraceSteps - 1)), (float)(i + 2) / (float)(HexMetrics.TerraceSteps - 1)),
                        HexMetrics.Perturb(HexMetrics.TerraceLerp(left, right, i)),
                        HexMetrics.Perturb(HexMetrics.TerraceLerp(left, right, i + 2))

                    );
                    terrain.AddQuadColor(
                        HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, i),
                        HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, i + 2),
                        HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, i),
                        HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, i + 2)
                    );
                }
            }
        }
        else
        {
            terrain.AddQuad(v3, v4, left, right);
            terrain.AddQuadColor(c3, c4, leftCell.Color, rightCell.Color);
        }
    }

    #endregion
}
