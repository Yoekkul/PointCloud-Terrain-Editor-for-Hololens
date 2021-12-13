using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pcx;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit;

public class DenseSelector : MonoBehaviour, IMixedRealityInputHandler {

    public new PointCloudRenderer renderer;
    public GameObject EditMarker;
    public GameObject menuContent; // Used to ensure the edit menu is open


    public EditorState editorState = EditorState.Constant;
    private float movementSpeed = 0.1f;
    
    private int elementSize = sizeof(float) * 4;

    private float radius = 2f;
    private float sigmaGaussian = 2f;

    #region Public editing paramters
    public void UpdateRadius(SliderEventData sliderEvent) {
        radius = sliderEvent.NewValue * 10 + 0.01f;//scaling factor from radius 0.01-10.01
    }
    public void UpdateSigma(SliderEventData sliderEvent) {
        sigmaGaussian = sliderEvent.NewValue * 2 + 0.01f;//scaling factor from radius 0.01-2.01
    }
    #endregion

    void Start() {
        transform.position = Vector3.forward;//FIXME remove this
        //GetComponent<MeshFilter>().mesh = coneCreator(2f, 0.5f);

        //getPointsInEditArea(new Vector3(10, 2, 3), 2f);
    }

    void Update() {
        ComputeBuffer dab = renderer.sourceData.computeBuffer;
        int count = renderer.sourceData.pointCount;
        Point[] pts = new Point[count];
        dab.GetData(pts);

        for (int i = 0; i < 1000; i++) {
            //Debug.Log(pts[i].position);
            pts[i].position += Vector3.up * 0.1f;
        }
        dab.SetData(pts);


        /*
        List <Vector3> p = new List<Vector3>();

        List<Color32> col = new List<Color32>();

        p.Add(new Vector3(0,0,Mathf.Sin(Time.time)));
        col.Add(Color.green);

        PointCloudData data = ScriptableObject.CreateInstance<PointCloudData>();


        data.Initialize(p,col);
        renderer.sourceData = data;
        */

        //---------------------------- Selector editing -----------------------------------------------
        if (currentPointer != null && editPoints) {
            float amount_moved = (currentPointer.Pointers[0].Position - initialPosition).y;
            amount_moved *= movementSpeed;

            foreach (GameObject node in currentSelection) {
                Vector3 nodePos = node.transform.position;
                float distance_to_orig_position = Mathf.Sqrt(Mathf.Pow((initialPosition.x - nodePos.x), 2) + Mathf.Pow((initialPosition.z - nodePos.z), 2));
                float deltaY;
                if (editorState == EditorState.Constant) {
                    deltaY = amount_moved;
                } else if (editorState == EditorState.Linear) {
                    deltaY = amount_moved * (radius - distance_to_orig_position) / radius;
                } else {
                    deltaY = amount_moved * Mathf.Exp(-Mathf.Pow(distance_to_orig_position, 2) / sigmaGaussian);
                }

                node.transform.position += new Vector3(0, deltaY, 0);

            }
        }
    }

    #region Input handling

    private Vector3 initialPosition;
    private bool editPoints;
    private IMixedRealityInputSource currentPointer;
    private List<GameObject> currentSelection;
    private List<int> editedIndexes;
    private float distanceToCamera = 3f;

    public void OnInputDown(InputEventData eventData) {
        if (eventData.MixedRealityInputAction.Description == "Select" && !menuContent.activeSelf) {
            Debug.Log("Select");
            initialPosition = eventData.InputSource.Pointers[0].Position; // The initial position is reset such that on retry we center dead-zone again
            editPoints = true;
            if (currentPointer == null) {
                currentPointer = eventData.InputSource;
                //currentSelection = 
                createPointsWithinRadius(initialPosition+Camera.main.transform.forward*distanceToCamera, radius); // getPointsInEditArea(initialPosition, radius);
            }
        }
    }

    public void OnInputUp(InputEventData eventData) {
        editPoints = false;
        /*
        foreach(GameObject go in currentSelection) {
            Destroy(go);
        }
        currentPointer = null;*/

    }

    private void OnEnable() {
        CoreServices.InputSystem?.RegisterHandler<IMixedRealityInputHandler>(this);
    }

    private void OnDisable() {
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealityInputHandler>(this);
    }


