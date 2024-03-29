﻿using UnityEngine;
using UnityEngine.UI;

namespace ThirdPersonCamera.Handlers
{
    public class UIHandler : MonoBehaviour
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

        private RectTransform _signalscopeDistanceRectTransform;
        private RectTransform _shipTextRectTransform;
        private RectTransform _translatorRectTransform;
        private RectTransform _probeLauncherRectTransform;
        private RectTransform _signalscopeRectTransform;

        private GameObject _sigScopeDisplay;

		private SuitNotificationDisplay _suitNotificationDisplay;

		public void Awake()
		{
            Main.CommonCameraAPI.EquipTool().AddListener(OnToolEquiped);
            Main.CommonCameraAPI.UnequipTool().AddListener(OnToolUnequiped);

            GlobalMessenger.AddListener("ExitFlightConsole", OnExitFlightConsole);
            GlobalMessenger<OWRigidbody>.AddListener("EnterFlightConsole", OnEnterFlightConsole);
            GlobalMessenger.AddListener("Probe Snapshot Removed", OnProbeSnapshotRemoved);
            GlobalMessenger<OWCamera>.AddListener("SwitchActiveCamera", OnSwitchActiveCamera);
            GlobalMessenger.AddListener("GamePaused", OnGamePaused);
            GlobalMessenger.AddListener("GameUnpaused", OnGameUnpaused);
            GlobalMessenger<GraphicSettings>.AddListener("GraphicSettingsUpdated", OnGraphicSettingsUpdated);
        }

        public void OnDestroy()
        {
            Main.CommonCameraAPI.EquipTool().RemoveListener(OnToolEquiped);
            Main.CommonCameraAPI.UnequipTool().RemoveListener(OnToolUnequiped);

            GlobalMessenger.RemoveListener("ExitFlightConsole", OnExitFlightConsole);
            GlobalMessenger<OWRigidbody>.RemoveListener("EnterFlightConsole", OnEnterFlightConsole);
            GlobalMessenger.RemoveListener("Probe Snapshot Removed", OnProbeSnapshotRemoved);
            GlobalMessenger<OWCamera>.RemoveListener("SwitchActiveCamera", OnSwitchActiveCamera);
            GlobalMessenger.RemoveListener("GamePaused", OnGamePaused);
            GlobalMessenger.RemoveListener("GameUnpaused", OnGameUnpaused);
            GlobalMessenger<GraphicSettings>.RemoveListener("GraphicSettingsUpdated", OnGraphicSettingsUpdated);
        }

