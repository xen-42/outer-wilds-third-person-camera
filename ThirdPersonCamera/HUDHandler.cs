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
        private bool _checkCockpitLockOnNextTick = false;
        private Canvas[] _helmetOffUI;
        private GameObject _helmet;
        private GameObject _lightFlickerEffectBubble;
        private GameObject _darkMatterBubble;

        public HUDHandler()
        {
            GlobalMessenger.AddListener("PutOnHelmet", new Callback(OnPutOnHelmet));
            GlobalMessenger.AddListener("RemoveHelmet", new Callback(OnRemoveHelmet));
            GlobalMessenger.AddListener("ExitFlightConsole", new Callback(OnExitFlightConsole));
            GlobalMessenger<OWRigidbody>.AddListener("EnterFlightConsole", new Callback<OWRigidbody>(OnEnterFlightConsole));
            GlobalMessenger<OWCamera>.AddListener("SwitchActiveCamera", new Callback<OWCamera>(OnSwitchActiveCamera));
        }

        public void OnDestroy()
        {
            GlobalMessenger.RemoveListener("PutOnHelmet", new Callback(OnPutOnHelmet));
            GlobalMessenger.RemoveListener("RemoveHelmet", new Callback(OnRemoveHelmet));
            GlobalMessenger.RemoveListener("ExitFlightConsole", new Callback(OnExitFlightConsole));
            GlobalMessenger<OWRigidbody>.RemoveListener("EnterFlightConsole", new Callback<OWRigidbody>(OnEnterFlightConsole));
            GlobalMessenger<OWCamera>.RemoveListener("SwitchActiveCamera", new Callback<OWCamera>(OnSwitchActiveCamera));
        }

        public void Init()
        {
            _helmetOffUI = GameObject.Find("PlayerHUD/HelmetOffUI")?.GetComponentsInChildren<Canvas>();
            _helmet = GameObject.Find("Helmet");
        }

        private void OnSwitchActiveCamera(OWCamera camera)
        {
            if(camera.name == "ThirdPersonCamera")
            {
                ShowHelmetHUD(!PlayerState.AtFlightConsole());
                ShowReticule(PlayerState.AtFlightConsole());
                ShowMarkers(true);
                ShowCockpitLockOn(PlayerState.AtFlightConsole());
            }
            else
            {
                ShowHelmetHUD(false);
                ShowReticule(true);
                ShowMarkers(false);
                ShowCockpitLockOn(false);
            }
        }

        public void OnPutOnHelmet()
        {
            ShowHelmetHUD(Main.IsThirdPerson());
        }

        public void OnRemoveHelmet()
        {
            ShowHelmetHUD(!PlayerState.AtFlightConsole() && Main.IsThirdPerson());
        }

        private void OnExitFlightConsole()
        {
            ShowReticule(!Main.IsThirdPerson());
            ShowMarkers(Main.IsThirdPerson());
            ShowHelmetHUD(Main.IsThirdPerson());
            ShowCockpitLockOn(false);
        }

        private void OnEnterFlightConsole(OWRigidbody _)
        {
            ShowHelmetHUD(false);
            ShowReticule(true);
            ShowMarkers(Main.IsThirdPerson());

            // Set it next frame or it doesnt work sometimes idk
            _checkCockpitLockOnNextTick = true;
            ShowCockpitLockOn(Main.IsThirdPerson());
        }

        private void ShowHelmetHUD(bool visible)
        {
            if(_helmetOffUI != null) foreach (Canvas canvas in _helmetOffUI)
            {
                canvas.worldCamera = visible ? ThirdPersonCamera.GetCamera() : Locator.GetPlayerCamera().mainCamera;
            }
            
            if (_helmet != null)
            {
                // Reparent the HUDCamera stuff
                _helmet.transform.parent = Main.IsThirdPerson() ? ThirdPersonCamera.GetCamera().transform : Locator.GetPlayerCamera().transform;
                _helmet.transform.localPosition = Vector3.zero;

                // Get rid of 2D helmet stuff
                _helmet.transform.Find("HelmetRoot/HelmetMesh/HUD_Helmet_v2/Helmet").transform.localScale = Main.IsThirdPerson() ? new Vector3(0, 0, 0) : new Vector3(1, 1, 1);
                _helmet.transform.Find("HelmetRoot/HelmetMesh/HUD_Helmet_v2/HelmetFrame").transform.localScale = Main.IsThirdPerson() ? new Vector3(0, 0, 0) : new Vector3(1, 1, 1);
                _helmet.transform.Find("HelmetRoot/HelmetMesh/HUD_Helmet_v2/Scarf").transform.localScale = Main.IsThirdPerson() ? new Vector3(0, 0, 0) : new Vector3(1, 1, 1);
            }

            // Put bubble effects on the right camera
            if (_darkMatterBubble == null)
            {
                try
                {
                    _darkMatterBubble = Locator.GetPlayerCamera().transform.Find("ScreenEffects/DarkMatterBubble").gameObject;
                }
                catch(Exception)
                {
                    Main.WriteWarning("Couldn't find DarkMatterBubble");
                }
            }
            if (_darkMatterBubble != null)
            {
                _darkMatterBubble.transform.parent = Main.IsThirdPerson() ? ThirdPersonCamera.GetCamera().transform : Locator.GetPlayerCamera().transform;
                _darkMatterBubble.transform.localPosition = Vector3.zero;
            }


            if (_lightFlickerEffectBubble == null) 
            {
                try
                {
                    _lightFlickerEffectBubble = Locator.GetPlayerCamera().transform.Find("ScreenEffects/LightFlickerEffectBubble").gameObject;
                }
                catch(Exception)
                {
                    Main.WriteWarning("Couldn't find LightFlickerEffectBubble");
                }
            }
            if (_lightFlickerEffectBubble != null)
            {
                _lightFlickerEffectBubble.transform.parent = Main.IsThirdPerson() ? ThirdPersonCamera.GetCamera().transform : Locator.GetPlayerCamera().transform;
                _lightFlickerEffectBubble.transform.localPosition = Vector3.zero;
            }
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
