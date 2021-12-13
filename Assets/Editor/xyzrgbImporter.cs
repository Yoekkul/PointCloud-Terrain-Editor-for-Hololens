using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Pcx{
    [ScriptedImporter(1, "xyzrgb")]
    public class XyzrgbImporter : ScriptedImporter {
        public override void OnImportAsset(AssetImportContext context) {
            // ComputeBuffer container
            // Create a prefab with PointCloudRenderer.
            
            var gameObject = new GameObject();
            PointCloudData data = SimplePointCloudData(context.assetPath);

            var renderer = gameObject.AddComponent<PointCloudRenderer>();
            renderer.sourceData = data;

            context.AddObjectToAsset("prefab", gameObject);
            if (data != null) context.AddObjectToAsset("data", data);

            context.SetMainObject(gameObject);

            //Broken
            /*
            var data = ImportAsBakedPointCloud(context.assetPath);
            if (data != null) {
                context.AddObjectToAsset("container", data);
                context.AddObjectToAsset("position", data.positionMap);
                context.AddObjectToAsset("color", data.colorMap);
                context.SetMainObject(data);
            }*/
        }
        PointCloudData SimplePointCloudData(string path) {
            try {
                Tuple<List<Vector3>, List<Color32>>  vc = LoadAndCenter(path);
                
                var data = ScriptableObject.CreateInstance<PointCloudData>();
                data.Initialize(vc.Item1, vc.Item2);
                data.name = Path.GetFileNameWithoutExtension(path);
                
                return data;
            } catch (Exception e) {
                Debug.LogError("Failed importing " + path + ". " + e.Message);
                return null;
            }
        }
        Mesh ImportAsMesh(string path) {
            var mesh = new Mesh();
            mesh.name = Path.GetFileNameWithoutExtension(path);
            Tuple<List<Vector3>, List<Color32>> vc = LoadAndCenter(path);

            /*
            mesh.indexFormat = header.vertexCount > 65535 ?
                IndexFormat.UInt32 : IndexFormat.UInt16;
            */
            mesh.SetVertices(vc.Item1);
            mesh.SetColors(vc.Item2);

            mesh.SetIndices(
                Enumerable.Range(0, vc.Item1.Count).ToArray(),
                MeshTopology.Points, 0
            );

            mesh.UploadMeshData(true);
            return mesh;
        }

        BakedPointCloud ImportAsBakedPointCloud(string path) {
            try {
                Tuple<List<Vector3>, List<Color32>> vc = LoadAndCenter(path);

                var data = ScriptableObject.CreateInstance<BakedPointCloud>();
                data.Initialize(vc.Item1, vc.Item2);
                data.name = Path.GetFileNameWithoutExtension(path);
                return data;
            } catch (Exception e) {
                Debug.LogError("Failed importing " + path + ". " + e.Message);
                return null;
            }
        }
        public float decimation = 0.01f; //(Decimate 99%) 0.01 -> 1.00 (Do not decimate)
        Tuple<List<Vector3>, List<Color32>> LoadAndCenter(string path) {
            var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            StreamReader reader = new StreamReader(stream);
            List<Vector3> elmts = new List<Vector3>();
            List<Color32> colors = new List<Color32>();
            int i = 0;

            outerloop:
            while (!reader.EndOfStream && i < 1000000) {
                string line = "";
                for(float j = 0; j < 1; j += decimation) {
                    if(reader.EndOfStream) 
                        goto outerloop;
                    line = reader.ReadLine();
                }
                string[] entries = line.Split(',');
                Vector3 pos = new Vector3(float.Parse(entries[0]), float.Parse(entries[2]), float.Parse(entries[1]));
                Color col = new Color(float.Parse(entries[3]) / 255, float.Parse(entries[4]) / 255, float.Parse(entries[5]) / 255, 1f);
                Color32 colB = col;
                elmts.Add(pos);
                colors.Add(colB);

                //i++;
            }

            //data.vertexCount = elmts.Count;
            Vector3 size = elmts[elmts.Count - 1] - elmts[0];
            Bounds bounds = new Bounds(elmts[0] + size / 2, size);
            Debug.Log(bounds.center);
            Debug.Log(elmts.Count);
            for (int j = 0; j < elmts.Count; j++) {
                elmts[j] = elmts[j] - bounds.center;
            }
            Tuple<List<Vector3>, List<Color32>> ret = new Tuple<List<Vector3>, List<Color32>>(elmts,colors);

            return ret;
        }
        static Material GetDefaultMaterial() {
            // Via package manager
            var path_upm = "Packages/jp.keijiro.pcx/Editor/Default Point.mat";
            // Via project asset database
            var path_prj = "Assets/Pcx/Editor/Default Point.mat";
            return AssetDatabase.LoadAssetAtPath<Material>(path_upm) ??
                   AssetDatabase.LoadAssetAtPath<Material>(path_prj);
        }



    }

}
