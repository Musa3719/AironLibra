using UnityEngine;

public class TerrainBehaviour : MonoBehaviour
{
    public PlaneSoundType[,] _SoundTypeMap;
    private Terrain _terrain;
    private void Awake()
    {
        _terrain = GetComponent<Terrain>();
        CacheTerrainSoundTypes(_terrain);
    }

    private void CacheTerrainSoundTypes(Terrain terrain)
    {
        int w = terrain.terrainData.alphamapWidth;
        int h = terrain.terrainData.alphamapHeight;

        float[,,] alphaMap = terrain.terrainData.GetAlphamaps(0, 0, w, h);

        _SoundTypeMap = new PlaneSoundType[w, h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // her noktadaki layer aðýrlýklarý
                float[] weights = new float[alphaMap.GetLength(2)];
                if (weights.Length == 0) continue;
                for (int i = 0; i < weights.Length; i++)
                {
                    weights[i] = alphaMap[y, x, i];
                }

                _SoundTypeMap[x, y] = GetDominantTextureType(weights);
            }
        }
    }
    private PlaneSoundType GetDominantTextureType(float[] textureWeights)
    {
        int dominantIndex = 0;
        float maxWeight = textureWeights[0];
        float weightMultiplier = 1f;
        for (int i = 0; i < textureWeights.Length; i++)
        {
            /*if (i == 0)//desert
                weightMultiplier = 1.15f;
            else if (i == 1)//plain
                weightMultiplier = 0.6f;
            else if (i == 5)//rocky
                weightMultiplier = 3f;
            else if (i == 7)//snowy
                weightMultiplier = 10f;*/
            //check for snow

            //textureWeights[i] += i == 1 ? textureWeights[6] : 0f;//adds Stone Moss weight to plain weight
            if (textureWeights[i] * weightMultiplier > maxWeight)
            {
                maxWeight = textureWeights[i] * weightMultiplier;
                dominantIndex = i;
            }
        }
        switch (dominantIndex)
        {
            case 0:
                return PlaneSoundType.Sand;
            case 1:
                return PlaneSoundType.Grass;
            case 2:
                return PlaneSoundType.Grass;
            case 3:
                return PlaneSoundType.Dirt;
            case 4:
                return PlaneSoundType.WoodenDirt;
            case 5:
                return PlaneSoundType.Stone;
            case 6:
                return PlaneSoundType.Stone;
            case 7:
                return PlaneSoundType.Snow;
            default:
                return PlaneSoundType.Dirt;
        }
    }

}
