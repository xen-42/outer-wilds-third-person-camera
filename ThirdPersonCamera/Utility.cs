using OWML.ModHelper;
using OWML.Common;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ThirdPersonCamera
{
    class Utility
    {
        public static GameObject[] FindGameObjectsWithLayer(int layer)
        {
            List<GameObject> objects = new List<GameObject>();
            foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>())
            {
                if (go.layer == layer) objects.Add(go);
            }
            if (objects.Count == 0) return null;
            return objects.ToArray();
        }

        public static string GetPath(Transform current)
        {
            if (current.parent == null) return "/" + current.name;
            return GetPath(current.parent) + "/" + current.name;
        }

        public static void ChangeLayersRecursively(Transform transform, int layer)
        {
            transform.gameObject.layer = layer;
            foreach(Transform child in transform)
            {
                ChangeLayersRecursively(child, layer);
            }
        }
    }
}
