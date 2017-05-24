﻿#define TRIANGLES

using UnityEngine;

namespace GPUFluid
{
    /// <summary>
    /// This class executes a marching cubes algorithm on the data of a CellularAutomaton.
    /// At the moment there are two possible visualisations:
    /// The CUBES visualisation creates a voxelised mesh.
    /// The TRIANGLE visualisation creates a smooth mesh.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MarchingCubesVisualisation : MonoBehaviour
    {
        //The scale of the visualisation
        public Vector3 scale;

        //The size of the CellularAutomaton
        private GridDimensions dimensions;

        //A compute shader that generates a texture3D out of a cellular automaton
        public ComputeShader texture3DCS;
        private int texture3DCSKernel;
        public RenderTexture texture3D;

        //A compute shader that executes the Marching Cubes algorithm
        public ComputeShader marchingCubesCS;
        private int marchingCubesCSKernel;

        //The material for the mesh, that is generated by the Marching Cubes algorithm
        public Material material;

        //At the moment there are two possible visualisations:
        //The CUBES visualisation creates a voxelised mesh.
        //The TRIANGLE visualisation creates a smooth mesh.
#if CUBES
        private ComputeBuffer quads;
#else
        private ComputeBuffer triangles;
#endif

        //A compute buffer that stores the number of triangles/quads generated by the Marching Cubes algorith
        private ComputeBuffer args;
        private int[] data;

        public void Initialize(GridDimensions dimensions)
        {
            this.dimensions = dimensions;
            if(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB4444))
                texture3D = new RenderTexture(dimensions.x * 16, dimensions.y * 16, 1, RenderTextureFormat.ARGB4444);
            else 
                texture3D = new RenderTexture(dimensions.x * 16, dimensions.y * 16, 1);
            texture3D.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            texture3D.filterMode = FilterMode.Trilinear;
            texture3D.volumeDepth = dimensions.z * 16;
            texture3D.enableRandomWrite = true;
            texture3D.Create();
            material.SetTexture("_MainTex", texture3D);

            InitializeComputeBuffer();
            InitializeShader();
        }

        private void InitializeComputeBuffer()
        {
            args = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
            data = new int[4] { 0, 1, 0, 0 };
            args.SetData(data);

#if CUBES
            quads = new ComputeBuffer((dimensions.x * dimensions.y * dimensions.z) * 4096, 4 * 3 * sizeof(float), ComputeBufferType.Append);
            ComputeBuffer.CopyCount(quads, args, 0);
#else
            triangles = new ComputeBuffer((dimensions.x * dimensions.y * dimensions.z) * 4096, 3 * 3 * sizeof(float), ComputeBufferType.Append);
            ComputeBuffer.CopyCount(triangles, args, 0);
#endif
        }

        private void InitializeShader()
        {
            marchingCubesCS.SetInts("size", new int[] { dimensions.x * 16, dimensions.y * 16, dimensions.z * 16 });
            marchingCubesCSKernel = marchingCubesCS.FindKernel("CSMain");

            texture3DCS.SetInts("size", new int[] { dimensions.x * 16, dimensions.y * 16, dimensions.z * 16 });
            texture3DCSKernel = texture3DCS.FindKernel("CSMain");

            material.SetVector("scale", new Vector4(scale.x, scale.y, scale.z, 1));
        }

        /// <summary>
        /// Copy from the Water-basic Script from the standard assets.
        /// Used to render realistic water.
        /// </summary>
        private void RenderRealisticWater()
        {
            Vector4 waveSpeed = material.GetVector("WaveSpeed");
            float waveScale = material.GetFloat("_WaveScale");
            float t = Time.time / 20.0f;

            Vector4 offset4 = waveSpeed * (t * waveScale);
            Vector4 offsetClamped = new Vector4(Mathf.Repeat(offset4.x, 1.0f), Mathf.Repeat(offset4.y, 1.0f),
            Mathf.Repeat(offset4.z, 1.0f), Mathf.Repeat(offset4.w, 1.0f));
            material.SetVector("_WaveOffset", offsetClamped);
        }

        /// <summary>
        /// Creates a 3D-Texture out of a cellular automaton. The different fluid-types are represented with different color.
        /// </summary>
        /// <param name="cells">The cells of a CellularAutomaton</param>
        private void RenderTexture3D(ComputeBuffer cells)
        {
            texture3DCS.SetBuffer(texture3DCSKernel, "currentGeneration", cells);
            texture3DCS.SetTexture(texture3DCSKernel, "Result", texture3D);
            texture3DCS.Dispatch(texture3DCSKernel, dimensions.x, dimensions.y * 2, dimensions.z * 2);
        }

        /// <summary>
        /// Perfroms the Marching Cubes Algorithm and generates the mesh.
        /// </summary>
        /// <param name="cells">The cells of a CellularAutomaton</param>
        public void Render(ComputeBuffer cells)
        {
#if CUBES
            quads.SetCounterValue(0);
            marchingCubesCS.SetBuffer(marchingCubesCS.FindKernel("CSMain"), "cubes", quads);
#else
            triangles.SetCounterValue(0);
            marchingCubesCS.SetBuffer(marchingCubesCSKernel, "triangles", triangles);
#endif
            marchingCubesCS.SetBuffer(marchingCubesCSKernel, "currentGeneration", cells);
            marchingCubesCS.Dispatch(marchingCubesCSKernel, dimensions.x, dimensions.y * 2, dimensions.z * 2);

            RenderTexture3D(cells);
#if  !CUBES
            RenderRealisticWater();
#endif
        }

        void OnPostRender()
        {
            material.SetPass(0);
#if CUBES
            ComputeBuffer.CopyCount(quads, args, 0);
            material.SetBuffer("quads", quads);
            Graphics.DrawProceduralIndirect(MeshTopology.Points, args);
#else
            ComputeBuffer.CopyCount(triangles, args, 0);
            material.SetBuffer("triangles", triangles);
            Graphics.DrawProceduralIndirect(MeshTopology.Points, args);
#endif
        }

        /// <summary>
        /// Don't forget releasing the buffers.
        /// </summary>
        void OnDisable()
        {
#if CUBES
            quads.Release();
#else
            triangles.Release();
#endif
            args.Release();
        }
    }
}