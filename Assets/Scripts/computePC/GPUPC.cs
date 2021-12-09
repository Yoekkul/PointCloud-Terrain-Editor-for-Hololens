using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUPC : MonoBehaviour {
    [Header("Point Cloud")]
    public string fileName = "211022_Topo_Pointcloud_Detail_Transformed.xyz";
    int maximumVertices = 100000;
    public Mesh mesh;

    [Header("Internals")]
    [SerializeField]
    Material material;
    [SerializeField]
    ComputeShader computeShader;

    private MeshInfos meshes;

    static readonly int
        positionsId = Shader.PropertyToID("_Positions"),
        colorsId = Shader.PropertyToID("_Colors"),
        widthId = Shader.PropertyToID("_Width"),
        heightId = Shader.PropertyToID("_Height");

    ComputeBuffer positionsBuffer;
    ComputeBuffer colorBuffer;

    int width = 500;
    int height = 500;

    Bounds sceneBounds;

    void UpdateFunctionOnGPU() {
        material.SetBuffer(positionsId, positionsBuffer);
        material.SetBuffer(colorsId, colorBuffer);
        //material.SetFloat(stepId, step);

        
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, new Bounds(Vector3.zero, Vector3.one * 10000f), positionsBuffer.count);
        
    }


    private void SetupPointCloudShader() {
        sceneBounds = meshes.bounds;

        positionsBuffer = new ComputeBuffer(meshes.vertices.Length, 3 * 4);
        if(meshes.colors != null) {
            colorBuffer = new ComputeBuffer(meshes.vertices.Length, sizeof(float) * 4);//, ComputeBufferType.Default
            colorBuffer.SetData(meshes.colors);
        }

        

        positionsBuffer.SetData(meshes.vertices);
        //colorbuffer.SetData(color);
    }

    public Vector3[] GetVertices() {
        return meshes.vertices;
    }

    public MeshInfos LoadPointCloud() {
        string filePath = Application.streamingAssetsPath + "/" + fileName;
        return SimpleImporter.Instance.Load(filePath, maximumVertices);
    }

    public void Generate() {
        meshes = LoadPointCloud();
        Debug.Log("Center: " + meshes.bounds.center);
        Debug.Log("Count: " + meshes.vertexCount);
        
        for (int i = 0; i < meshes.vertexCount; i++) {
            meshes.vertices[i] -= meshes.bounds.center;
        }
    }

    private void OnEnable() {
        if(meshes == null) {
            Generate();
        }
        SetupPointCloudShader();
        
    }

    public void UpdatePointCloud(Vector3[] newPositions) {
        positionsBuffer.SetData(newPositions);
    }

    private void OnDisable() {
        colorBuffer.Release();
        colorBuffer = null;
        positionsBuffer.Release();
        positionsBuffer = null;
    }

    private void Update() {
        UpdateFunctionOnGPU();
    }
}


