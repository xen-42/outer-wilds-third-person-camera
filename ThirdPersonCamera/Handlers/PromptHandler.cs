using UnityEngine;

namespace ThirdPersonCamera.Handlers
{
    public class PromptHandler : MonoBehaviour
    {
        private static ScreenPrompt _gamepadCameraPrompt;
        private static ScreenPrompt _keyboardCameraPrompt;
        private static Texture2D _vKey;
        private static bool _initialized;

        private bool _enabled;

        private void Awake()
        {
            // Only ever has to happen once
            if(!_initialized)
            {
                _vKey = Main.SharedInstance.ModHelper.Assets.GetTexture("assets/V_Key_Dark.png");

                var vSprite = Sprite.Create(_vKey, new Rect(0, 0, _vKey.width, _vKey.height), new Vector2(_vKey.width, _vKey.height) / 2f);
                _gamepadCameraPrompt = new ScreenPrompt(InputLibrary.toolOptionLeft, "Toggle Third Person <CMD>");
                _keyboardCameraPrompt = new ScreenPrompt("Toggle Third Person <CMD>", vSprite);

                _initialized = true;
            }

            Locator.GetPromptManager().AddScreenPrompt(_gamepadCameraPrompt, PromptPosition.UpperRight, false);
            Locator.GetPromptManager().AddScreenPrompt(_keyboardCameraPrompt, PromptPosition.UpperRight, false);

            GlobalMessenger.AddListener("GamePaused", OnGamePaused);
            GlobalMessenger.AddListener("GameUnpaused", OnGameUnpaused);
            GlobalMessenger.AddListener("WakeUp", OnWakeUp);

            var toolMode = Locator.GetToolModeSwapper().GetToolMode();

            UpdatePromptVisibility();
        }

        private void OnDestroy()
        {
            Locator.GetPromptManager().RemoveScreenPrompt(_gamepadCameraPrompt, PromptPosition.UpperRight);
            Locator.GetPromptManager().RemoveScreenPrompt(_keyboardCameraPrompt, PromptPosition.UpperRight);

            GlobalMessenger.RemoveListener("GamePaused", OnGamePaused);
            GlobalMessenger.RemoveListener("GameUnpaused", OnGameUnpaused);
            GlobalMessenger.RemoveListener("WakeUp", OnWakeUp);
        }

        private void Update()
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
            if (_enabled && canUse)
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
