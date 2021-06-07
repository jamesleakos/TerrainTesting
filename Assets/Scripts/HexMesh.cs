using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    #region Variables

    Mesh hexMesh;

    // The switch to static is interesting - they act as a buffer, and we do one mesh at a time - even though there are many meshes
    static List<Vector3> vertices = new List<Vector3>();
    static List<Color> colors = new List<Color>();
    static List<int> triangles = new List<int>();

    MeshCollider meshCollider;

    public bool flatTerraces;
    public bool upwardTrisForSoloBottomTerrace;

    #endregion

    void Awake()
    {
        GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        hexMesh.name = "Hex Mesh";
        //vertices = new List<Vector3>();
        //triangles = new List<int>();
        //colors = new List<Color>();
    }

    #region Triangulation Starters

    public void Triangulate(HexCell[] cells)
    {
        hexMesh.Clear();
        vertices.Clear();
        triangles.Clear();
        colors.Clear();

        for (int i = 0; i < cells.Length; i++)
        {
            Triangulate(cells[i]);
        }
        hexMesh.vertices = vertices.ToArray();
        hexMesh.triangles = triangles.ToArray();
        hexMesh.colors = colors.ToArray();

        hexMesh.RecalculateNormals();

        meshCollider.sharedMesh = hexMesh;
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

        TriangulateEdgeFan(center, e, cell.Color);

        if (direction <= HexDirection.SE)
        {
            TriangulateConnection(direction, cell, e);
        }
    }

    #endregion

    #region Triangulate Between Cells

    void TriangulateConnection( HexDirection direction, HexCell cell, EdgeVertices e1 )
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
            e1.v4 + bridge
        );

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
            Vector3 v5 = e1.v4 + HexMetrics.GetBridge(direction.Next());
            v5.y = nextNeighbor.Position.y;

            if (cell.Elevation <= neighbor.Elevation)
            {
                if (cell.Elevation <= nextNeighbor.Elevation)
                {
                    TriangulateCorner(
                        e1.v4, cell, e2.v4, neighbor, v5, nextNeighbor
                    );
                }
                else
                {
                    TriangulateCorner(
                        v5, nextNeighbor, e1.v4, cell, e2.v4, neighbor
                    );
                }
            }
            else if (neighbor.Elevation <= nextNeighbor.Elevation)
            {
                TriangulateCorner(
                    e2.v4, neighbor, v5, nextNeighbor, e1.v4, cell
                );
            }
            else
            {
                TriangulateCorner(
                    v5, nextNeighbor, e1.v4, cell, e2.v4, neighbor
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
            AddTriangle(bottom, left, right);
            AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
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

            AddTriangle(begin, v2, right);
            AddTriangleColor(beginCell.Color, c2, rightCell.Color);

            for (int i = 1; i <= HexMetrics.TerraceSteps; i++)
            {
                Vector3 v1 = v2;
                Color c1 = c2;
                v2 = HexMetrics.TerraceLerp(begin, left, i);
                c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
                AddTriangle(v1, v2, right);
                AddTriangleColor(c1, c2, rightCell.Color);
            }
            return;
        }

        // CC code
        float b = 1f / (rightCell.Elevation - beginCell.Elevation);
        if (b < 0)
        {
            b = -b;
        }
        Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(right), b);
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
            AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
            AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
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

            AddTriangle(begin, left, v2);
            AddTriangleColor(beginCell.Color, leftCell.Color, c2);

            for (int i = 1; i <= HexMetrics.TerraceSteps; i++)
            {
                Vector3 v1 = v2;
                Color c1 = c2;
                v2 = HexMetrics.TerraceLerp(begin, right, i);
                c2 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);
                AddTriangle(v1, left, v2);
                AddTriangleColor(c1, leftCell.Color, c2);
            }
            return;
        }

        float b = 1f / (leftCell.Elevation - beginCell.Elevation);
        if (b < 0)
        {
            b = -b;
        }
        Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(left), b);
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
            AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
            AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    void TriangulateBoundaryTriangleFlatTerracesTerraceOnRight (
        Vector3 begin, HexCell beginCell,
        Vector3 right, HexCell rightCell,
        Vector3 boundary, Color boundaryColor
    )
    {
        Vector3 v2 = Perturb(HexMetrics.TerraceLerp(begin, right, 1));
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, 1);

        // calcuate some heights for lerping with boundary
        float pointHeight = v2.y - Perturb(begin).y;
        float boundaryHeight = boundary.y - Perturb(begin).y;
        var ratio = pointHeight / boundaryHeight;
        var wallPoint = Vector3.Lerp(Perturb(begin), boundary, ratio);

        // add first tri
        AddTriangleUnperturbed(Perturb(begin), wallPoint, v2);
        AddTriangleColor(beginCell.Color, boundaryColor, c2);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = Perturb(HexMetrics.TerraceLerp(begin, right, i));
            c2 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);

            pointHeight = v2.y - Perturb(begin).y;
            ratio = pointHeight / boundaryHeight;

            if (i%2 == 0)
            {
                AddTriangleUnperturbed(v1, wallPoint, v2);
                AddTriangleColor(c1, boundaryColor, c2);
            } else
            {
                var oldPoint = wallPoint;
                wallPoint = Vector3.Lerp(Perturb(begin), boundary, ratio);
                AddQuadUnperturbed(oldPoint, v1, wallPoint, v2);
                AddQuadColor(boundaryColor, c1, boundaryColor, c2);
            }            
        }

        AddQuadUnperturbed(wallPoint, v2, boundary, Perturb(right));
        AddQuadColor(boundaryColor, c2, boundaryColor, rightCell.Color);
    }

    void TriangulateBoundaryTriangleFlatTerracesTerraceOnLeft(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 boundary, Color boundaryColor
    )
    {
        Vector3 v2 = Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);

        // calcuate some heights for lerping with boundary
        float pointHeight = v2.y - Perturb(begin).y;
        float boundaryHeight = boundary.y - Perturb(begin).y;
        var ratio = pointHeight / boundaryHeight;
        var wallPoint = Vector3.Lerp(Perturb(begin), boundary, ratio);

        // add first tri
        AddTriangleUnperturbed(Perturb(begin), v2, wallPoint);
        AddTriangleColor(beginCell.Color, c2, boundaryColor);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);

            pointHeight = v2.y - Perturb(begin).y;
            ratio = pointHeight / boundaryHeight;

            if (i % 2 == 0)
            {
                AddTriangleUnperturbed(v1, v2, wallPoint);
                AddTriangleColor(c1, c2, boundaryColor);
            }
            else
            {
                var oldPoint = wallPoint;
                wallPoint = Vector3.Lerp(Perturb(begin), boundary, ratio);
                AddQuadUnperturbed(v1, oldPoint, v2, wallPoint);
                AddQuadColor(c1, boundaryColor, c2, boundaryColor);
            }

        }

        AddQuadUnperturbed(v2, wallPoint, Perturb(left), boundary);
        AddQuadColor(c2, boundaryColor, leftCell.Color, boundaryColor);
    }

    void TriangulateBoundaryTriangleMeetAtPoint(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 boundary, Color boundaryColor
    )
    {
        Vector3 v2 = Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);

        AddTriangleUnperturbed(Perturb(begin), v2, boundary);
        AddTriangleColor(beginCell.Color, c2, boundaryColor);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            AddTriangleUnperturbed(v1, v2, boundary);
            AddTriangleColor(c1, c2, boundaryColor);
        }

        AddTriangleUnperturbed(v2, Perturb(left), boundary);
        AddTriangleColor(c2, leftCell.Color, boundaryColor);
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

        AddTriangle(begin, v3, v4);
        AddTriangleColor(beginCell.Color, c3, c4);

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
            AddQuad(v1, v2, v3, v4);
            AddQuadColor(c1, c2, c3, c4);
        }

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Terrace)
        {
            if (leftCell.Elevation > rightCell.Elevation)
            {
                AddQuad(v3, v4, HexMetrics.TerraceLerp(right, left, HexMetrics.TerraceSteps - 1), right);
                AddQuadColor(c3, c4, c3, c4);

                AddTriangle(v3, left, HexMetrics.TerraceLerp(right, left, HexMetrics.TerraceSteps - 1));
                AddTriangleColor(c3, leftCell.Color, HexMetrics.TerraceLerp(rightCell.Color, leftCell.Color, HexMetrics.TerraceSteps - 1));

                for (int i = 0; i < HexMetrics.TerraceSteps - 2; i += 2)
                {
                    AddTriangle(
                        HexMetrics.TerraceLerp(right, left, i),
                        HexMetrics.TerraceLerp(right, left, i + 2),
                        HexMetrics.TerraceLerp(right, left, i + 1)
                    );
                    AddTriangleColor(
                        HexMetrics.TerraceLerp(rightCell.Color, leftCell.Color, i),
                        HexMetrics.TerraceLerp(rightCell.Color, leftCell.Color, i + 2),
                        HexMetrics.TerraceLerp(rightCell.Color, leftCell.Color, i + 1)
                    );
                }

                // fill gaps between smoothed steps and quad
                if (HexMetrics.terracesPerSlope <= 1) return;

                // add first tri
                AddTriangleUnperturbed(
                    Perturb(right),
                    Vector3.Lerp(Perturb(right), Perturb(HexMetrics.TerraceLerp(right, left, HexMetrics.TerraceSteps - 1)), (float)2 / (float)(HexMetrics.TerraceSteps - 1)),
                    Perturb(HexMetrics.TerraceLerp(right, left, 2))                
                );
                AddTriangleColor(
                    leftCell.Color,
                    HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, 2),
                    HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, 2)
                );
                // add last tri
                AddTriangleUnperturbed(
                    Perturb(HexMetrics.TerraceLerp(right, left, HexMetrics.TerraceSteps - 3)),
                    Vector3.Lerp(Perturb(right), Perturb(HexMetrics.TerraceLerp(right, left, HexMetrics.TerraceSteps - 1)), (float)(HexMetrics.TerraceSteps - 3) / (float)(HexMetrics.TerraceSteps - 1)),
                    Perturb(HexMetrics.TerraceLerp(right, left, HexMetrics.TerraceSteps - 1))
                    
                );
                AddTriangleColor(
                    HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, HexMetrics.TerraceSteps - 3),
                    HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, HexMetrics.TerraceSteps - 1),
                    HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, HexMetrics.TerraceSteps - 1)
                );

                // fill quads in situations where there are more steps than 2
                if (HexMetrics.terracesPerSlope <= 2) return;

                for (int i = 2; i < HexMetrics.TerraceSteps - 3; i += 2)
                {
                    AddQuadUnperturbed(                        
                        Vector3.Lerp(Perturb(right), Perturb(HexMetrics.TerraceLerp(right, left, HexMetrics.TerraceSteps - 1)), (float)(i + 2) / (float)(HexMetrics.TerraceSteps - 1)),
                        Vector3.Lerp(Perturb(right), Perturb(HexMetrics.TerraceLerp(right, left, HexMetrics.TerraceSteps - 1)), (float)(i) / (float)(HexMetrics.TerraceSteps - 1)),                        
                        Perturb(HexMetrics.TerraceLerp(right, left, i + 2)),
                        Perturb(HexMetrics.TerraceLerp(right, left, i))

                    );
                    AddQuadColor(
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
                AddQuad(v3, v4, left, HexMetrics.TerraceLerp(left, right, HexMetrics.TerraceSteps - 1));
                AddQuadColor(c3, c4, c3, c4);

                // add large final triangle
                AddTriangle(v4, HexMetrics.TerraceLerp(left, right, HexMetrics.TerraceSteps - 1), right);
                AddTriangleColor(c4, HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, HexMetrics.TerraceSteps - 1), rightCell.Color);

                // fill in step gaps between steps
                for (int i = 0; i < HexMetrics.TerraceSteps - 2; i += 2)
                {
                    AddTriangle(
                        HexMetrics.TerraceLerp(left, right, i),
                        HexMetrics.TerraceLerp(left, right, i + 1),
                        HexMetrics.TerraceLerp(left, right, i + 2)
                    );
                    AddTriangleColor(
                        HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, i),
                        HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, i + 1),
                        HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, i + 2)
                    );
                }

                // fill gaps between smoothed steps and quad
                if (HexMetrics.terracesPerSlope <= 1) return;

                // add first tri
                AddTriangleUnperturbed(
                    Perturb(left),
                    Perturb(HexMetrics.TerraceLerp(left, right, 2)),
                    Vector3.Lerp(Perturb(left), Perturb(HexMetrics.TerraceLerp(left, right, HexMetrics.TerraceSteps - 1)),(float)2/(float)(HexMetrics.TerraceSteps - 1))
                );
                AddTriangleColor(
                    leftCell.Color,
                    HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, 2),
                    HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, 2)
                );
                // add last tri
                AddTriangleUnperturbed(
                    Perturb(HexMetrics.TerraceLerp(left, right, HexMetrics.TerraceSteps - 3)),
                    Perturb(HexMetrics.TerraceLerp(left, right, HexMetrics.TerraceSteps - 1)),
                    Vector3.Lerp(Perturb(left), Perturb(HexMetrics.TerraceLerp(left, right, HexMetrics.TerraceSteps - 1)), (float)(HexMetrics.TerraceSteps - 3) / (float)(HexMetrics.TerraceSteps - 1))
                );
                AddTriangleColor(
                    HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, HexMetrics.TerraceSteps - 3),
                    HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, HexMetrics.TerraceSteps - 1),
                    HexMetrics.TerraceLerp(leftCell.Color, rightCell.Color, HexMetrics.TerraceSteps - 1)
                );

                // fill quads in situations where there are more steps than 2
                if (HexMetrics.terracesPerSlope <= 2) return;

                for (int i = 2; i < HexMetrics.TerraceSteps - 3; i += 2)
                {
                    AddQuadUnperturbed(
                        Vector3.Lerp(Perturb(left), Perturb(HexMetrics.TerraceLerp(left, right, HexMetrics.TerraceSteps - 1)), (float)(i) / (float)(HexMetrics.TerraceSteps - 1)),
                        Vector3.Lerp(Perturb(left), Perturb(HexMetrics.TerraceLerp(left, right, HexMetrics.TerraceSteps - 1)), (float)(i + 2) / (float)(HexMetrics.TerraceSteps - 1)),
                        Perturb(HexMetrics.TerraceLerp(left, right, i)),
                        Perturb(HexMetrics.TerraceLerp(left, right, i + 2))

                    );
                    AddQuadColor(
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
            AddQuad(v3, v4, left, right);
            AddQuadColor(c3, c4, leftCell.Color, rightCell.Color);
        }
    }

    #endregion

    #region Helpers - Add Tris, Quads, Color

    void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(Perturb(v1));
        vertices.Add(Perturb(v2));
        vertices.Add(Perturb(v3));
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    void AddTriangleColor(Color c1)
    {
        colors.Add(c1);
        colors.Add(c1);
        colors.Add(c1);
    }

    void AddTriangleColor(Color c1, Color c2, Color c3)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
    }

    void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(Perturb(v1));
        vertices.Add(Perturb(v2));
        vertices.Add(Perturb(v3));
        vertices.Add(Perturb(v4));
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }

    void AddQuadUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        vertices.Add(v4);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }

    void AddQuadColor(Color c1, Color c2)
    {
        colors.Add(c1);
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c2);
    }

    void AddQuadColor(Color c1, Color c2, Color c3, Color c4)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
        colors.Add(c4);
    }

    #endregion

    #region Helpers - Perturbation

    Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = HexMetrics.SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * HexMetrics.cellPerturbStrength;
        //position.y += (sample.y * 2f - 1f) * HexMetrics.cellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * HexMetrics.cellPerturbStrength;
        return position;
    }

    void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
    {
        AddTriangle(center, edge.v1, edge.v2);
        AddTriangleColor(color);
        AddTriangle(center, edge.v2, edge.v3);
        AddTriangleColor(color);
        AddTriangle(center, edge.v3, edge.v4);
        AddTriangleColor(color);
    }

    void TriangulateEdgeStrip(
        EdgeVertices e1, Color c1,
        EdgeVertices e2, Color c2
    )
    {
        AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        AddQuadColor(c1, c2);
        AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        AddQuadColor(c1, c2);
        AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        AddQuadColor(c1, c2);
    }

    #endregion
}
