﻿#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

// Usage: Attach to gameobject, assign target gameobject (from where the mesh is taken), Run, Press savekey

public class SaveMeshInEditor : MonoBehaviour
{

    public KeyCode saveKey = KeyCode.F12;
    public string saveName = "SavedMesh";

    void Update()
    {
        if (Input.GetKeyDown(saveKey))
        {
            SaveAsset();
        }
    }

    void SaveAsset()
    {
        var mf = gameObject.GetComponent<MeshFilter>();
        if (mf)
        {
            var savePath = "Assets/SavedMeshes/" + saveName + ".asset";
            Debug.Log("Saved Mesh to:" + savePath);
            AssetDatabase.CreateAsset(mf.mesh, savePath);
        }
    }
}
#endif