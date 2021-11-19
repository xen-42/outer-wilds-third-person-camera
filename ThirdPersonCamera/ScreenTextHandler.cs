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
    public class ScreenTextHandler : INotifiable
    {
        private static readonly Regex noWhitespace = new Regex(@"\s+");
        private readonly Main parent;

        private List<string> _shipNotifications = new List<string>();

        private Text shipText;
        private Text translatorText;

        private bool _isPilotingShip = false;

        private bool _isTranslatorEquiped = false;

        private NomaiAudioVolume nomaiAudio;

        private static NomaiText currentNomaiText;
        private static int _currentTextID = -2;

        public ScreenTextHandler(Main _main)
        {
            parent = _main;

            GlobalMessenger<NomaiText, int>.AddListener("SetNomaiText", new Callback<NomaiText, int>(OnSetNomaiText));
            GlobalMessenger<NomaiAudioVolume, int>.AddListener("SetNomaiAudio", new Callback<NomaiAudioVolume, int>(SetNomaiAudio));
            // DO NOT CALL NomaiText.GetTextNode FROM IN HERE OK
            // IT GETS INTO A BIG RECURSIVE LOOP AND ITS BAD OK VERY BAD DO NOT DO
            GlobalMessenger<NomaiText, int>.AddListener("GetTextNode", new Callback<NomaiText, int>(GetTextNode));
            GlobalMessenger<NomaiText>.AddListener("TextedTranslated", new Callback<NomaiText>(OnTextedTranslated));
            GlobalMessenger<PlayerTool>.AddListener("OnEquipTool", new Callback<PlayerTool>(OnToolEquiped));
            GlobalMessenger<PlayerTool>.AddListener("OnUnequipTool", new Callback<PlayerTool>(OnToolUnequiped));
            GlobalMessenger.AddListener("DeactivateThirdPersonCamera", new Callback(OnDeactivateThirdPersonCamera));
            GlobalMessenger.AddListener("ActivateThirdPersonCamera", new Callback(OnActivateThirdPersonCamera));
            GlobalMessenger.AddListener("ExitFlightConsole", new Callback(OnExitFlightConsole));
            GlobalMessenger<OWRigidbody>.AddListener("EnterFlightConsole", new Callback<OWRigidbody>(OnEnterFlightConsole));

            NotificationManager.SharedInstance.RegisterNotifiable(this);
        }

        public void OnDestroy()
        {
            GlobalMessenger<NomaiText, int>.RemoveListener("SetNomaiText", new Callback<NomaiText, int>(OnSetNomaiText));
            GlobalMessenger<NomaiAudioVolume, int>.RemoveListener("SetNomaiAudio", new Callback<NomaiAudioVolume, int>(SetNomaiAudio));
            GlobalMessenger<PlayerTool>.RemoveListener("OnEquipTool", new Callback<PlayerTool>(OnToolEquiped));
            GlobalMessenger<PlayerTool>.RemoveListener("OnUnequipTool", new Callback<PlayerTool>(OnToolUnequiped));
            GlobalMessenger.RemoveListener("DeactivateThirdPersonCamera", new Callback(OnDeactivateThirdPersonCamera));
            GlobalMessenger.RemoveListener("ActivateThirdPersonCamera", new Callback(OnActivateThirdPersonCamera));
            GlobalMessenger.RemoveListener("ExitFlightConsole", new Callback(OnExitFlightConsole));
            GlobalMessenger<OWRigidbody>.RemoveListener("EnterFlightConsole", new Callback<OWRigidbody>(OnEnterFlightConsole));

            NotificationManager.SharedInstance.UnregisterNotifiable(this);
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

            shipText = shipTextObject.AddComponent<Text>();
            shipText.font = font;
            shipText.text = "";
            shipText.fontSize = 24;
            shipText.alignment = TextAnchor.UpperCenter;

            // Text position
            RectTransform shipTextRectTransform = shipText.GetComponent<RectTransform>();
            shipTextRectTransform.localPosition = new Vector3(0, Screen.height / 4f - 32, 0);
            shipTextRectTransform.sizeDelta = new Vector2(Screen.width, Screen.height / 2f);

            // Nomai Text
            GameObject myText2 = new GameObject();
            myText2.transform.parent = canvasObject.transform;
            myText2.name = "TranslatorText";

            translatorText = myText2.AddComponent<Text>();
            translatorText.font = font;
            translatorText.text = "";
            translatorText.fontSize = 32;
            translatorText.alignment = TextAnchor.UpperCenter;

            // Text position
            RectTransform translatorRectTransform = translatorText.GetComponent<RectTransform>();
            translatorRectTransform.localPosition = new Vector3(0, Screen.height / 4f - 32, 0);
            translatorRectTransform.sizeDelta = new Vector2(Screen.width * 0.6f, Screen.height / 2f);

            // Start inactive
            shipText.gameObject.SetActive(false);
            translatorText.gameObject.SetActive(false);
        }

        private void OnSetNomaiText(NomaiText text, int textID)
        {
            nomaiAudio = null;
            ReadNomaiText(text, textID, true);
        }

        private void SetNomaiAudio(NomaiAudioVolume audio, int textPage)
        {
            nomaiAudio = audio;
            ReadNomaiText(audio.GetAudioText(), textPage, true);
        }

        private void GetTextNode(NomaiText text, int id)
        {
            if(nomaiAudio?.GetAudioText() == text)
            {
                ReadNomaiText(text, id, false);
            }
        }

        private void OnTextedTranslated(NomaiText text)
        {
            if (nomaiAudio?.GetAudioText() == text && currentNomaiText != text) ReadNomaiText(text, _currentTextID, false);
        }

        private void ReadNomaiText(NomaiText text, int textID, bool checkIfTranslated)
        {
            if (currentNomaiText == text && _currentTextID == textID) return; //Already is displayed!

            if (!checkIfTranslated || text.IsTranslated(textID))
            {
                currentNomaiText = text;
                _currentTextID = textID;
                translatorText.text = CleanString(text.GetTextNode(textID));
            }
            else translatorText.text = "???";
        }

        private void OnToolEquiped(PlayerTool t)
        {
            if (t.name == "NomaiTranslatorProp")
            {
                translatorText.gameObject.SetActive(parent.IsThirdPerson());
                _isTranslatorEquiped = true;
            }
        }

        private void OnToolUnequiped(PlayerTool t)
        {
            if (t.name == "NomaiTranslatorProp")
            {
                translatorText.gameObject.SetActive(false);
                _isTranslatorEquiped = false;
            }
        }

        public void OnDeactivateThirdPersonCamera()
        {
            shipText.gameObject.SetActive(false);
            translatorText.gameObject.SetActive(false);
        }

        public void OnActivateThirdPersonCamera()
        {
            shipText.gameObject.SetActive(_isPilotingShip);
            translatorText.gameObject.SetActive(_isTranslatorEquiped);
        }

        public void OnExitFlightConsole()
        {
            _isPilotingShip = false;
            shipText.gameObject.SetActive(false);
        }

        public void OnEnterFlightConsole(OWRigidbody _)
        {
            _isPilotingShip = true;
            shipText.gameObject.SetActive(parent.IsThirdPerson());
        }

        public string GetShipText()
        {
            string s = "";
            foreach(string i in _shipNotifications)
            {
                s += i + "\n";
            }
            return s;
        }

        private string CleanString(string s)
        {
            return Regex.Replace(s.Trim().Replace("\n", " ").Replace("\r", " "), @"\s+", " ");
        }

        private string RemoveAllWhitespace(string s)
        {
            return noWhitespace.Replace(s.ToLower().Normalize(), "");
        }

        private int GetStringIndexFromList(string s, List<string> list)
        {
            string s1 = RemoveAllWhitespace(s);
            for (int i = 0; i < list.Count; i++)
            {
                string s2 = RemoveAllWhitespace(list[i]);
                parent.WriteInfo($"{s1}, {s2}");
                if (string.Equals(s1, s2)) return i;
            }
            return -1;
        }

        public void PushNotification(NotificationData data)
        {
            if (data.notificationTgt == NotificationTarget.Ship)
            {
                // Don't put duplicates
                if (GetStringIndexFromList(data.displayMessage, _shipNotifications) != -1) return;

                bool pushToTop = data.displayMessage.Contains("EXIT") || data.displayMessage.Contains("STAGE") || data.displayMessage.Contains("AUTOPILOT");

                if (pushToTop) _shipNotifications.Insert(0, data.displayMessage);
                else _shipNotifications.Add(data.displayMessage);

                // Some messages never have their notifications removed
                if (data.displayMessage.Contains("AUTOPILOT") || data.displayMessage.Contains("VELOCITY")) parent.StartCoroutine(WaitAndRemoveNotification(data, 3.0f));
            }
            shipText.text = GetShipText();
        }
    
        IEnumerator WaitAndRemoveNotification(NotificationData data, float t)
        {
            yield return new WaitForSeconds(t);
            RemoveNotification(data);
        }

        public void RemoveNotification(NotificationData data)
        {
            if (data.notificationTgt == NotificationTarget.Ship)
            {
                parent.WriteInfo($"Notification removed: {data.displayMessage}");
                int index = GetStringIndexFromList(data.displayMessage, _shipNotifications);
                if (index == -1) {
                    parent.WriteWarning($"Couldn't find message {data.displayMessage}");
                    _shipNotifications.Clear(); // Would rather have nothing displayed than clog it up forever
                }
                else _shipNotifications.RemoveAt(index);
            }
            shipText.text = GetShipText();
        }

        public static NomaiText GetCurrentText()
        {
            return currentNomaiText;
        }

        public static int GetCurrentTextID()
        {
            return _currentTextID;
        }
    }
}
