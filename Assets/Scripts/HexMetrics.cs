using UnityEngine;

public class HexMetrics
{
    #region Vars
    // start vars
    #region World Size

    // world  size
    public const int chunkSizeX = 5, chunkSizeZ = 5;

    #endregion

    #region Hex Size

    // hex size
    public const float outerToInner = 0.866025404f;
    public const float innerToOuter = 1f / outerToInner;

    public const float outerRadius = 10f;

    public const float innerRadius = outerRadius * outerToInner;

    // inner hex ratio
    //public const float solidFactor = 0.9f;
    //public const float blendFactor = 1f - solidFactor;
    public static float solidFactor;

    #endregion

    #region Colors

    public static Color[] colors;

    #endregion

    #region Elevation
    // elevation
    public static float elevationStep = 2f;

    #endregion

    #region Terraces

    // terraces
    //public const int terracesPerSlope = 2;
    //public const int terraceSteps = terracesPerSlope * 2 + 1;
    //public const float horizontalTerraceStepSize = 1f / terraceSteps;
    //public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);
    public static int terracesPerSlope = 2;
    public static int TerraceSteps {
        get {
            return terracesPerSlope * 2 + 1;
        }
    }
    public static float HorizontalTerraceStepSize {
        get {
            return 1f / TerraceSteps;
        }
    }
    public static float VerticalTerraceStepSize {
        get {
            return 1f / (terracesPerSlope + 1);
        }
    }

    #endregion

    #region Perturbation and Noise

    // perturbation and noise
    //public const float cellPerturbStrength = 4f;
    //public const float elevationPerturbStrength = 1.5f;
    public static float cellPerturbStrength = 4f;
    public static float elevationPerturbStrength = 1.5f;
    public const float noiseScale = 0.003f;
    public static Texture2D noiseSource;

    public const int hashGridSize = 256;
    static HexHash[] hashGrid;
    public const float hashGridScale = 0.25f;



    #endregion

    #region Rivers
    // rivers
    public const float streamBedElevationOffset = -1.75f;
    public const float waterElevationOffset = -0.5f;

    #endregion

    #region Water

    public const float waterFactor = 0.6f;
    public const float waterBlendFactor = 1f - waterFactor;


    #endregion

    #region Features and Walls and bridges

    static float[][] featureThresholds = {
        new float[] {0.0f, 0.0f, 0.4f},
        new float[] {0.0f, 0.4f, 0.6f},
        new float[] {0.4f, 0.6f, 0.8f}
    };

    public const float wallHeight = 4f;
    public const float wallYOffset = -1f;
    public const float wallThickness = 0.75f;
    public static float wallElevationOffset = VerticalTerraceStepSize;

    // Tower freq
    public const float wallTowerThreshold = 0.5f;

    // bridge length
    public const float bridgeDesignLength = 7f;



    #endregion
    /// end vars
    #endregion

    #region Getting Corners

    static Vector3[] corners = {
        new Vector3(0f, 0f, outerRadius),
        new Vector3(innerRadius, 0f, 0.5f * outerRadius),
        new Vector3(innerRadius, 0f, -0.5f * outerRadius),
        new Vector3(0f, 0f, -outerRadius),
        new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
        new Vector3(-innerRadius, 0f, 0.5f * outerRadius)
    };

    public static Vector3 GetFirstCorner(HexDirection direction)
    {
        return corners[(int)direction];
    }

    public static Vector3 GetSecondCorner(HexDirection direction)
    {
        return corners[((int)direction + 1) % 6];
    }

    public static Vector3 GetFirstSolidCorner(HexDirection direction)
    {
        return corners[(int)direction] * solidFactor;
    }

    public static Vector3 GetSecondSolidCorner(HexDirection direction)
    {
        return corners[((int)direction + 1) % 6] * solidFactor;
    }

    #endregion

    #region Bridges, Terraces, and Edges

    public static Vector3 GetBridge(HexDirection direction)
    {
        //return (corners[(int)direction] + corners[((int)direction + 1) % 6]) * blendFactor;
        return (corners[(int)direction] + corners[((int)direction + 1) % 6]) * (1 - solidFactor);
    }

    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
    {
        float h = step * HexMetrics.HorizontalTerraceStepSize;
        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;
        float v = ((step + 1) / 2) * HexMetrics.VerticalTerraceStepSize;
        a.y += (b.y - a.y) * v;
        return a;
    }

    public static Color TerraceLerp(Color a, Color b, int step)
    {
        float h = step * HexMetrics.HorizontalTerraceStepSize;
        return Color.Lerp(a, b, h);
    }

