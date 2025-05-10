using System.IO;
using UnityEngine;

namespace ThirdPersonCamera.Handlers
{
    public class PromptHandler : MonoBehaviour
    {
        private ScreenPrompt _gamepadCameraPrompt;
        private ScreenPrompt _keyboardCameraPrompt;
        private Texture2D _vKey;
        private Sprite _vSprite;

        private bool _enabled;

        public void Awake()
        {
            _vKey = Main.SharedInstance.ModHelper.Assets.GetTexture("assets/V_Key_Dark.png");
            _vSprite = Sprite.Create(_vKey, new Rect(0, 0, _vKey.width, _vKey.height), new Vector2(_vKey.width, _vKey.height) / 2f);

            var promptText = TranslationHandler.GetTranslation("THIRD_PERSON_CAMERA_TOGGLE");

            _gamepadCameraPrompt = new ScreenPrompt(InputLibrary.toolOptionLeft, promptText);
            _keyboardCameraPrompt = new ScreenPrompt(promptText, _vSprite);

            Locator.GetPromptManager().AddScreenPrompt(_gamepadCameraPrompt, PromptPosition.UpperRight, false);
            Locator.GetPromptManager().AddScreenPrompt(_keyboardCameraPrompt, PromptPosition.UpperRight, false);

            GlobalMessenger.AddListener("GamePaused", OnGamePaused);
            GlobalMessenger.AddListener("GameUnpaused", OnGameUnpaused);
            GlobalMessenger.AddListener("WakeUp", OnWakeUp);

            UpdatePromptVisibility();
        }

        public void OnDestroy()
        {
            if (Locator.GetPromptManager() != null)
            {
                Locator.GetPromptManager().RemoveScreenPrompt(_gamepadCameraPrompt, PromptPosition.UpperRight);
                Locator.GetPromptManager().RemoveScreenPrompt(_keyboardCameraPrompt, PromptPosition.UpperRight);
            }

            GlobalMessenger.RemoveListener("GamePaused", OnGamePaused);
            GlobalMessenger.RemoveListener("GameUnpaused", OnGameUnpaused);
            GlobalMessenger.RemoveListener("WakeUp", OnWakeUp);

            Object.Destroy(_vSprite);
            Object.Destroy(_vKey);
        }

        public void Update()
        {
            UpdatePromptVisibility();
        }

        private void OnGamePaused()
        {
            _enabled = false;
        }

        private void OnGameUnpaused()
        {
            _enabled = true;
        }

        private void OnWakeUp()
        {
            _enabled = true;
        }

        private void UpdatePromptVisibility()
        {
            var canUse = (ThirdPersonCamera.CanUse() && ThirdPersonCamera.CameraEnabled);
            if (Main.ShowButtonPrompts && _enabled && canUse)
            {
                _gamepadCameraPrompt.SetVisibility(OWInput.UsingGamepad());
                _keyboardCameraPrompt.SetVisibility(!OWInput.UsingGamepad());
            }
            else
            {
                _gamepadCameraPrompt.SetVisibility(false);
                _keyboardCameraPrompt.SetVisibility(false);
            }
        }
    }
}
