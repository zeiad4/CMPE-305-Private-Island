using UnityEngine;
using UnityEngine.Rendering;

namespace PrivateIsland
{
    internal static class IslandMeshBuilder
    {
        public static float SampleHeight(float x, float z, float islandSize, float peakHeight)
        {
            float halfSize = islandSize * 0.5f;
            float radial = Mathf.Sqrt((x * x) + (z * z)) / halfSize;
            radial = Mathf.Clamp01(radial);

            float corePlateau = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.58f, 0.84f, radial));
            float shorelineShelf = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.78f, 1f, radial));
            float interiorBlend = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.2f, 0.86f, radial));

            float broadNoise = Mathf.PerlinNoise((x * 0.018f) + 17.4f, (z * 0.018f) + 9.2f) - 0.5f;
            float detailNoise = Mathf.PerlinNoise((x * 0.045f) + 51.8f, (z * 0.045f) + 29.6f) - 0.5f;
            float coastNoise = Mathf.PerlinNoise((x * 0.038f) + 103.7f, (z * 0.038f) + 65.3f) - 0.5f;

            float plateauHeight = Mathf.Lerp(0.54f, 0.74f, corePlateau);
            float noise = ((broadNoise * 0.22f) + (detailNoise * 0.08f)) * interiorBlend;
            float coastVariation = coastNoise * shorelineShelf * 0.05f;

            float shape = plateauHeight + (shorelineShelf * 0.05f) + noise + coastVariation;
            shape *= Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.9f, 1f, radial));
            shape = Mathf.Clamp01(shape);

            return shape * peakHeight;
        }

        public static float SampleSlope(float x, float z, float islandSize, float peakHeight, float step = 0.8f)
        {
            float left = SampleHeight(x - step, z, islandSize, peakHeight);
            float right = SampleHeight(x + step, z, islandSize, peakHeight);
            float down = SampleHeight(x, z - step, islandSize, peakHeight);
            float up = SampleHeight(x, z + step, islandSize, peakHeight);

            Vector3 gradient = new Vector3(left - right, step * 2f, down - up).normalized;
            return 1f - gradient.y;
        }

        public static void RebuildTerrainMesh(Mesh mesh, int resolution, float islandSize, float peakHeight)
        {
            resolution = Mathf.Max(16, resolution);

            int vertexCount = (resolution + 1) * (resolution + 1);
            var vertices = new Vector3[vertexCount];
            var uv = new Vector2[vertexCount];
            var colors = new Color[vertexCount];
            var triangles = new int[resolution * resolution * 6];

            float halfSize = islandSize * 0.5f;
            int index = 0;

            for (int z = 0; z <= resolution; z++)
            {
                float v = z / (float)resolution;
                float worldZ = Mathf.Lerp(-halfSize, halfSize, v);

                for (int x = 0; x <= resolution; x++)
                {
                    float u = x / (float)resolution;
                    float worldX = Mathf.Lerp(-halfSize, halfSize, u);
                    float height = SampleHeight(worldX, worldZ, islandSize, peakHeight);
                    float slope = SampleSlope(worldX, worldZ, islandSize, peakHeight);

                    vertices[index] = new Vector3(worldX, height, worldZ);
                    uv[index] = new Vector2(u, v);
                    colors[index] = new Color(height / Mathf.Max(peakHeight, 0.001f), slope, 0f, 1f);
                    index++;
                }
            }

            int triangleIndex = 0;
            int stride = resolution + 1;

            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int baseIndex = x + (z * stride);

                    triangles[triangleIndex++] = baseIndex;
                    triangles[triangleIndex++] = baseIndex + stride;
                    triangles[triangleIndex++] = baseIndex + 1;

                    triangles[triangleIndex++] = baseIndex + 1;
                    triangles[triangleIndex++] = baseIndex + stride;
                    triangles[triangleIndex++] = baseIndex + stride + 1;
                }
            }

            mesh.Clear();
            mesh.indexFormat = vertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.name = "Island Terrain";
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        public static void RebuildWaterMesh(Mesh mesh, float radius, int radialSegments = 96)
        {
            radialSegments = Mathf.Max(16, radialSegments);

            var vertices = new Vector3[radialSegments + 1];
            var uv = new Vector2[radialSegments + 1];
            var triangles = new int[radialSegments * 3];

            vertices[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < radialSegments; i++)
            {
                float angle = (i / (float)radialSegments) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;

                vertices[i + 1] = new Vector3(x, 0f, z);
                uv[i + 1] = new Vector2((x / (radius * 2f)) + 0.5f, (z / (radius * 2f)) + 0.5f);
            }

            for (int i = 0; i < radialSegments; i++)
            {
                int next = (i + 1) % radialSegments;
                int triangleIndex = i * 3;

                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = next + 1;
                triangles[triangleIndex + 2] = i + 1;
            }

            mesh.Clear();
            mesh.name = "Island Water";
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        public static void RebuildTerrainTexture(Texture2D texture, int resolution, float islandSize, float peakHeight, float seaLevel)
        {
            resolution = Mathf.Max(64, resolution);
            var pixels = new Color[resolution * resolution];
            float halfSize = islandSize * 0.5f;

            Color deepGrass = new Color(0.22f, 0.43f, 0.2f);
            Color brightGrass = new Color(0.36f, 0.57f, 0.28f);
            Color cliff = new Color(0.43f, 0.42f, 0.39f);
            Color highlight = new Color(0.61f, 0.58f, 0.5f);
            Color sand = new Color(0.86f, 0.79f, 0.62f);
            Color wetSand = new Color(0.71f, 0.65f, 0.52f);

            for (int y = 0; y < resolution; y++)
            {
                float v = y / (float)(resolution - 1);
                float worldZ = Mathf.Lerp(-halfSize, halfSize, v);

                for (int x = 0; x < resolution; x++)
                {
                    float u = x / (float)(resolution - 1);
                    float worldX = Mathf.Lerp(-halfSize, halfSize, u);
                    float height = SampleHeight(worldX, worldZ, islandSize, peakHeight);
                    float slope = SampleSlope(worldX, worldZ, islandSize, peakHeight);
                    float normalizedHeight = height / Mathf.Max(peakHeight, 0.001f);

                    Color color;
                    if (height < seaLevel + 0.6f)
                    {
                        float beachBlend = Mathf.InverseLerp(seaLevel - 0.35f, seaLevel + 0.6f, height);
                        color = Color.Lerp(wetSand, sand, beachBlend);
                    }
                    else
                    {
                        color = Color.Lerp(deepGrass, brightGrass, normalizedHeight);

                        float cliffBlend = Mathf.InverseLerp(0.25f, 0.62f, slope);
                        cliffBlend = Mathf.Max(cliffBlend, Mathf.InverseLerp(0.72f, 0.96f, normalizedHeight) * 0.65f);
                        color = Color.Lerp(color, cliff, cliffBlend);
                        color = Color.Lerp(color, highlight, cliffBlend * 0.22f);
                    }

                    float noiseTint = Mathf.PerlinNoise((worldX * 0.22f) + 301.3f, (worldZ * 0.22f) + 115.4f);
                    color *= Mathf.Lerp(0.94f, 1.06f, noiseTint);
                    pixels[(y * resolution) + x] = color;
                }
            }

            texture.Reinitialize(resolution, resolution, TextureFormat.RGBA32, false);
            texture.name = "Island Terrain Texture";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: true, makeNoLongerReadable: false);
        }
    }
}
