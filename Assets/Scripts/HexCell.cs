using UnityEngine;
using System.IO;

public class HexCell : MonoBehaviour
{
    #region Variables

    [SerializeField]
    HexCell[] neighbors;

    public HexCoordinates coordinates;

    public RectTransform uiRect;

    public HexGridChunk chunk;

    #region Elevation

    // Elevation
    public int Elevation {
        get {
            return elevation;
        }
        set {
            if (elevation == value)
            {
                return;
            }
            elevation = value;
            RefreshPosition();
            ValidateRivers();

            for (int i = 0; i < roads.Length; i++)
            {
                if (roads[i] && GetElevationDifference((HexDirection)i) > 1)
                {
                    SetRoad(i, false);
                }
            }

            Refresh();
        }
    }
    int elevation = int.MinValue;

    void RefreshPosition()
    {
        Vector3 position = transform.localPosition;
        position.y = elevation * HexMetrics.elevationStep;
        position.y +=
            (HexMetrics.SampleNoise(position).y * 2f - 1f) *
            HexMetrics.elevationPerturbStrength;
        transform.localPosition = position;

        Vector3 uiPosition = uiRect.localPosition;
        uiPosition.z = -position.y;
        uiRect.localPosition = uiPosition;
    }

    #endregion

    #region Color

    //Color
    public Color Color {
        get {
            return HexMetrics.colors[terrainTypeIndex];
        }
    }
    int terrainTypeIndex;
    public int TerrainTypeIndex {
        get {
            return terrainTypeIndex;
        }
        set {
            if (terrainTypeIndex != value)
            {
                terrainTypeIndex = value;
                Refresh();
            }
        }
    }

    #endregion

    #region Roads

    [SerializeField]
    bool[] roads;

    #endregion

    #region Water

    public int WaterLevel {
        get {
            return waterLevel;
        }
        set {
            if (waterLevel == value)
            {
                return;
            }
            waterLevel = value;
            ValidateRivers();
            Refresh();
        }
    }

    int waterLevel;

    public bool IsUnderwater {
        get {
            return waterLevel > elevation;
        }
    }

