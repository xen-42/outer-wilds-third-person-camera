using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ThirdPersonCamera
{
    public class UIHandler
    {
        public static Text ShipText { get; private set; }
        public static Text TranslatorText { get; private set; }

        public static GameObject SigScopeReticuleParent;

        private static Image _shipProbeLauncherImage;

        private static Text _signalScopeText;
        private static Text _signalScopeDistanceText;
        private static LineRenderer _waveformRenderer;

        private bool _isTranslatorEquiped = false;
        private bool _isSignalScopeEquiped = false;

        private static bool _isShipProbeLauncherPictureTaken = false;
        private static bool _isShipProbeLauncherEquiped = false;

        private Vector3 _shipSigScopeLocalScale = Vector3.zero;

        private bool initialized = false;

        public UIHandler()
        {
            GlobalMessenger<PlayerTool>.AddListener("OnEquipTool", new Callback<PlayerTool>(OnToolEquiped));
            GlobalMessenger<PlayerTool>.AddListener("OnUnequipTool", new Callback<PlayerTool>(OnToolUnequiped));
            GlobalMessenger.AddListener("DeactivateThirdPersonCamera", new Callback(OnDeactivateThirdPersonCamera));
            GlobalMessenger.AddListener("ExitFlightConsole", new Callback(OnExitFlightConsole));
            GlobalMessenger<OWRigidbody>.AddListener("EnterFlightConsole", new Callback<OWRigidbody>(OnEnterFlightConsole));
            GlobalMessenger.AddListener("Probe Snapshot Removed", new Callback(OnProbeSnapshotRemoved));
            GlobalMessenger<OWCamera>.AddListener("SwitchActiveCamera", new Callback<OWCamera>(OnSwitchActiveCamera));
            GlobalMessenger.AddListener("GamePaused", new Callback(OnGamePaused));
            GlobalMessenger.AddListener("GameUnpaused", new Callback(OnGameUnpaused));
        }

        public void OnDestroy()
        {
            GlobalMessenger<PlayerTool>.RemoveListener("OnEquipTool", new Callback<PlayerTool>(OnToolEquiped));
            GlobalMessenger<PlayerTool>.RemoveListener("OnUnequipTool", new Callback<PlayerTool>(OnToolUnequiped));
            GlobalMessenger.RemoveListener("DeactivateThirdPersonCamera", new Callback(OnDeactivateThirdPersonCamera));
            GlobalMessenger.RemoveListener("ExitFlightConsole", new Callback(OnExitFlightConsole));
            GlobalMessenger<OWRigidbody>.RemoveListener("EnterFlightConsole", new Callback<OWRigidbody>(OnEnterFlightConsole));
            GlobalMessenger.RemoveListener("Probe Snapshot Removed", new Callback(OnProbeSnapshotRemoved));
            GlobalMessenger<OWCamera>.RemoveListener("SwitchActiveCamera", new Callback<OWCamera>(OnSwitchActiveCamera));
            GlobalMessenger.RemoveListener("GamePaused", new Callback(OnGamePaused));
            GlobalMessenger.RemoveListener("GameUnpaused", new Callback(OnGameUnpaused));
        }

        public void Init()
        {
            Font font = GameObject.Find("PlayerHUD/HelmetOnUI/UICanvas/SecondaryGroup/GForce/NumericalReadout/GravityText").GetComponent<Text>().font;

            // Canvas
            GameObject canvasObject = new GameObject();
            canvasObject.name = "ThirdPersonCanvas";
            canvasObject.AddComponent<Canvas>();
            canvasObject.transform.SetParent(ThirdPersonCamera.GetCamera().transform);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            // Ship Text
            GameObject shipTextObject = new GameObject();
            shipTextObject.transform.parent = canvasObject.transform;
            shipTextObject.name = "ShipConsoleText";

            ShipText = shipTextObject.AddComponent<Text>();
            ShipText.font = font;
            ShipText.text = "";
            ShipText.fontSize = 24;
            ShipText.alignment = TextAnchor.UpperCenter;

            // Text position
            RectTransform shipTextRectTransform = ShipText.GetComponent<RectTransform>();
            shipTextRectTransform.localPosition = new Vector3(0, Screen.height / 4f - 48, 0);
            shipTextRectTransform.sizeDelta = new Vector2(Screen.width, Screen.height / 2f);

            // Nomai Text
            GameObject myText2 = new GameObject();
            myText2.transform.parent = canvasObject.transform;
            myText2.name = "TranslatorText";

            TranslatorText = myText2.AddComponent<Text>();
            TranslatorText.font = font;
            TranslatorText.text = "";
            TranslatorText.fontSize = 32;
            TranslatorText.alignment = TextAnchor.UpperCenter;

            // Text position
            RectTransform translatorRectTransform = TranslatorText.GetComponent<RectTransform>();
            translatorRectTransform.localPosition = new Vector3(0, Screen.height / 4f - 48, 0);
            translatorRectTransform.sizeDelta = new Vector2(Screen.width * 0.6f, Screen.height / 2f);

            // Start inactive
            ShipText.gameObject.SetActive(false);
            TranslatorText.gameObject.SetActive(false);

            // Ship probe camera
            GameObject imgObject = new GameObject("ShipProbeLauncherImage");

            RectTransform probeLauncherRectTransform = imgObject.AddComponent<RectTransform>();
            probeLauncherRectTransform.transform.SetParent(canvas.transform);
            probeLauncherRectTransform.localScale = Vector3.one;
            probeLauncherRectTransform.anchoredPosition = new Vector2(600f, 250f);
            probeLauncherRectTransform.sizeDelta = new Vector2(400, 400);

            _shipProbeLauncherImage = imgObject.AddComponent<Image>();
            imgObject.transform.SetParent(canvas.transform);
            imgObject.SetActive(false);

            // SignalScope reticle
            SigScopeReticuleParent = new GameObject();
            Transform parent = ThirdPersonCamera.GetCamera().transform;
            SigScopeReticuleParent.transform.parent = parent;
            SigScopeReticuleParent.transform.rotation = parent.rotation;
            SigScopeReticuleParent.transform.position = parent.position + parent.TransformDirection(Vector3.forward) * 2f;

            // Signalscope line renderer
            GameObject _waveFormGameObject = new GameObject();
            _waveFormGameObject.transform.SetParent(ThirdPersonCamera.GetCamera().gameObject.transform);
            _waveFormGameObject.transform.position = _waveFormGameObject.transform.parent.position
                    + _waveFormGameObject.transform.parent.TransformDirection(Vector3.forward) * 200f
                    + _waveFormGameObject.transform.parent.TransformDirection(Vector3.down) * 120f;
            _waveFormGameObject.transform.rotation = _waveFormGameObject.transform.parent.rotation;
            _waveFormGameObject.name = "ShipSignalScopeWaveform";

            var material = GameObject.Find("/Ship_Body/Module_Cockpit/Systems_Cockpit/ShipCockpitUI/SignalScreen/SignalScreenPivot/SigScopeDisplay").GetComponentInChildren<LineRenderer>().material;

            _waveformRenderer = _waveFormGameObject.AddComponent<LineRenderer>();
            _waveformRenderer.positionCount = 256;
            _waveformRenderer.material = material;
            _waveformRenderer.useWorldSpace = false;

            // SignalScope Text
            GameObject myText3 = new GameObject();
            myText3.transform.SetParent(canvas.transform);
            myText3.name = "ShipSignalScopeText";

            _signalScopeText = myText3.AddComponent<Text>();
            _signalScopeText.font = font;
            _signalScopeText.text = "";
            _signalScopeText.fontSize = 24;
            _signalScopeText.alignment = TextAnchor.UpperCenter;

            // Text position
            RectTransform signalscopeRectTransform = _signalScopeText.GetComponent<RectTransform>();
            signalscopeRectTransform.localPosition = new Vector3(0, -Screen.height / 1.7f, 0);
            signalscopeRectTransform.sizeDelta = new Vector2(Screen.width * 0.6f, Screen.height / 2f);

            // SignalScope Text
            GameObject myText4 = new GameObject();
            myText4.transform.SetParent(canvas.transform);
            myText4.name = "ShipSignalScopeDistanceText";

            _signalScopeDistanceText = myText4.AddComponent<Text>();
            _signalScopeDistanceText.font = font;
            _signalScopeDistanceText.text = "";
            _signalScopeDistanceText.fontSize = 32;
            _signalScopeDistanceText.alignment = TextAnchor.UpperCenter;

            // Text position
            RectTransform signalscopeDistanceRectTransform = _signalScopeDistanceText.GetComponent<RectTransform>();
            signalscopeDistanceRectTransform.localPosition = new Vector3(0, -Screen.height / 3f + 32, 0);
            signalscopeDistanceRectTransform.sizeDelta = new Vector2(Screen.width * 0.6f, Screen.height / 2f);

            initialized = true;

            SetSignalScopeUIVisible(false);
        }

        private void OnToolEquiped(PlayerTool t)
        {
            switch(t.name)
            {
                case "NomaiTranslatorProp":
                    TranslatorText.gameObject.SetActive(Main.IsThirdPerson());
                    _isTranslatorEquiped = true;
                    break;
                case "ProbeLauncher":
                    if (PlayerState.AtFlightConsole())
                    {
                        _isShipProbeLauncherEquiped = true;
                        _shipProbeLauncherImage.gameObject.SetActive(Main.IsThirdPerson() && _isShipProbeLauncherPictureTaken);
                    }
                    break;
                case "Signalscope":
                    SetSignalScopeUIVisible(PlayerState.AtFlightConsole() && Main.IsThirdPerson());
                    _isSignalScopeEquiped = true;
                    break;
            }
        }

        private void OnToolUnequiped(PlayerTool t)
        {
            switch (t.name)
            {
                case "NomaiTranslatorProp":
                    TranslatorText.gameObject.SetActive(false);
                    _isTranslatorEquiped = false;
                    break;
                case "ProbeLauncher":
                    if (PlayerState.AtFlightConsole()) _shipProbeLauncherImage.gameObject.SetActive(false);
                    _isShipProbeLauncherEquiped = false;
                    break;
                case "Signalscope":
                    SetSignalScopeUIVisible(false);
                    _isSignalScopeEquiped = false;
                    break;
            }
        }

        private void OnDeactivateThirdPersonCamera()
        {
            ShipText.gameObject.SetActive(false);
            TranslatorText.gameObject.SetActive(false);
            SetSignalScopeUIVisible(false);
            _shipProbeLauncherImage.gameObject.SetActive(false);
        }

        private void OnExitFlightConsole()
        {
            ShipText.gameObject.SetActive(false);
            SetSignalScopeUIVisible(false);
            _shipProbeLauncherImage.gameObject.SetActive(false);
        }

        private void OnEnterFlightConsole(OWRigidbody _)
        {
            ShipText.gameObject.SetActive(Main.IsThirdPerson());
        }

        private void OnSwitchActiveCamera(OWCamera camera)
        {
            if (camera.name == "MapCamera")
            {
                ShipText.gameObject.SetActive(false);
                TranslatorText.gameObject.SetActive(false);
                SetSignalScopeUIVisible(false);
                _shipProbeLauncherImage.gameObject.SetActive(false);
            }
            if (camera.name == "ThirdPersonCamera")
            {
                ShipText.gameObject.SetActive(PlayerState.AtFlightConsole());
                TranslatorText.gameObject.SetActive(_isTranslatorEquiped);
                SetSignalScopeUIVisible(PlayerState.AtFlightConsole() && _isSignalScopeEquiped);
                _shipProbeLauncherImage.gameObject.SetActive(PlayerState.AtFlightConsole() && _isShipProbeLauncherEquiped && _isShipProbeLauncherPictureTaken);
            }
        }

        private void OnGamePaused()
        {
            if (!initialized) return;

            ShipText.gameObject.SetActive(false);
            _shipProbeLauncherImage.gameObject.SetActive(false);

            SetSignalScopeUIVisible(false);
        }

        private void OnGameUnpaused()
        {
            if (!initialized) return;
            if (Locator.GetActiveCamera().name != "ThirdPersonCamera") return;

            ShipText.gameObject.SetActive(PlayerState.AtFlightConsole());
            _shipProbeLauncherImage.gameObject.SetActive(PlayerState.AtFlightConsole() && _isShipProbeLauncherEquiped && _isShipProbeLauncherPictureTaken);

            SetSignalScopeUIVisible(PlayerState.AtFlightConsole() && _isSignalScopeEquiped && Main.IsThirdPerson());
        }

        private void SetSignalScopeUIVisible(bool visible)
        {
            if (!initialized) return;
            _signalScopeText.gameObject.SetActive(visible);
            _signalScopeDistanceText.gameObject.SetActive(visible);
            _waveformRenderer.gameObject.SetActive(visible);

            // Disappear the rest of the UI
            GameObject sigScopeDisplay = GameObject.Find("/Ship_Body/Module_Cockpit/Systems_Cockpit/ShipCockpitUI/SignalScreen/SignalScreenPivot/SigScopeDisplay");
            if (sigScopeDisplay != null)
            {
                if(_shipSigScopeLocalScale == Vector3.zero) _shipSigScopeLocalScale = sigScopeDisplay.transform.localScale;
                sigScopeDisplay.transform.localScale = visible ? Vector3.zero : _shipSigScopeLocalScale;
            }
        }

        private void OnProbeSnapshotRemoved()
        {
            _isShipProbeLauncherPictureTaken = false;
            if(_shipProbeLauncherImage != null) _shipProbeLauncherImage.gameObject.SetActive(false);
        }

        public static void SetProbeLauncherTexture(Texture2D texture)
        {
            _isShipProbeLauncherPictureTaken = true;

            _shipProbeLauncherImage.material.SetTexture("_MainTex", texture);
            _shipProbeLauncherImage.SetMaterialDirty();
            _shipProbeLauncherImage.gameObject.SetActive(_isShipProbeLauncherEquiped && Main.IsThirdPerson());
        }

        public static void SetProbeLauncherTexture(RenderTexture texture)
        {
            _isShipProbeLauncherPictureTaken = true;

            _shipProbeLauncherImage.material.SetTexture("_MainTex", texture);
            _shipProbeLauncherImage.SetMaterialDirty();
            _shipProbeLauncherImage.gameObject.SetActive(_isShipProbeLauncherEquiped && Main.IsThirdPerson());
        }

        public static void SetSignalScopeLabel(string signalscopeText, string distanceText)
        {
            _signalScopeText.text = "FREQUENCY:\n" + signalscopeText;
            _signalScopeDistanceText.text = distanceText;
        }

        public static void SetSignalScopeWaveform(Vector3[] linePoints)
        {
            _waveformRenderer.SetPositions(linePoints);
        }
    }
}