        public void Init()
        {
            _sigScopeDisplay = Locator.GetShipBody()?.transform.Find("/Ship_Body/Module_Cockpit/Systems_Cockpit/ShipCockpitUI/SignalScreen/SignalScreenPivot/SigScopeDisplay").gameObject;

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
            ShipText.alignment = TextAnchor.UpperCenter;

            _shipTextRectTransform = ShipText.GetComponent<RectTransform>();

            // Nomai Text
            GameObject myText2 = new GameObject();
            myText2.transform.parent = canvasObject.transform;
            myText2.name = "TranslatorText";

            TranslatorText = myText2.AddComponent<Text>();
            TranslatorText.font = font;
            TranslatorText.text = "";
            TranslatorText.alignment = TextAnchor.UpperCenter;

            _translatorRectTransform = TranslatorText.GetComponent<RectTransform>();

            // Start inactive
            ShipText.gameObject.SetActive(false);
            TranslatorText.gameObject.SetActive(false);

            // Ship probe camera
            GameObject imgObject = new GameObject("ShipProbeLauncherImage");

            _probeLauncherRectTransform = imgObject.AddComponent<RectTransform>();
            _probeLauncherRectTransform.transform.SetParent(canvas.transform);
            _probeLauncherRectTransform.localScale = Vector3.one;

            _shipProbeLauncherImage = imgObject.AddComponent<Image>();
            var colour = _shipProbeLauncherImage.color;
            colour.a = 0.8f;
            _shipProbeLauncherImage.color = colour;
            imgObject.transform.SetParent(canvas.transform);
            imgObject.SetActive(false);

            // SignalScope reticle
            SigScopeReticuleParent = new GameObject();
            Transform parent = ThirdPersonCamera.GetCamera().transform;
            SigScopeReticuleParent.transform.parent = parent;
            SigScopeReticuleParent.transform.rotation = parent.rotation;
            SigScopeReticuleParent.transform.position = parent.position + parent.TransformDirection(Vector3.forward) * 2f;

            // Signalscope line renderer
            var material = GameObject.Find("/Ship_Body/Module_Cockpit/Systems_Cockpit/ShipCockpitUI/SignalScreen/SignalScreenPivot/SigScopeDisplay")?.GetComponentInChildren<LineRenderer>().material;
            if(material != null)
            {
                GameObject _waveFormGameObject = new GameObject();
                _waveFormGameObject.transform.SetParent(ThirdPersonCamera.GetCamera().gameObject.transform);
                _waveFormGameObject.transform.position = _waveFormGameObject.transform.parent.position
                        + _waveFormGameObject.transform.parent.TransformDirection(Vector3.forward) * 200f
                        + _waveFormGameObject.transform.parent.TransformDirection(Vector3.down) * 120f;
                _waveFormGameObject.transform.rotation = _waveFormGameObject.transform.parent.rotation;
                _waveFormGameObject.name = "ShipSignalScopeWaveform";

                _waveformRenderer = _waveFormGameObject.AddComponent<LineRenderer>();
                _waveformRenderer.positionCount = 256;
                _waveformRenderer.material = material;
                _waveformRenderer.useWorldSpace = false;
            }

            // SignalScope Text
            GameObject myText3 = new GameObject();
            myText3.transform.SetParent(canvas.transform);
            myText3.name = "ShipSignalScopeText";

            _signalScopeText = myText3.AddComponent<Text>();
            _signalScopeText.font = font;
            _signalScopeText.text = "";
            _signalScopeText.alignment = TextAnchor.UpperCenter;

            // Text position
            _signalscopeRectTransform = _signalScopeText.GetComponent<RectTransform>();

            // SignalScope Text
            GameObject myText4 = new GameObject();
            myText4.transform.SetParent(canvas.transform);
            myText4.name = "ShipSignalScopeDistanceText";

            _signalScopeDistanceText = myText4.AddComponent<Text>();
            _signalScopeDistanceText.font = font;
            _signalScopeDistanceText.text = "";
            _signalScopeDistanceText.alignment = TextAnchor.UpperCenter;

            // Text position
            _signalscopeDistanceRectTransform = _signalScopeDistanceText.GetComponent<RectTransform>();

            ResetGraphicsSizes();

            SetSignalScopeUIVisible(false);

			_suitNotificationDisplay = GameObject.FindObjectOfType<SuitNotificationDisplay>();
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
            if (Main.IsAtEye) return;

            if (camera.name == "ThirdPersonCamera")
            {
                ShipText.gameObject.SetActive(PlayerState.AtFlightConsole());
                TranslatorText.gameObject.SetActive(_isTranslatorEquiped);
                SetSignalScopeUIVisible(PlayerState.AtFlightConsole() && _isSignalScopeEquiped);
                _shipProbeLauncherImage.gameObject.SetActive(PlayerState.AtFlightConsole() && _isShipProbeLauncherEquiped && _isShipProbeLauncherPictureTaken);
            }
            else
            {
                ShipText.gameObject.SetActive(false);
                TranslatorText.gameObject.SetActive(false);
                SetSignalScopeUIVisible(false);
                _shipProbeLauncherImage.gameObject.SetActive(false);
            }
        }

        private void OnGamePaused()
        {
            if (!Main.IsLoaded || ShipText == null) return;

            if(!Main.IsAtEye)
            {
                ShipText.gameObject.SetActive(false);
                _shipProbeLauncherImage.gameObject.SetActive(false);
            }

            SetSignalScopeUIVisible(false);
        }

        private void OnGameUnpaused()
        {
            if (!Main.IsLoaded) return;
            if (Locator.GetActiveCamera().name != "ThirdPersonCamera") return;

            if(!Main.IsAtEye)
            {
                ShipText.gameObject.SetActive(PlayerState.AtFlightConsole());
                _shipProbeLauncherImage.gameObject.SetActive(PlayerState.AtFlightConsole() && _isShipProbeLauncherEquiped && _isShipProbeLauncherPictureTaken);
            }

            SetSignalScopeUIVisible(PlayerState.AtFlightConsole() && _isSignalScopeEquiped && Main.IsThirdPerson());
        }

