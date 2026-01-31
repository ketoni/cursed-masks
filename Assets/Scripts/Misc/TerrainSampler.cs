using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class TerrainSampler : Singleton<TerrainSampler>
{
    [SerializeField] LayerMask terrainPhasingLayer = 0; 
    [Min(1)] [SerializeField] int maxDetailDensity = 100;
    // Note: If you change the grass sampling radius you have to also change the
    // maximum density above. The value controls at least how loud some movement sounds
    // are played, essentially maxing out if we see the max amount at given radius.
    float grassSamplingRadius = 0.5f;

    [SerializeField] MeshCollider[] waterPlaneColliders;

    public Terrain Terrain => GetComponent<Terrain>();
    public Collider Collider => Terrain.GetComponent<Collider>();
    public TerrainData TerrainData => Terrain.terrainData;


    public TreeInstance[] TreesRandom
    { 
        get
        {
            var trees = TerrainData.treeInstances;
            System.Random rng = new();
            for (int i = trees.Length - 1; i > 0; i--)
            {
                int swapIndex = rng.Next(i + 1);
                (trees[swapIndex], trees[i]) = (trees[i], trees[swapIndex]);
            }
            return trees;
        }
    }


    void Awake()
    {
        // Check if we have water planes and for those we do,
        // ensure they are not convex and are disabled, so that the sampling will work correctly.
        // We also do not support rotated planes at this point.
        if (waterPlaneColliders.Count() == 0)
        {
            //Debug.LogWarning("No water planes for TerrainSampler");
            return;
        }
        foreach (var collider in waterPlaneColliders)
        {
            if (collider.transform.rotation != quaternion.identity)
            {
                throw new NotImplementedException("TerrainSampler doesn't support rotated water planes as of now");
            }
            if (collider.convex)
            {
                Debug.LogError("TerrainSampler water planes cannot have a convex collider!");
                return;
            }
            if (collider.enabled)
            {
                Debug.LogWarning("Disabling TerrainSampler water plane collider to allow submersion.");
                collider.enabled = false;
            }
        }
        if (terrainPhasingLayer == 0)
        {
            Debug.LogError("Terrain phasing layer is not set!", this);
        }
    }

    public List<string> GetTextureNames()
    {
        // Returns the names of textures currently used on the terrain
        var names = new List<string>();
        foreach (var layer in TerrainData.terrainLayers)
        {
            names.Add(layer.name);
        }
        return names;
    }

    public Dictionary<string, float> SampleTerrainLayersAt(Vector3 samplingPosition)
    {
        // Samples the terrain at given position and returns a dictionary of weights,
        // where keys are the names of terrain layers present at this position
        // and values are the respective weights of each layer. Weights should sum to 1.0

        // Convert sampling position to terrain local position
        Vector3 terrainPosition = Terrain.transform.position;
        Vector3 localPosition = samplingPosition - terrainPosition;

        // Normalize local position to get it in the range [0, 1] relative to the terrain
        float normalizedX = Mathf.InverseLerp(0, TerrainData.size.x, localPosition.x);
        float normalizedZ = Mathf.InverseLerp(0, TerrainData.size.z, localPosition.z);

        // Convert normalized coordinates to alpha map coordinates
        int mapX = Mathf.RoundToInt(normalizedX * (TerrainData.alphamapWidth - 1));
        int mapZ = Mathf.RoundToInt(normalizedZ * (TerrainData.alphamapHeight - 1));

        float[,,] alphaMap = TerrainData.GetAlphamaps(mapX, mapZ, 1, 1);

        // Loop through all the terrain layers on the position and return a weight map 
        Dictionary<string, float> weightMap = new();
        for (int i = 0; i < TerrainData.alphamapLayers; i++)
        {
            string layerName = TerrainData.terrainLayers[i].name;
            float weight = alphaMap[0, 0, i];
            weightMap[layerName] = weight;
        }
        return weightMap;
    }

    public float SampleWaterPlanesAt(Vector3 samplingPosition, float maxDepth)
    {
        // Samples all water planes and returns a value [0,1] depending on how deep below
        // "underwater" the sampling point is. Below `maxDepth` caps the value at 1
        foreach (var collider in waterPlaneColliders)
        {
            // Transform the sampling position into the local space of the collider
            Vector3 localPoint = collider.transform.InverseTransformPoint(samplingPosition);

            // Check if the localPoint is within the XZ bounds of the local space bounds
            Bounds localBounds = collider.sharedMesh.bounds;
            if (localPoint.x < localBounds.min.x || localPoint.x > localBounds.max.x ||
                localPoint.z < localBounds.min.z || localPoint.z > localBounds.max.z)
            {
                continue; // If not, we can't be under it 
            }

            // If the depth is positive, the point is underwater 
            float depth = localBounds.center.y - localPoint.y;
            if (depth > 0)
            {
                return Mathf.Clamp01(depth / maxDepth);
            }
        }

        // If no valid underwater point is found, return 0
        return 0f;
    }

    public Dictionary<string, int> SampleTerrainGrassAt(Vector3 samplingPosition)
    {
        // Samples the terrain at given position for grass detail textures, skippin any detail meshes.
        // Returns a dictionary with texture names as keys and the amount of that texture present
        // at the sampling position, restricted by `grassSamplingRadius`.

        // Convert sampling position to detail map position
        int mapX = Mathf.FloorToInt((samplingPosition.x - Terrain.transform.position.x) / TerrainData.size.x * TerrainData.detailWidth);
        int mapZ = Mathf.FloorToInt((samplingPosition.z - Terrain.transform.position.z) / TerrainData.size.z * TerrainData.detailHeight);

        // Radius to check around in detail map coordinates
        int mapRadius = Mathf.CeilToInt(grassSamplingRadius / (TerrainData.size.x / TerrainData.detailWidth));

        int[] sampledAmounts = new int[TerrainData.detailPrototypes.Length];
        var densities = new Dictionary<string, int>();

        // Loop through each detail layer
        for (int layer = 0; layer < TerrainData.detailPrototypes.Length; layer++)
        {
            var texture = TerrainData.detailPrototypes[layer].prototypeTexture;
            if (texture == null)
            {
                // If this is not a grass detail layer, skip
                continue;
            }
            // Get the detail layer data in the sampling radius
            var xIndex = Mathf.Max(0, mapX - mapRadius);
            var yIndex = Mathf.Max(0, mapZ - mapRadius);
            var width = Mathf.Min(2 * mapRadius + 1, TerrainData.detailWidth - (mapX - mapRadius));
            var height = Mathf.Min(2 * mapRadius + 1, TerrainData.detailHeight - (mapZ - mapRadius));
            int[,] detailLayer = TerrainData.GetDetailLayer(xIndex, yIndex, width, height, layer);

            // Sum up the grass objects in the retrieved area
            for (int x = 0; x < detailLayer.GetLength(0); x++)
            {
                for (int z = 0; z < detailLayer.GetLength(1); z++)
                {
                    sampledAmounts[layer] += detailLayer[x, z];
                }
            }
            // Store density
            densities[texture.name] = sampledAmounts[layer];
        }
        return densities;
    }

    internal float DetailDensityScale(int detailDensity)
    {
        // Returns [0.0, 1.0] based on the ratio of this detail density against the global maximum. 
        return Mathf.Min((float)detailDensity / maxDetailDensity, 1.0f);
    }

    public void SetTerrainGhosting(Collider collider, bool enabled)
    {
        // Makes given object phase through terrain obstacles if true and collides normally if false.
        // We always collide with the ground, though. If not enabled, the object is set to layer 0.
        int layer = (int)Mathf.Log(terrainPhasingLayer.value, 2);
        collider.gameObject.layer = enabled ? layer : 0;
    }

    internal float SampleHeight(Vector3 samplingPosition)
    {
        return Terrain.SampleHeight(samplingPosition);
    }

    internal SurfaceType SampleSurfaceTypeAt(Vector3 samplingPosition)
    {
        // Returns the first found SurfaceType we are touching at given position
        // The components refer to common terrain layers to unify footstep sound generation 
        var colliders = Physics.OverlapSphere(samplingPosition, 1f);
        foreach (var collider in colliders)
        {
            var surface = collider.gameObject.GetComponent<SurfaceType>();
            if (surface) return surface;
        }
        return null;
    }
}