    public float WaterSurfaceY {
        get {
            return
                (waterLevel + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;
        }
    }

    #endregion
        
    #region Features

    public int UrbanLevel {
        get {
            return urbanLevel;
        }
        set {
            if (urbanLevel != value)
            {
                urbanLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int FarmLevel {
        get {
            return farmLevel;
        }
        set {
            if (farmLevel != value)
            {
                farmLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int PlantLevel {
        get {
            return plantLevel;
        }
        set {
            if (plantLevel != value)
            {
                plantLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    int urbanLevel, farmLevel, plantLevel;

    #endregion

    #region Walls

    public bool Walled {
        get {
            return walled;
        }
        set {
            if (walled != value)
            {
                walled = value;
                Refresh();
            }
        }
    }

    bool walled;

    #endregion

    #region Special Features

    int specialIndex;
    public int SpecialIndex {
        get {
            return specialIndex;
        }
        set {
            if (specialIndex != value)
            {
                specialIndex = value;
                RefreshSelfOnly();
            }
        }
    }
    public bool IsSpecial {
        get {
            return specialIndex > 0;
        }
    }


    #endregion

    #endregion

    #region Getting Neighbors and Edges

    public HexCell GetNeighbor(HexDirection direction)
    {
        return neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection direction)
    {
        return HexMetrics.GetEdgeType(
            elevation, neighbors[(int)direction].elevation
        );
    }

    public HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(
            elevation, otherCell.elevation
        );
    }

    #endregion

    #region Rivers

    #region Removing Rivers

    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    public void RemoveOutgoingRiver()
    {
        if (!hasOutgoingRiver)
        {
            return;
        }
        hasOutgoingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(outgoingRiver);
        neighbor.hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveIncomingRiver()
    {
        if (!hasIncomingRiver)
        {
            return;
        }
        hasIncomingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(incomingRiver);
        neighbor.hasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    #endregion

    #region Adding Rivers

    public void SetOutgoingRiver(HexDirection direction)
    {
        if (hasOutgoingRiver && outgoingRiver == direction)
        {
            return;
        }
        HexCell neighbor = GetNeighbor(direction);
        if (!IsValidRiverDestination(neighbor))
        {
            return;
        }

        RemoveOutgoingRiver();
        if (hasIncomingRiver && incomingRiver == direction)
        {
            RemoveIncomingRiver();
        }

        hasOutgoingRiver = true;
        outgoingRiver = direction;
        //RefreshSelfOnly();

        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = direction.Opposite();
        //neighbor.RefreshSelfOnly();

        // refresh happens in here
        SetRoad((int)direction, false);
    }

    #endregion

    #region River Info

    #region River Bools and Directions

    bool hasIncomingRiver, hasOutgoingRiver;
    HexDirection incomingRiver, outgoingRiver;

    public bool HasIncomingRiver {
        get {
            return hasIncomingRiver;
        }
    }

    public bool HasOutgoingRiver {
        get {
            return hasOutgoingRiver;
        }
    }

    public HexDirection IncomingRiver {
        get {
            return incomingRiver;
        }
    }

    public HexDirection OutgoingRiver {
        get {
            return outgoingRiver;
        }
    }

    public HexDirection RiverBeginOrEndDirection {
        get {
            return hasIncomingRiver ? incomingRiver : outgoingRiver;
        }
    }

    public bool HasRiver {
        get {
            return hasIncomingRiver || hasOutgoingRiver;
        }
    }

    public bool HasRiverBeginOrEnd {
        get {
            return hasIncomingRiver != hasOutgoingRiver;
        }
    }

    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return
            hasIncomingRiver && incomingRiver == direction ||
            hasOutgoingRiver && outgoingRiver == direction;
    }

    #endregion

    public Vector3 Position {
        get {
            return transform.localPosition;
        }
    }

    public float StreamBedY {
        get {
            return
                (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;
        }
    }

    public float RiverSurfaceY {
        get {
            return
                (elevation + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;
        }
    }

    #endregion

    bool IsValidRiverDestination(HexCell neighbor)
    {
        return neighbor && (
            elevation >= neighbor.elevation || waterLevel == neighbor.elevation
        );
    }

    void ValidateRivers()
    {
        if (
            hasOutgoingRiver &&
            !IsValidRiverDestination(GetNeighbor(outgoingRiver))
        )
        {
            RemoveOutgoingRiver();
        }
        if (
            hasIncomingRiver &&
            !GetNeighbor(incomingRiver).IsValidRiverDestination(this)
        )
        {
            RemoveIncomingRiver();
        }
    }

    #endregion

    #region Roads

    public bool HasRoadThroughEdge(HexDirection direction)
    {
        return roads[(int)direction];
    }

    public bool HasRoads {
        get {
            for (int i = 0; i < roads.Length; i++)
            {
                if (roads[i])
                {
                    return true;
                }
            }
            return false;
        }
    }

    public void AddRoad(HexDirection direction)
    {
        if (!roads[(int)direction] && !HasRiverThroughEdge(direction))// && GetElevationDifference(direction) <= 1)
        {
            SetRoad((int)direction, true);
        }
    }

    void SetRoad(int index, bool state)
    {
        roads[index] = state;
        neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
        neighbors[index].RefreshSelfOnly();
        RefreshSelfOnly();
    }


    public void RemoveRoads()
    {
        for (int i = 0; i < neighbors.Length; i++)
        {
            if (roads[i])
            {
                roads[i] = false;
            }
        }
    }

    public int GetElevationDifference(HexDirection direction)
    {
        int difference = elevation - GetNeighbor(direction).elevation;
        return difference >= 0 ? difference : -difference;
    }

    #endregion

    void Refresh()
    {
        if (chunk)
        {
            chunk.Refresh();
            for (int i = 0; i < neighbors.Length; i++)
            {
                HexCell neighbor = neighbors[i];
                if (neighbor != null && neighbor.chunk != chunk)
                {
                    neighbor.chunk.Refresh();
                }
            }
        }
    }

    void RefreshSelfOnly()
    {
        chunk.Refresh();
    }

    #region Saving and Loading

    public void Save(BinaryWriter writer)
    {
        // converting to bytes to save some space
        writer.Write((byte)terrainTypeIndex);
        writer.Write((byte)elevation);
        writer.Write((byte)waterLevel);
        writer.Write((byte)urbanLevel);
        writer.Write((byte)farmLevel);
        writer.Write((byte)plantLevel);
        writer.Write((byte)specialIndex);
        writer.Write(walled);
        // we do it like this to save some data space
        if (hasIncomingRiver)
        {
            writer.Write((byte)(incomingRiver + 128));
        }
        else
        {
            writer.Write((byte)0);
        }

        if (hasOutgoingRiver)
        {
            writer.Write((byte)(outgoingRiver + 128));
        }
        else
        {
            writer.Write((byte)0);
        }

        int roadFlags = 0;
        for (int i = 0; i < roads.Length; i++)
        {
            if (roads[i])
            {
                roadFlags |= 1 << i;
            }
        }
        writer.Write((byte)roadFlags);
    }

    public void Load(BinaryReader reader)
    {
        terrainTypeIndex = reader.ReadByte();
        elevation = reader.ReadByte();
        RefreshPosition();
        waterLevel = reader.ReadByte();
        urbanLevel = reader.ReadByte();
        farmLevel = reader.ReadByte();
        plantLevel = reader.ReadByte();
        specialIndex = reader.ReadByte();
        walled = reader.ReadBoolean();

        byte riverData = reader.ReadByte();
        if (riverData >= 128)
        {
            hasIncomingRiver = true;
            incomingRiver = (HexDirection)(riverData - 128);
        }
        else
        {
            hasIncomingRiver = false;
        }

        riverData = reader.ReadByte();
        if (riverData >= 128)
        {
            hasOutgoingRiver = true;
            outgoingRiver = (HexDirection)(riverData - 128);
        }
        else
        {
            hasOutgoingRiver = false;
        }

        int roadFlags = reader.ReadByte();
        for (int i = 0; i < roads.Length; i++)
        {
            roads[i] = (roadFlags & (1 << i)) != 0;
        }
    }

    #endregion
}