        private void SetSignalScopeUIVisible(bool visible)
        {
            if (!Main.IsLoaded || Main.IsAtEye) return;
            _signalScopeText.gameObject.SetActive(visible);
            _signalScopeDistanceText.gameObject.SetActive(visible);
            _waveformRenderer.gameObject.SetActive(visible);

            // Disappear the rest of the UI
            if (_sigScopeDisplay != null)
            {
                if(_shipSigScopeLocalScale == Vector3.zero) _shipSigScopeLocalScale = _sigScopeDisplay.transform.localScale;
                _sigScopeDisplay.transform.localScale = visible ? Vector3.zero : _shipSigScopeLocalScale;
            }
        }

        private void OnProbeSnapshotRemoved()
        {
            if (Main.IsAtEye) return;

            _isShipProbeLauncherPictureTaken = false;
            if(_shipProbeLauncherImage != null) _shipProbeLauncherImage.gameObject.SetActive(false);
        }

        public static void SetProbeLauncherTexture(Texture2D texture)
        {
            if (Main.IsAtEye) return;

            _isShipProbeLauncherPictureTaken = true;

            _shipProbeLauncherImage.material.SetTexture("_MainTex", texture);
            _shipProbeLauncherImage.SetMaterialDirty();
            _shipProbeLauncherImage.gameObject.SetActive(_isShipProbeLauncherEquiped && Main.IsThirdPerson());
        }

        public static void SetProbeLauncherTexture(RenderTexture texture)
        {
            if (Main.IsAtEye) return;

            _isShipProbeLauncherPictureTaken = true;

            _shipProbeLauncherImage.material.SetTexture("_MainTex", texture);
            _shipProbeLauncherImage.SetMaterialDirty();
            _shipProbeLauncherImage.gameObject.SetActive(_isShipProbeLauncherEquiped && Main.IsThirdPerson());
        }

        public static void SetSignalScopeLabel(string signalscopeText, string distanceText)
        {
            if (Main.IsAtEye) return;

            _signalScopeText.text = "FREQUENCY:\n" + signalscopeText;
            _signalScopeDistanceText.text = distanceText;
        }

        public static void SetSignalScopeWaveform(Vector3[] linePoints)
        {
            if (Main.IsAtEye) return;

            _waveformRenderer.SetPositions(linePoints);
        }

        private void OnGraphicSettingsUpdated(GraphicSettings graphicSettings)
        {
            ResetGraphicsSizes();
        }

        private void ResetGraphicsSizes()
        {
            if (!Main.IsLoaded) return;

            var height = PlayerData.GetGraphicSettings().displayResHeight;
            var width = PlayerData.GetGraphicSettings().displayResWidth;

            // Ship
            ShipText.fontSize = (int)(24f * height / 1080f);
            _shipTextRectTransform.localPosition = new Vector3(0, (int) (0.18f * height), 0);
            _shipTextRectTransform.sizeDelta = new Vector2((int)width, (int)(height / 2f));

            // Translator
            TranslatorText.fontSize = (int)(32f * height / 1080f);
            _translatorRectTransform.localPosition = new Vector3(0, (int)(0.18f * height), 0);
            _translatorRectTransform.sizeDelta = new Vector2((int)(width * 0.6f), (int)(height / 2f));

            // Probe image
            _probeLauncherRectTransform.anchoredPosition = new Vector2((int)(0.3125f * width), (int)(0.231f * height));
            _probeLauncherRectTransform.sizeDelta = new Vector2((int)(0.20f * width), (int)(0.20f * width));

            // Signalscope frequency
            _signalScopeText.fontSize = (int)(24f * height / 1080f);
            _signalscopeRectTransform.localPosition = new Vector3(0, (int)(0.50f * -height), 0);
            _signalscopeRectTransform.sizeDelta = new Vector2((int)(width * 0.6f), (int)(height / 2f));

            // Signalscope distance
            _signalScopeDistanceText.fontSize = (int)(32f * height / 1080f);
            _signalscopeDistanceRectTransform.localPosition = new Vector3(0, (int)(0.35f * -height), 0);
            _signalscopeDistanceRectTransform.sizeDelta = new Vector2((int)(width * 0.6f), (int)(0.5f * height));
        }

		public void Update()
		{
			if (!Main.IsLoaded)
			{
				return;
			}

			// Hide notifications when in third person and showing text in the same place
			var shouldHideNotifications = Main.IsThirdPerson() && !PlayerState.AtFlightConsole() && !string.IsNullOrEmpty(TranslatorText.text);
			if (_suitNotificationDisplay.gameObject.activeSelf == shouldHideNotifications) _suitNotificationDisplay.gameObject.SetActive(!shouldHideNotifications);
		}
	}
}
