using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ThirdPersonCamera
{
    public class HUDHandler
    {
        private Main parent;
        private bool _isPilotingShip = false;

        public HUDHandler(Main _main)
        {
            parent = _main;

            GlobalMessenger.AddListener("ActivateThirdPersonCamera", new Callback(OnActivateThirdPersonCamera));
            GlobalMessenger.AddListener("DeactivateThirdPersonCamera", new Callback(OnDeactivateThirdPersonCamera));
            GlobalMessenger.AddListener("PutOnHelmet", new Callback(OnPutOnHelmet));
            GlobalMessenger.AddListener("RemoveHelmet", new Callback(OnRemoveHelmet));
            GlobalMessenger.AddListener("ExitFlightConsole", new Callback(OnExitFlightConsole));
            GlobalMessenger<OWRigidbody>.AddListener("EnterFlightConsole", new Callback<OWRigidbody>(OnEnterFlightConsole));
        }

        private void OnActivateThirdPersonCamera()
        {
            ShowHelmetHUD(!_isPilotingShip);
            ShowReticule(_isPilotingShip);
            ShowMarkers(true);
        }

        private void OnDeactivateThirdPersonCamera()
        {
            ShowHelmetHUD(false);
            ShowReticule(true);
            ShowMarkers(false);
        }

        public void OnPutOnHelmet()
        {
            ShowHelmetHUD(parent.IsThirdPerson());
        }

        public void OnRemoveHelmet()
        {
            ShowHelmetHUD(false);
        }

        private void OnExitFlightConsole()
        {
            _isPilotingShip = false;
            ShowReticule(!parent.IsThirdPerson());
        }

        private void OnEnterFlightConsole(OWRigidbody _)
        {
            _isPilotingShip = true;
            ShowHelmetHUD(false);
            ShowReticule(true);
        }

        private void ShowHelmetHUD(bool visible)
        {
            // Change the HUD
            if (Locator.GetPlayerSuit().IsWearingHelmet() || (_isPilotingShip && Locator.GetPlayerSuit().IsWearingSuit()))
            {
                Canvas UICanvas = GameObject.Find("PlayerHUD/HelmetOnUI/UICanvas").GetComponent<Canvas>();
                UICanvas.renderMode = !visible ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
            }
            else
            {
                foreach (Canvas canvas in GameObject.Find("PlayerHUD/HelmetOffUI").GetComponentsInChildren<Canvas>())
                {
                    canvas.worldCamera = !visible ? Locator.GetPlayerCamera().mainCamera : ThirdPersonCamera.GetCamera();
                }
            }

            // Get rid of 2D helmet stuff
            GameObject.Find("Helmet").transform.localScale = parent.IsThirdPerson() ? new Vector3(0, 0, 0) : new Vector3(1, 1, 1);
        }

        private void ShowReticule(bool visible)
        {
            GameObject reticule = GameObject.Find("Reticule");
            if(visible)
            {
                reticule.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            }
            else
            {
                reticule.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
                reticule.GetComponent<Canvas>().worldCamera = Locator.GetPlayerCamera().mainCamera;
            }
        }

        private void ShowMarkers(bool thirdPerson)
        {
            foreach (string s in new string[] { "CanvasMarker(Clone)", "CanvasMarkerManager" })
            {
                Canvas c = GameObject.Find(s)?.GetComponentInChildren<Canvas>();
                if (c != null) c.worldCamera = thirdPerson ? ThirdPersonCamera.GetCamera() : Locator.GetPlayerCamera().mainCamera;
            }

            /*
            string[] strings = { "MapLockOnCanvas", "CockpitLockOnCanvas", "MarkerManager" };
            foreach(string s in strings)
            {
                Canvas c = GameObject.Find(s)?.GetComponentInChildren<Canvas>();
                if (c != null) c.renderMode = thirdPerson ? RenderMode.ScreenSpaceOverlay : RenderMode.ScreenSpaceCamera;
            }
            */

            /*
            foreach(Canvas c in GameObject.FindObjectsOfType<Canvas>())
            {
                parent.WriteInfo($"{c.name}, {c.renderMode}, {c.gameObject.name}, {c.worldCamera?.name}");
            }
            */
        }
    }
}
