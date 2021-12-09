using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointCloudGenerator : MonoBehaviour
{
    [Header("Point Cloud")]
    public string fileName = "211022_Topo_Pointcloud_Detail_Transformed";
    public int maximumVertices = 100000;
    public PointCloud pointCloud;

    private Vector3 baseOffset = new Vector3(-3400, -481, -250);

    public GameObject sphere;

    private MeshInfos meshes;

    
    private void OnEnable() {
        Generate();
        
    }

    public MeshInfos LoadPointCloud() {
        string filePath = Application.streamingAssetsPath+"/"+ fileName + ".xyz";
        return SimpleImporter.Instance.Load(filePath, maximumVertices);
    }

    public void Generate() {
        meshes = LoadPointCloud();
        Debug.Log("Center: "+meshes.bounds.center);
        /*
        for(int i = 0; i < meshes.vertexCount; i++) {
            GameObject.Instantiate(sphere, meshes.vertices[i]-meshes.bounds.center, Quaternion.identity, transform);
        }*/
        for (int i = 0; i < meshes.vertexCount; i++) {
            meshes.vertices[i] -= meshes.bounds.center;
            //meshes.vertices[i] /= 4;
        }
        pointCloud.SetupPointCloudShader(meshes.vertices);
    }
    public MeshInfos GetMeshInfos() {
        return meshes;
    }

    public Vector3[] getVertices() {
        return meshes.vertices;
    }

    public Bounds getBounds() {
        return meshes.bounds;
    }
}
