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
    public class ScreenTextHandler
    {
        private static readonly Regex noWhitespace = new Regex(@"\s+");

        private List<string> _shipNotifications = new List<string>();

        public static Text ShipText { get; private set; }
        public static Text TranslatorText { get; private set; } 

        private bool _isPilotingShip = false;

        private bool _isTranslatorEquiped = false;

        public ScreenTextHandler()
        {
            GlobalMessenger<PlayerTool>.AddListener("OnEquipTool", new Callback<PlayerTool>(OnToolEquiped));
            GlobalMessenger<PlayerTool>.AddListener("OnUnequipTool", new Callback<PlayerTool>(OnToolUnequiped));
            GlobalMessenger.AddListener("DeactivateThirdPersonCamera", new Callback(OnDeactivateThirdPersonCamera));
            GlobalMessenger.AddListener("ActivateThirdPersonCamera", new Callback(OnActivateThirdPersonCamera));
            GlobalMessenger.AddListener("ExitFlightConsole", new Callback(OnExitFlightConsole));
            GlobalMessenger<OWRigidbody>.AddListener("EnterFlightConsole", new Callback<OWRigidbody>(OnEnterFlightConsole));
        }

        public void OnDestroy()
        {
            GlobalMessenger<PlayerTool>.RemoveListener("OnEquipTool", new Callback<PlayerTool>(OnToolEquiped));
            GlobalMessenger<PlayerTool>.RemoveListener("OnUnequipTool", new Callback<PlayerTool>(OnToolUnequiped));
            GlobalMessenger.RemoveListener("DeactivateThirdPersonCamera", new Callback(OnDeactivateThirdPersonCamera));
            GlobalMessenger.RemoveListener("ActivateThirdPersonCamera", new Callback(OnActivateThirdPersonCamera));
            GlobalMessenger.RemoveListener("ExitFlightConsole", new Callback(OnExitFlightConsole));
            GlobalMessenger<OWRigidbody>.RemoveListener("EnterFlightConsole", new Callback<OWRigidbody>(OnEnterFlightConsole));
        }

        public void Init()
        {
            Font font = GameObject.Find("PlayerHUD/HelmetOnUI/UICanvas/SecondaryGroup/GForce/NumericalReadout/GravityText").GetComponent<Text>().font;

            // Canvas
            GameObject canvasObject = new GameObject();
            canvasObject.name = "ThirdPersonCanvas";
            canvasObject.AddComponent<Canvas>();

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
        }

        private void OnToolEquiped(PlayerTool t)
        {
            if (t.name == "NomaiTranslatorProp")
            {
                TranslatorText.gameObject.SetActive(Main.IsThirdPerson());
                _isTranslatorEquiped = true;
            }
        }

        private void OnToolUnequiped(PlayerTool t)
        {
            if (t.name == "NomaiTranslatorProp")
            {
                TranslatorText.gameObject.SetActive(false);
                _isTranslatorEquiped = false;
            }
        }

        public void OnDeactivateThirdPersonCamera()
        {
            ShipText.gameObject.SetActive(false);
            TranslatorText.gameObject.SetActive(false);
        }

        public void OnActivateThirdPersonCamera()
        {
            ShipText.gameObject.SetActive(_isPilotingShip);
            TranslatorText.gameObject.SetActive(_isTranslatorEquiped);
        }

        public void OnExitFlightConsole()
        {
            _isPilotingShip = false;
            ShipText.gameObject.SetActive(false);
        }

        public void OnEnterFlightConsole(OWRigidbody _)
        {
            _isPilotingShip = true;
            ShipText.gameObject.SetActive(Main.IsThirdPerson());
        }
    }
}
