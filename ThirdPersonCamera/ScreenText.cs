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
    public class ScreenText : INotifiable
    {
        private static readonly Regex sWhitespace = new Regex(@"\s+");
        private readonly Main parent;

        private List<string> _shipNotifications = new List<string>();

        private Text shipText;
        private Text translatorText;

        public ScreenText(Main _main)
        {
            GlobalMessenger<NomaiText, int>.AddListener("SetNomaiText", new Callback<NomaiText, int>(OnSetNomaiText));
            GlobalMessenger<PlayerTool>.AddListener("OnUnequipTool", new Callback<PlayerTool>(OnToolUnequiped));
            parent = _main;
            NotificationManager.SharedInstance.RegisterNotifiable(this);
        }

        public void ShowShipOverlay(bool visible)
        {
            shipText.gameObject.SetActive(visible);
        }

        public void Init()
        {
            Font font = GameObject.Find("PlayerHUD/HelmetOnUI/UICanvas/SecondaryGroup/GForce/NumericalReadout/GravityText").GetComponent<Text>().font;

            GameObject myGO;
            GameObject myText;
            Canvas myCanvas;

            // Canvas
            myGO = new GameObject();
            myGO.name = "TestCanvas";
            myGO.AddComponent<Canvas>();

            myCanvas = myGO.GetComponent<Canvas>();
            myCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            myGO.AddComponent<CanvasScaler>();
            myGO.AddComponent<GraphicRaycaster>();

            // Text
            myText = new GameObject();
            myText.transform.parent = myGO.transform;
            myText.name = "ShipConsoleText";

            shipText = myText.AddComponent<Text>();
            shipText.font = font;
            shipText.text = "";
            shipText.fontSize = 24;
            shipText.alignment = TextAnchor.UpperCenter;

            // Text position
            RectTransform rectTransform = shipText.GetComponent<RectTransform>();
            rectTransform.localPosition = new Vector3(0, Screen.height/4f - 32, 0);
            rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height / 2f);

            // Text
            GameObject myText2 = new GameObject();
            myText2.transform.parent = myGO.transform;
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
            ShowShipOverlay(false);
        }

        private void OnToolUnequiped(PlayerTool t)
        {
            if (t.name == "NomaiTranslatorProp") translatorText.text = "";
        }

        private void OnSetNomaiText(NomaiText text, int textID)
        {
            if (text.IsTranslated(textID)) translatorText.text = text.GetTextNode(textID);
            else translatorText.text = "???";
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

        private string RemoveAllWhitespace(string s)
        {
            return sWhitespace.Replace(s.ToLower().Normalize(), "");
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
            bool pushToTop = false;
            if(data.notificationTgt == NotificationTarget.Ship)
            {
                // Don't put duplicates
                if (GetStringIndexFromList(data.displayMessage, _shipNotifications) != -1) return;

                pushToTop = data.displayMessage.Contains("EXIT") || data.displayMessage.Contains("STAGE") || data.displayMessage.Contains("AUTOPILOT");
                parent.WriteInfo($"New notification: {data.displayMessage}");
                if (pushToTop) _shipNotifications.Insert(0, data.displayMessage);
                else _shipNotifications.Add(data.displayMessage);

                // Clear autopilot aborted after 3 seconds
                if (data.displayMessage.Contains("ABORT")) parent.StartCoroutine(WaitAndRemoveNotification(data, 3.0f));
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
    }
}
