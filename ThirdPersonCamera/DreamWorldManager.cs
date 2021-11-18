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

        private bool _created = false;

        private readonly Main parent;
        public DreamWorldManager(Main _main)
        {
            parent = _main;
        }

        public void Init()
        {
            GlobalMessenger<OWCamera>.AddListener("SwitchActiveCamera", OnSwitchActiveCamera);
            try
            {
                Locator.GetDreamWorldController().OnExitLanternBounds += OnExitLanternBounds;
                Locator.GetDreamWorldController().OnEnterLanternBounds += OnEnterLanternBounds;
                _created = true;
            } 
            catch(Exception)
            {
                parent.WriteError("Couldn't find DreamWorldController");
            }
        }

        public void OnDestroy()
        {
            GlobalMessenger<OWCamera>.RemoveListener("SwitchActiveCamera", OnSwitchActiveCamera);
            try
            {
                Locator.GetDreamWorldController().OnExitLanternBounds -= OnExitLanternBounds;
                Locator.GetDreamWorldController().OnEnterLanternBounds -= OnEnterLanternBounds;
            }
            catch(Exception)
            {
                parent.WriteError("DreamWorldController already gone");
            }

            parent.WriteLine($"Done destroying {nameof(DreamWorldManager)}", MessageType.Success);
        }

        private void OnSwitchActiveCamera(OWCamera _camera)
        {
            parent.WriteInfo($"Now using camera {_camera.name}");
        }

        private void OnEnterLanternBounds()
        {
            SetMatrixMode(false);
        }

        private void OnExitLanternBounds()
        {
            SetMatrixMode(true);
        }

        private void SetMatrixMode(bool enableMatrix)
        {
            parent.WriteLine($"{(enableMatrix ? "Entering" : "Exiting")} matrix", MessageType.Info);

            inMatrix = enableMatrix;

            // You like weirdly see yourself inside the lantern radius so I'll make the player character invisible
            try
            {
                Locator.GetPlayerBody().GetComponentInChildren<MeshRenderer>().enabled = !enableMatrix;
            }
            catch (Exception)
            {
                parent.WriteLine($"Couldn't find the player", MessageType.Error);
            }
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