    private List<GameObject> createPointsWithinRadius(Vector3 initialPosition,float radius) {


        getPointsInEditArea(initialPosition, radius);
        return null;
    }

    // Handling deletion confirmation of new points
    
    public void AcceptPoints() {

        ComputeBuffer dab = renderer.sourceData.computeBuffer;
        int count = renderer.sourceData.pointCount;
        Point[] pts = new Point[count];
        dab.GetData(pts);

        for (int i = 0; i < currentSelection.Count; i++) {
            pts[editedIndexes[i]].position = currentSelection[i].transform.position;
        }

        dab.SetData(pts);
        ClearPoints();
    }

    public void ClearPoints() {
        editPoints = false;
        foreach (GameObject go in currentSelection) {
            Destroy(go);
        }
        currentPointer = null;
    }

    public void SetShape(int shape) {
        if (shape == 0) {
            editorState = EditorState.Constant;
        } else if (shape == 1) {
            editorState = EditorState.Linear;
        } else {
            editorState = EditorState.Gaussian;
        }
    }


    #endregion

    //----------------------------------------------------------------------

    private List<int> getPointsInEditArea(Vector3 center, float radius) {
        ComputeBuffer dab = renderer.sourceData.computeBuffer;
        int count = renderer.sourceData.pointCount;
        Point[] pts = new Point[count];
        dab.GetData(pts);

        editedIndexes = new List<int>();
        currentSelection = new List<GameObject>();


        for (int i = 0; i < count; i++) {
            if (Mathf.Abs(center.x - pts[i].position.x) + Mathf.Abs(center.z - pts[i].position.z) < radius) {
                editedIndexes.Add(i);
                currentSelection.Add(GameObject.Instantiate(EditMarker, pts[i].position + Vector3.up, Quaternion.identity, transform));
            }
        }

        return editedIndexes;
    }

    #region Procedural Cone Generation

    int trianglesPerRad = 4;
    Mesh coneCreator(float height, float radius) {

        Mesh cone = new Mesh();
        cone.vertices = CalculateVertices(height, radius).ToArray();
        cone.triangles = CalculateTriangles().ToArray();
        //cone.uv = calculateUVs().ToArray();
        cone.RecalculateNormals();
        return cone;
    }
    /*
    protected List<Vector2> calculateUVs(){
        var triangleCount = GetTriangleCount();
        var uvs = new List<Vector2>();

        for (int i = 0; i < triangleCount/3; i++) {
            float theta = i / (float)triangleCount * 2 * Mathf.PI;
            var vertex = new Vector3(Mathf.Cos(theta) * radius, 0, Mathf.Sin(theta) * radius);
            vertices.Add(vertex);
        }

        return uvs;
    }*/


    protected List<Vector3> CalculateVertices(float height, float radius) {
        var triangleCount = GetTriangleCount();
        var vertices = new List<Vector3>();

        //vertices.Add(Vector2.zero);

        for (int i = 0; i < triangleCount; i+=3) {
            vertices.Add(new Vector3(0, height, 0));

            float theta = i+1 / (float)triangleCount * 2 * Mathf.PI;
            var vertex = new Vector3(Mathf.Cos(theta)*radius,0, Mathf.Sin(theta)*radius);
            vertices.Add(vertex);

            theta = i + 2 / (float)triangleCount * 2 * Mathf.PI;
            vertex = new Vector3(Mathf.Cos(theta) * radius, 0, Mathf.Sin(theta) * radius);
            vertices.Add(vertex);

        }

        return vertices;
    }
    protected List<int> CalculateTriangles() {
        var triangleCount = GetTriangleCount();
        var triangles = new List<int>();

        for (int i = 0; i < triangleCount-2; i+=3) {
            int index0 = i;
            int index1 = i + 1;
            int index2 = i + 2;


            
            if (i == triangleCount - 1) {
                index2 = 1; //second vertex of last triangle is vertex1
            }
            
            triangles.Add(index0);
            triangles.Add(index2);
            triangles.Add(index1);
        }

        return triangles;
    }

    private int GetTriangleCount() {
        return Mathf.CeilToInt(3 * Mathf.PI * trianglesPerRad);
    }

    #endregion

    public enum EditorState {
        Constant,
        Linear,
        Gaussian
    }

    struct Point {
        public Vector3 position;
        public uint color;
    }
}
