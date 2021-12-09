// https://github.com/przemyslawzaworski
//https://forum.unity.com/threads/point-cloud-compute-shader.764363/
using UnityEngine;

public class PointCloud : MonoBehaviour {
    public Material material;
    public Mesh mesh;
    protected int number = 512 * 512;
    protected ComputeBuffer depthbuffer;
    protected ComputeBuffer colorbuffer;
    protected ComputeBuffer meshBuffer;

    protected float[] depth;
    protected Vector4[] color;
    protected Vector3[] tris;

    static readonly float[,] m = new float[,] { { 0.8f, 0.01f }, { 0.01f, 0.8f } };

    float hash(Vector2 p)   //generates pseudorandom number from (0..1) range
    {
        return Mathf.Abs((Mathf.Sin(p.x * 12.9898f + p.y * 78.233f) * 43758.5453f) % 1);
    }

    float lerp(float a, float b, float t) {
        return Mathf.Lerp(a, b, t);
    }

    float noise(Vector2 p)   //makes random tiles with bilinear interpolation to create smooth surface
    {
        Vector2 i = new Vector2(Mathf.Floor(p.x), Mathf.Floor(p.y));
        Vector2 u = new Vector2(Mathf.Abs(p.x % 1), Mathf.Abs(p.y % 1));
        u = new Vector2(u.x * u.x * (3.0f - 2.0f * u.x), u.y * u.y * (3.0f - 2.0f * u.y));
        Vector2 a = new Vector2(0.0f, 0.0f);
        Vector2 b = new Vector2(1.0f, 0.0f);
        Vector2 c = new Vector2(0.0f, 1.0f);
        Vector2 d = new Vector2(1.0f, 1.0f);
        float r = lerp(lerp(hash(i + a), hash(i + b), u.x), lerp(hash(i + c), hash(i + d), u.x), u.y);
        return r * r;
    }

    float fbm(Vector2 p)   //deforms tiles to get more organic looking surface
    {
        float f = 0.0f;
        f += 0.5000f * noise(p); p = p * 2.02f; p = new Vector2(p.x * m[0, 0] + p.y * m[0, 1], p.x * m[1, 0] + p.y * m[1, 1]);
        f += 0.2500f * noise(p); p = p * 2.03f; p = new Vector2(p.x * m[0, 0] + p.y * m[0, 1], p.x * m[1, 0] + p.y * m[1, 1]);
        f += 0.1250f * noise(p); p = p * 2.01f; p = new Vector2(p.x * m[0, 0] + p.y * m[0, 1], p.x * m[1, 0] + p.y * m[1, 1]);
        f += 0.0625f * noise(p);
        return f / 0.9375f;
    }

    void Start() {
        /*
        int triangleCount = mesh.triangles.Length;

        float r = Random.Range(0.0f, 100.0f);
        depthbuffer = new ComputeBuffer(number, sizeof(float), ComputeBufferType.Default);
        colorbuffer = new ComputeBuffer(number, sizeof(float) * 4, ComputeBufferType.Default);
        meshBuffer = new ComputeBuffer(number*triangleCount, sizeof(float) * 3, ComputeBufferType.Default);
        depth = new float[number];
        color = new Vector4[number];
        tris = new Vector3[number*triangleCount];
        int i = 0;
        int l = 0;
        for (int y = 0; y < 512; y++) {
            for (int x = 0; x < 512; x++) {
                Vector2 resolution = new Vector2(512, 512);
                Vector2 coordinates = new Vector2((float)x, (float)y);
                Vector2 uv = new Vector2((2.0f * coordinates.x - resolution.x) / resolution.y + 1.0f, (2.0f * coordinates.y - resolution.y) / resolution.y + 1.0f);
                ushort h = System.Convert.ToUInt16((fbm(new Vector2(uv.x * 5.0f + r, uv.y * 5.0f + r)) + 0.1f) * ushort.MaxValue);
                depth[i] = (float)((float)h / (float)ushort.MaxValue);
                if (depth[i] < 0.1)
                    color[i] = new Vector4(0.77f, 0.90f, 0.98f, 1.0f);
                else
                if (depth[i] < 0.2)
                    color[i] = new Vector4(0.82f, 0.92f, 0.99f, 1.0f);
                else
                if (depth[i] < 0.3)
                    color[i] = new Vector4(0.91f, 0.97f, 0.99f, 1.0f);
                else
                if (depth[i] < 0.45)
                    color[i] = new Vector4(0.62f, 0.75f, 0.59f, 1.0f);
                else
                if (depth[i] < 0.55)
                    color[i] = new Vector4(0.86f, 0.90f, 0.68f, 1.0f);
                else
                if (depth[i] < 0.65)
                    color[i] = new Vector4(0.99f, 0.99f, 0.63f, 1.0f);
                else
                if (depth[i] < 0.75)
                    color[i] = new Vector4(0.99f, 0.83f, 0.59f, 1.0f);
                else
                if (depth[i] < 0.90)
                    color[i] = new Vector4(0.98f, 0.71f, 0.49f, 1.0f);
                else
                if (depth[i] < 0.95)
                    color[i] = new Vector4(0.98f, 0.57f, 0.47f, 1.0f);
                else
                    color[i] = new Vector4(0.79f, 0.48f, 0.43f, 1.0f);
                i++;//i+=triangleCount
                
                Vector3 pos = new Vector3(x*2, (float)((float)h / (float)ushort.MaxValue), y*2);
                for (int n = 0; n < mesh.triangles.Length;n++) {
                    //Vector3 pos = new Vector3(x, y, (float)((float)h / (float)ushort.MaxValue));
                    tris[l] = pos +mesh.vertices[mesh.triangles[n]]; //vector3 -> float, float, float (x,y,z)
                    //tris[l+1] = pos +mesh.vertices[mesh.triangles[n + 1]];
                    //tris[l +2] = pos +mesh.vertices[mesh.triangles[n + 2]];
                    l ++;
                }
            }
        }
        depthbuffer.SetData(depth);
        colorbuffer.SetData(color);
        meshBuffer.SetData(tris);
        for (int n = 0; n < mesh.triangles.Length / 3; n += 3) {
          Debug.Log(mesh.vertices[mesh.triangles[n]]);
          Debug.Log(mesh.vertices[mesh.triangles[n+1]]);
          Debug.Log(mesh.vertices[mesh.triangles[n+2]]);
        }

        Camera.onPostRender = PostRender;
        */
    }