    public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
    {
        if (elevation1 == elevation2)
        {
            return HexEdgeType.Flat;
        }
        int delta = elevation2 - elevation1;
        if (delta == 1 || delta == -1 || delta == 2 || delta == -2)
        {
            return HexEdgeType.Terrace;
        }
        return HexEdgeType.Cliff;
    }

    public static Vector3 GetSolidEdgeMiddle(HexDirection direction)
    {
        return
            (corners[(int)direction] + corners[((int)direction + 1) % 6]) *
            (0.5f * solidFactor);
    }


    #endregion

    #region Noise and Irreg

    public static Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * cellPerturbStrength;
        //position.y += (sample.y * 2f - 1f) * HexMetrics.cellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * cellPerturbStrength;
        return position;
    }

    public static Vector4 SampleNoise(Vector3 position)
    {
        return noiseSource.GetPixelBilinear(
            position.x * noiseScale,
            position.z * noiseScale
        );
    }

    public static void InitializeHashGrid(int seed)
    {
        hashGrid = new HexHash[hashGridSize * hashGridSize];
        Random.State currentState = Random.state;
        Random.InitState(seed);
        for (int i = 0; i < hashGrid.Length; i++)
        {
            hashGrid[i] = HexHash.Create();
        }
        Random.state = currentState;
    }

    public static HexHash SampleHashGrid(Vector3 position)
    {
        int x = (int)(position.x * hashGridScale) % hashGridSize;
        if (x < 0)
        {
            x += hashGridSize;
        }
        int z = (int)(position.z * hashGridScale) % hashGridSize;
        if (z < 0)
        {
            z += hashGridSize;
        }
        return hashGrid[x + z * hashGridSize];
    }


    #endregion

    #region Water

    public static Vector3 GetFirstWaterCorner(HexDirection direction)
    {
        return corners[(int)direction] * waterFactor;
    }

    public static Vector3 GetSecondWaterCorner(HexDirection direction)
    {
        return corners[((int)direction + 1) % 6] * waterFactor;
    }

    public static Vector3 GetWaterBridge(HexDirection direction)
    {
        return (corners[(int)direction] + corners[(int)direction + 1]) * waterBlendFactor;
    }


    #endregion

    #region Features and Walls

    public static float[] GetFeatureThresholds(int level)
    {
        return featureThresholds[level];
    }

    public static Vector3 WallThicknessOffset(Vector3 near, Vector3 far)
    {
        Vector3 offset;
        offset.x = far.x - near.x;
        offset.y = 0f;
        offset.z = far.z - near.z;
        return offset.normalized * (wallThickness * 0.5f);
    }

    public static Vector3 WallLerp(Vector3 near, Vector3 far)
    {
        near.x += (far.x - near.x) * 0.5f;
        near.z += (far.z - near.z) * 0.5f;
        float v = near.y < far.y ? wallElevationOffset : (1f - wallElevationOffset);
        near.y += (far.y - near.y) * v + wallYOffset;
        return near;
    }

    #endregion

}

public enum HexEdgeType
{
    Flat, Terrace, Cliff
}

public struct EdgeVertices
{
    public Vector3 v1, v2, v3, v4, v5;

    public EdgeVertices(Vector3 corner1, Vector3 corner2)
    {
        v1 = corner1;
        v2 = Vector3.Lerp(corner1, corner2, 0.25f);
        v3 = Vector3.Lerp(corner1, corner2, 0.5f);
        v4 = Vector3.Lerp(corner1, corner2, 0.75f);
        v5 = corner2;
    }

    public static EdgeVertices TerraceLerp(EdgeVertices a, EdgeVertices b, int step)
    {
        EdgeVertices result;
        result.v1 = HexMetrics.TerraceLerp(a.v1, b.v1, step);
        result.v2 = HexMetrics.TerraceLerp(a.v2, b.v2, step);
        result.v3 = HexMetrics.TerraceLerp(a.v3, b.v3, step);
        result.v4 = HexMetrics.TerraceLerp(a.v4, b.v4, step);
        result.v5 = HexMetrics.TerraceLerp(a.v5, b.v5, step);
        return result;
    }

    public EdgeVertices(Vector3 corner1, Vector3 corner2, float outerStep)
    {
        v1 = corner1;
        v2 = Vector3.Lerp(corner1, corner2, outerStep);
        v3 = Vector3.Lerp(corner1, corner2, 0.5f);
        v4 = Vector3.Lerp(corner1, corner2, 1f - outerStep);
        v5 = corner2;
    }
}

public struct HexHash
{
    public float a, b, c, d, e;

    public static HexHash Create()
    {
        HexHash hash;
        hash.a = Random.value * 0.999f;
        hash.b = Random.value * 0.999f;
        hash.c = Random.value * 0.999f;
        hash.d = Random.value * 0.999f;
        hash.e = Random.value * 0.999f;
        return hash;
    }
}
