using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit;
using System;
using Microsoft.MixedReality.Toolkit.UI;
using Pcx;

public class SelectorHandler : MonoBehaviour, IMixedRealityInputHandler {
    public GameObject marker;
    public GPUPC pcHolder;

    public float radius = 2f;
    public float sigmaGaussian = 2f;

    public GameObject menuContent;

    public PointCloudRenderer rnd;

    private float movementSpeed = 0.1f;
    private float deadzone = 0.05f;

    private float resolution = 0.1f;
    private float xlength = 164;
    private float zlength = 174;


    private List<GameObject> currentSelection;  //also keep position in array ->
    private IMixedRealityInputSource currentPointer;
    private Vector3 initialPosition;
    private bool editPoints = false;

    public EditorState editorState = EditorState.Constant;

    public enum EditorState {
        Constant,
        Linear,
        Gaussian
    }

    public void UpdateRadius(SliderEventData sliderEvent) {
        radius = sliderEvent.NewValue*10+0.01f;//scaling factor from radius 0.01-10.01
    }
    public void UpdateSigma(SliderEventData sliderEvent) {
        radius = sliderEvent.NewValue * 2 + 0.01f;//scaling factor from radius 0.01-2.01
    }

    private void Update() {
        if(currentPointer != null && editPoints) {
            float amount_moved = (currentPointer.Pointers[0].Position - initialPosition).y;
            amount_moved *= movementSpeed;

            foreach (GameObject node in currentSelection) {
                Vector3 nodePos = node.transform.position;
                float distance_to_orig_position = Mathf.Sqrt(Mathf.Pow((initialPosition.x - nodePos.x), 2) + Mathf.Pow((initialPosition.z - nodePos.z), 2));
                float deltaY;
                if(editorState == EditorState.Constant) {
                    deltaY = amount_moved;
                }else if(editorState == EditorState.Linear) {
                    deltaY = amount_moved*(radius- distance_to_orig_position)/radius;
                    /*if (distance_to_orig_position < smallRadius) {
                        deltaY = amount_moved * (smallRadius - distance_to_orig_position) / smallRadius;
                    } else {
                        deltaY = 0;
                    }*/
                } else {
                    deltaY = amount_moved * Mathf.Exp(-Mathf.Pow(distance_to_orig_position, 2) / sigmaGaussian);
                }

                node.transform.position += new Vector3(0, deltaY, 0);

            }
        }
    }

    public void OnInputDown(InputEventData eventData) {
        if (eventData.MixedRealityInputAction.Description == "Select" && !menuContent.activeSelf) {
            //Debug.Log("Select");
            initialPosition = eventData.InputSource.Pointers[0].Position; // The initial position is reset such that on retry we center dead-zone again
            editPoints = true;
            if(currentPointer == null) {
                currentPointer = eventData.InputSource;
                currentSelection = createPointsWithinRadius(radius, initialPosition);
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

    // Handling deletion confirmation of new points
    public void AcceptPoints() {
        Vector3[] verts = pcHolder.GetVertices();
        foreach (GameObject go in currentSelection) {
            int index = Int32.Parse(go.name);
            //Debug.Log(verts[index] + "  -->  " + go.transform.position);
            verts[index] = go.transform.position;
        }

        pcHolder.UpdatePointCloud(verts);
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
        } else if(shape == 1) {
            editorState = EditorState.Linear;
        } else {
            editorState = EditorState.Gaussian;
        }
    }


    //--------- Helpers
    private Tuple<Vector3,int> posToArray(Vector3 startPos) {
        Vector3[] verts = pcHolder.GetVertices();
        Vector3 displacement = startPos - verts[0];
        int xindex = (int)(displacement.x * 2); //TODO make this more general!
        int zindex = (int)(displacement.z * 2);

        int index = xyToI(xindex, zindex);
        if (index >= 0 && index < verts.Length) {
            return new Tuple<Vector3,int>(verts[index],index);
        } else {
            Debug.LogError("Index out of range!");
            return new Tuple<Vector3, int>(Vector3.zero,0); //TODO properly handle errors
        }
    }
    private int xyToI(int x, int z) {
        return (int)(x + 164 * z)+1;
    }

    private List<GameObject> createPointsWithinRadius(float radius, Vector3 center) {
        
        List<GameObject> ret = new List<GameObject>();
        HashSet<Vector3> avoidDuplicates = new HashSet<Vector3>();

        for(int i = (int)(-radius / resolution); i< radius/resolution; i++) {
            for (int j = (int)(-radius / resolution); j < radius / resolution; j++) {
                Vector3 pos = center;
                pos.x += i * resolution;
                pos.z += j * resolution;
                Tuple<Vector3,int> newpos = posToArray(pos);
                pos = newpos.Item1;
                if (!avoidDuplicates.Contains(pos)) {
                    avoidDuplicates.Add(pos);
                    if( Mathf.Sqrt(Mathf.Pow(center.x-pos.x,2)+ Mathf.Pow(center.z - pos.z, 2)) < radius) {
                        GameObject node = GameObject.Instantiate(marker, pos, Quaternion.identity, transform);
                        node.name = newpos.Item2.ToString();
                        ret.Add(node);
                    }
                }
            }
        }

        return ret;
    }
}
