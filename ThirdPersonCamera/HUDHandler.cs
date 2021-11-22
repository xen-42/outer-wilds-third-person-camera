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
        private bool _isPilotingShip = false;
        private bool _checkCockpitLockOnNextTick = false;

        public HUDHandler()
        {
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
            ShowCockpitLockOn(_isPilotingShip);
        }

        private void OnDeactivateThirdPersonCamera()
        {
            ShowHelmetHUD(false);
            ShowReticule(true);
            ShowMarkers(false);
            ShowCockpitLockOn(false);
        }

        public void OnPutOnHelmet()
        {
            ShowHelmetHUD(Main.IsThirdPerson());
        }

        public void OnRemoveHelmet()
        {
            ShowHelmetHUD(false);
        }

        private void OnExitFlightConsole()
        {
            _isPilotingShip = false;
            ShowReticule(!Main.IsThirdPerson());
            ShowMarkers(Main.IsThirdPerson());
            ShowCockpitLockOn(false);
        }

        private void OnEnterFlightConsole(OWRigidbody _)
        {
            _isPilotingShip = true;
            ShowHelmetHUD(false);
            ShowReticule(true);
            ShowMarkers(Main.IsThirdPerson());

            // Set it next frame or it doesnt work sometimes idk
            _checkCockpitLockOnNextTick = true;
            ShowCockpitLockOn(Main.IsThirdPerson());
        }

        private void ShowHelmetHUD(bool visible)
        {
            Canvas UICanvas = GameObject.Find("PlayerHUD/HelmetOnUI/UICanvas").GetComponent<Canvas>();
            UICanvas.renderMode = visible && Locator.GetPlayerSuit().IsWearingHelmet() ? RenderMode.ScreenSpaceOverlay : RenderMode.ScreenSpaceCamera;
            foreach (Canvas canvas in GameObject.Find("PlayerHUD/HelmetOffUI").GetComponentsInChildren<Canvas>())
            {
                canvas.worldCamera = visible ? ThirdPersonCamera.GetCamera() : Locator.GetPlayerCamera().mainCamera;
            }

            // Get rid of 2D helmet stuff
            GameObject.Find("Helmet").transform.localScale = Main.IsThirdPerson() ? new Vector3(0, 0, 0) : new Vector3(1, 1, 1);
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
            Main.WriteInfo("Writing Canvas names");
            foreach(Canvas c in GameObject.FindObjectsOfType<Canvas>())
            {
                Main.WriteInfo($"{c.name}, {c.renderMode}, {c.gameObject.name}, {c.worldCamera?.name}");
            }
            */
        }

        private void ShowCockpitLockOn(bool visible)
        {
            Canvas c = GameObject.Find("CockpitLockOnCanvas")?.GetComponentInChildren<Canvas>();
            if (c != null) c.worldCamera = visible ? ThirdPersonCamera.GetCamera() : Locator.GetPlayerCamera().mainCamera;
        }

        public void Update()
        {
            if (_checkCockpitLockOnNextTick)
            {
                ShowCockpitLockOn(Main.IsThirdPerson());
            }
        }
    }
}