    public float scale = 0.05f;
    public void SetupPointCloudShader(Vector3[] points) {
        int triangleCount = mesh.triangles.Length;
        int count = points.Length * triangleCount;

        meshBuffer = new ComputeBuffer(count, sizeof(float) * 3, ComputeBufferType.Default);
        colorbuffer = new ComputeBuffer(count, sizeof(float) * 4, ComputeBufferType.Default);

        tris = new Vector3[count];
        color = new Vector4[count];
        int i = 0;
        for(int k = 0; k < points.Length; k++) {
            for (int n = 0; n < mesh.triangles.Length; n++) {
                tris[i] = points[k] + mesh.vertices[mesh.triangles[n]]*scale;
                color[i] = ColorSelector(tris[i].y);
                i++;
            }
        }
        meshBuffer.SetData(tris);
        colorbuffer.SetData(color);

        Camera.onPostRender = PostRender;
        //PostRender(Camera.main);
    }

    Vector4 ColorSelector(float h) {
        Vector4 color;
        if (h < 0.1)color = new Vector4(0.77f, 0.90f, 0.98f, 1.0f);
        else if (h < 0.2) color = new Vector4(0.82f, 0.92f, 0.99f, 1.0f);
        else if (h < 0.3)color = new Vector4(0.91f, 0.97f, 0.99f, 1.0f);
        else if (h < 0.45)color = new Vector4(0.62f, 0.75f, 0.59f, 1.0f);
        else if (h < 0.55) color = new Vector4(0.86f, 0.90f, 0.68f, 1.0f);
        else if (h < 0.65) color = new Vector4(0.99f, 0.99f, 0.63f, 1.0f);
        else if (h < 0.75)color = new Vector4(0.99f, 0.83f, 0.59f, 1.0f);
        else if (h < 0.90)color = new Vector4(0.98f, 0.71f, 0.49f, 1.0f);
        else if (h < 0.95) color = new Vector4(0.98f, 0.57f, 0.47f, 1.0f);
        else color = new Vector4(0.79f, 0.48f, 0.43f, 1.0f);
        return color;
    }

    void PostRender(Camera cam) {
        
        material.SetPass(0);
        material.SetBuffer("depthbuffer", depthbuffer);
        material.SetBuffer("colorbuffer", colorbuffer);
        material.SetBuffer("meshBuffer", meshBuffer);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, number* mesh.triangles.Length, 1);
        //Graphics.DrawMeshInstancedProcedural(mesh, 0, material, new Bounds(Vector3.zero, Vector3.one * 100f), number);
    }
    void OnDestroy() {
        depthbuffer.Release();
        colorbuffer.Release();
        meshBuffer.Release();
        depthbuffer = null;
        colorbuffer = null;
        meshBuffer = null;
    }
}
