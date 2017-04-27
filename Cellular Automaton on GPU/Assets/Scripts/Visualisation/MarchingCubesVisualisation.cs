﻿#define TRIANGLE

using UnityEngine;

namespace GPUFLuid
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
        public float scale;

        //The size of the CellularAutomaton
        private int gridSize;

        //The maximal volume of a cell
        private int maxVolume;

        //A compute shader that generates a texture3D out of a cellular automaton
        public ComputeShader texture3DCS;
        private int texture3DCSKernel;
        private RenderTexture texture3D;

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

        public void Initialize(int gridSize, int maxVolume)
        {
            this.gridSize = gridSize;
            this.maxVolume = maxVolume;

            texture3D = new RenderTexture(gridSize, gridSize, 1);
            texture3D.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            texture3D.volumeDepth = gridSize;
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
            quads = new ComputeBuffer(gridSize * gridSize * gridSize, 4 * 3 * sizeof(float), ComputeBufferType.Append);
            ComputeBuffer.CopyCount(quads, args, 0);
#else
            triangles = new ComputeBuffer(gridSize * gridSize * gridSize, 3 * 3 * sizeof(float), ComputeBufferType.Append);
            ComputeBuffer.CopyCount(triangles, args, 0);
#endif
        }

        private void InitializeShader()
        {
            marchingCubesCS.SetFloat("scale", scale);
            marchingCubesCS.SetInt("size", gridSize);
            marchingCubesCS.SetInt("maxVolume", maxVolume);
            marchingCubesCSKernel = marchingCubesCS.FindKernel("CSMain");

            texture3DCS.SetInt("size", gridSize);
            texture3DCS.SetInt("maxVolume", maxVolume);
            texture3DCSKernel = texture3DCS.FindKernel("CSMain");

            material.SetFloat("scale", scale);
            material.SetFloat("size", gridSize);
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
            texture3DCS.Dispatch(texture3DCSKernel, gridSize / 16, gridSize / 8, gridSize / 8);
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
            marchingCubesCS.Dispatch(marchingCubesCSKernel, gridSize / 16, gridSize / 8, gridSize / 8);

            RenderTexture3D(cells);
            RenderRealisticWater();
        }

        void OnPostRender()
        {
            material.SetPass(0);
#if CUBES
            ComputeBuffer.CopyCount(quads, args, 0);
            testMaterial.SetBuffer("quads", quads);
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