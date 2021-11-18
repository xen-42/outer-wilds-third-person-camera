using OWML.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ThirdPersonCamera
{
    public class DreamWorldManager
    {
        private bool inMatrix = false;

        private readonly Main parent;
        public DreamWorldManager(Main _main)
        {
            parent = _main;
        }

        public void Init()
        {
            //GlobalMessenger<OWCamera>.AddListener("SwitchActiveCamera", OnSwitchActiveCamera);
            try
            {
                Locator.GetDreamWorldController().OnExitLanternBounds += OnExitLanternBounds;
                Locator.GetDreamWorldController().OnEnterLanternBounds += OnEnterLanternBounds;
            } 
            catch(Exception)
            {
                parent.WriteError("Couldn't find DreamWorldController");
            }
        }

        public void OnDestroy()
        {
            //GlobalMessenger<OWCamera>.RemoveListener("SwitchActiveCamera", OnSwitchActiveCamera);
            try
            {
                Locator.GetDreamWorldController().OnExitLanternBounds -= OnExitLanternBounds;
                Locator.GetDreamWorldController().OnEnterLanternBounds -= OnEnterLanternBounds;
            }
            catch(Exception)
            {
                parent.WriteError("DreamWorldController already gone");
            }

            parent.WriteSuccess($"Done destroying {nameof(DreamWorldManager)}");
        }

        /*
        private void OnSwitchActiveCamera(OWCamera _camera)
        {
            parent.WriteInfo($"Now using camera {_camera.name}");
        }
        */

        private void OnEnterLanternBounds()
        {
            SetMatrixMode(false);

            /*
            try
            {
                GameObject ghost = GameObject.FindObjectOfType<GhostController>().gameObject;

                parent.WriteInfo($"{ghost.layer}");

                MeshRenderer[] meshRenderers = ghost.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer meshRenderer in meshRenderers)
                {
                    parent.WriteInfo($"{meshRenderer.material.shader}, {meshRenderer.material.renderQueue}");
                }
            }
            catch (Exception)
            {
                parent.WriteLine($"Couldn't find a ghost", MessageType.Error);
            }
            */
        }

        private void OnExitLanternBounds()
        {
            SetMatrixMode(true);
        }

        private void SetMatrixMode(bool enableMatrix)
        {
            parent.WriteInfo($"{(enableMatrix ? "Entering" : "Exiting")} matrix");

            if (enableMatrix) parent.ThirdPersonCamera.DisableCamera();
            else parent.ThirdPersonCamera.EnableCamera();

            /*
            inMatrix = enableMatrix;

            // You like weirdly see yourself inside the lantern radius so I'll make the player character invisible
            try
            {
                if (enableMatrix) previousShaders = null;
                MeshRenderer[] meshRenderers = Locator.GetPlayerBody().GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer meshRenderer in meshRenderers)
                {
                    // When entering record all the shaders
                    if (enableMatrix)
                    {
                        previousShaders.Add(meshRenderer.name, meshRenderer.material.shader.name);
                        meshRenderer.material.shader = Shader.Find("Outer Wilds/Environment/Invisible Planet/Cyberspace Interactible");
                    }
                    else
                    {
                        // Give them back the old shaders
                        meshRenderer.material.shader = Shader.Find(previousShaders[meshRenderer.name]);
                    }
                }
            }
            catch (Exception)
            {
                parent.WriteLine($"Couldn't find the player", MessageType.Error);
            }
            */
        }

        public void Update()
        {
            if (inMatrix)
            {
                try
                {
                    // If we're in the matrix and are exiting the dream, make sure to disable the matrix mode
                    if (Locator.GetDreamWorldController().IsExitingDream()) SetMatrixMode(false);
                }
                catch(Exception)
                {
                    parent.WriteError("Couldn't check the DreamWorldController so assume we've left");
                    SetMatrixMode(false);
                }
            }
        }
    }
}
