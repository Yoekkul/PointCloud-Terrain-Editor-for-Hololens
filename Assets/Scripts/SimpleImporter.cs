using System.Collections;
using System.Collections.Generic;
using System.IO;
//using UnityEditor.AssetImporters;
using UnityEngine;

public class SimpleImporter : MonoBehaviour
{
	private static SimpleImporter instance;
	private SimpleImporter() { }
	public static SimpleImporter Instance {
		get {
			if (instance == null) {
				instance = new SimpleImporter();
			}
			return instance;
		}
	}

	public MeshInfos Load(string filePath, int maximumVertex = 65000) {
		MeshInfos data = new MeshInfos();
		FileInfo fi = new FileInfo(filePath);
		
        if (fi.Exists) {
			if(fi.Extension == ".xyz") {
				StreamReader reader = new StreamReader(filePath);
				List<Vector3> elmts = new List<Vector3>();
				while (!reader.EndOfStream) {
					string line = reader.ReadLine();
					string[] entries = line.Split(',');
					Vector3 pos = new Vector3(float.Parse(entries[0]), float.Parse(entries[2]), float.Parse(entries[1]));
					elmts.Add(pos);
				}
				data.vertexCount = elmts.Count;
				data.vertices = elmts.ToArray();
				Vector3 size = elmts[data.vertexCount - 1] - elmts[0];
				data.bounds = new Bounds(elmts[0]+size/2,size);
				Debug.Log("Data is read");
            }else if (fi.Extension == ".xyzrgb") {
				StreamReader reader = new StreamReader(filePath);
				List<Vector3> elmts = new List<Vector3>();
				List<Vector4> colors = new List<Vector4>();
				int i = 0;
				while (!reader.EndOfStream && i < 1000000) {
					string line = reader.ReadLine();
					string[] entries = line.Split(',');
					Vector3 pos = new Vector3(float.Parse(entries[0]), float.Parse(entries[2]), float.Parse(entries[1]));
					Vector4 col = new Vector4(float.Parse(entries[3])/255, float.Parse(entries[4])/255, float.Parse(entries[5])/255, 1f);
					elmts.Add(pos);
					colors.Add(col);
					
					//i++;
				}
				data.vertexCount = elmts.Count;
				data.vertices = elmts.ToArray();
				data.colors = colors.ToArray();
				Vector3 size = elmts[data.vertexCount - 1] - elmts[0];
				data.bounds = new Bounds(elmts[0] + size / 2, size);
				Debug.Log("Data is read");
			}

		} else {
			Debug.LogError(filePath+ " not found!");
        }

		return data;
    }
}

/*
//https://docs.unity3d.com/Manual/ScriptedImporters.html
[ScriptedImporter(1, "xyz")]
public class MeshInfosImporter : ScriptedImporter {

    public override void OnImportAsset(AssetImportContext ctx) {
		StreamReader reader = new StreamReader(ctx.assetPath);
		Debug.Log(ctx.assetPath);
		
    }
}
*/

public class MeshInfos{
	public int length;
	public int height;
	public Vector3[] vertices;
	public Vector3[] normals;
	public Vector4[] colors;
	public int vertexCount;
	public Bounds bounds;
}