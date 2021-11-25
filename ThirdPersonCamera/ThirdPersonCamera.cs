using OWML.ModHelper;
using OWML.Common;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;
using System.Linq;

namespace ThirdPersonCamera
{
    public class ThirdPersonCamera
    {
        private static GameObject _thirdPersonCamera;
        private static Camera  _camera;
        public static OWCamera OWCamera { get; private set; }

        private GameObject cameraPivot;

        private float _distance = 0f;
        private float _desiredDistance = 0f;

        private const float MIN_PLAYER_DISTANCE = 1f;
        private const float DEFAULT_PLAYER_DISTANCE = 3f;
        private const float MAX_PLAYER_DISTANCE = 10.0f;

        private const float MIN_PLAYER_SUIT_DISTANCE = 1.5f;
        private const float DEFAULT_PLAYER_SUIT_DISTANCE = 4f;
        private const float MAX_PLAYER_SUIT_DISTANCE = 15f;

        private const float MIN_SHIP_DISTANCE = 10f;
        private const float DEFAULT_SHIP_DISTANCE = 25f;
        private const float MAX_SHIP_DISTANCE = 60f;

        private const float CAMERA_SPEED = 4.0f;

        // Enabled is if we are allowed to be in 3rd person
        // Active is if the player wants to be in 3rd person
        public bool CameraEnabled { get; set; } 
        public bool CameraActive { get; private set; } 

        private bool isRoastingMarshmallow = false;

        private bool _ejected = false;

        private bool cameraModeLocked = false;

        public bool JustStartedLoop = true;


        public static OWCamera CurrentCamera { get; private set; }
        public static OWCamera PreviousCamera { get; private set; }

        public ThirdPersonCamera()
        {
            Main.WriteInfo("Creating ThirdPersonCamera");

            // Different behaviour when piloting the ship or not, so we must track this
            GlobalMessenger.AddListener("ExitFlightConsole", new Callback(OnExitFlightConsole));
            GlobalMessenger<OWRigidbody>.AddListener("EnterFlightConsole", new Callback<OWRigidbody>(OnEnterFlightConsole));

            // Go back to first person on certain actions
            GlobalMessenger<Campfire>.AddListener("EnterRoastingMode", new Callback<Campfire>(DisableCameraOnRoasting));
            GlobalMessenger.AddListener("ExitRoastingMode", new Callback(OnExitRoastingMode));

            GlobalMessenger.AddListener("EnterShipComputer", new Callback(DisableCamera));
            GlobalMessenger.AddListener("ExitShipComputer", new Callback(EnableCamera));

            GlobalMessenger<DeathType>.AddListener("PlayerDeath", new Callback<DeathType>(DisableCameraOnDeath));
            GlobalMessenger.AddListener("TriggerMemoryUplink", new Callback(DisableCamera));
            GlobalMessenger.AddListener("ResetSimulation", new Callback(EnableCamera));

            GlobalMessenger<Signalscope>.AddListener("EnterSignalscopeZoom", new Callback<Signalscope>(DisableCameraOnSignalscopeZoom));
            GlobalMessenger.AddListener("ExitSignalscopeZoom", new Callback(EnableCamera));

            GlobalMessenger.AddListener("StartViewingProjector", new Callback(DisableCamera));
            GlobalMessenger.AddListener("EndViewingProjector", new Callback(EnableCamera));

            // Some custom events
            GlobalMessenger.AddListener("DisableThirdPersonCamera", new Callback(DisableCamera));
            GlobalMessenger.AddListener("EnableThirdPersonCamera", new Callback(EnableCamera));

            GlobalMessenger<ShipDetachableModule>.AddListener("ShipModuleDetached", new Callback<ShipDetachableModule>(OnShipModuleDetached));
            GlobalMessenger.AddListener("OnRoastingStickActivate", new Callback(OnRoastingStickActivate));

            // Different behaviour for certain tools
            GlobalMessenger<PlayerTool>.AddListener("OnEquipTool", new Callback<PlayerTool>(OnToolEquiped));
            GlobalMessenger<PlayerTool>.AddListener("OnUnequipTool", new Callback<PlayerTool>(OnToolUnequiped));

            GlobalMessenger<OWCamera>.AddListener("SwitchActiveCamera", new Callback<OWCamera>(OnSwitchActiveCamera));

            Main.WriteSuccess("Done creating ThirdPersonCamera");
        }

        public void OnDestroy()
        {
            GlobalMessenger.RemoveListener("ExitFlightConsole", new Callback(OnExitFlightConsole));
            GlobalMessenger<OWRigidbody>.RemoveListener("EnterFlightConsole", new Callback<OWRigidbody>(OnEnterFlightConsole));
            GlobalMessenger<Campfire>.RemoveListener("EnterRoastingMode", new Callback<Campfire>(DisableCameraOnRoasting));
            GlobalMessenger.RemoveListener("ExitRoastingMode", new Callback(EnableCamera));
            GlobalMessenger.RemoveListener("EnterShipComputer", new Callback(DisableCamera));
            GlobalMessenger.RemoveListener("ExitShipComputer", new Callback(EnableCamera));
            GlobalMessenger<DeathType>.RemoveListener("PlayerDeath", new Callback<DeathType>(DisableCameraOnDeath));
            GlobalMessenger.RemoveListener("TriggerMemoryUplink", new Callback(DisableCamera));
            GlobalMessenger.RemoveListener("ResetSimulation", new Callback(EnableCamera));
            GlobalMessenger<Signalscope>.RemoveListener("EnterSignalscopeZoom", new Callback<Signalscope>(DisableCameraOnSignalscopeZoom));
            GlobalMessenger.RemoveListener("ExitSignalscopeZoom", new Callback(EnableCamera));
            GlobalMessenger.RemoveListener("DisableThirdPersonCamera", new Callback(DisableCamera));
            GlobalMessenger.RemoveListener("EnableThirdPersonCamera", new Callback(EnableCamera));

            GlobalMessenger<ShipDetachableModule>.RemoveListener("ShipModuleDetached", new Callback<ShipDetachableModule>(OnShipModuleDetached));
            GlobalMessenger.RemoveListener("OnRoastingStickActivate", new Callback(OnRoastingStickActivate));
            GlobalMessenger<PlayerTool>.RemoveListener("OnEquipTool", new Callback<PlayerTool>(OnToolEquiped));
            GlobalMessenger<PlayerTool>.RemoveListener("OnUnequipTool", new Callback<PlayerTool>(OnToolUnequiped));

            GlobalMessenger.RemoveListener("StartViewingProjector", new Callback(DisableCamera));
            GlobalMessenger.RemoveListener("EndViewingProjector", new Callback(EnableCamera));

            GlobalMessenger<OWCamera>.RemoveListener("SwitchActiveCamera", new Callback<OWCamera>(OnSwitchActiveCamera));

            Main.WriteSuccess($"Done destroying {nameof(ThirdPersonCamera)}");
        }

        public void PreInit()
        {
            Main.WriteInfo("PreInit ThirdPersonCamera");

            // Have to do this here or else the skybox breaks
            _thirdPersonCamera = new GameObject();
            _thirdPersonCamera.SetActive(false);

            _camera = _thirdPersonCamera.AddComponent<Camera>();
            _camera.enabled = false;

            OWCamera = _thirdPersonCamera.AddComponent<OWCamera>();
            OWCamera.renderSkybox = true;

            _desiredDistance = DEFAULT_PLAYER_DISTANCE;
        }

        public void Init()
        {
            Main.WriteInfo("Init ThirdPersonCamera");

            // Crashes without this idk stole it from Nebulas FreeCam
            FlashbackScreenGrabImageEffect temp = _thirdPersonCamera.AddComponent<FlashbackScreenGrabImageEffect>();
            temp._downsampleShader = Locator.GetPlayerCamera().gameObject.GetComponent<FlashbackScreenGrabImageEffect>()._downsampleShader;

            PlanetaryFogImageEffect _image = _thirdPersonCamera.AddComponent<PlanetaryFogImageEffect>();
            _image.fogShader = Locator.GetPlayerCamera().gameObject.GetComponent<PlanetaryFogImageEffect>().fogShader;

            PostProcessingBehaviour _postProcessiong = _thirdPersonCamera.AddComponent<PostProcessingBehaviour>();
            _postProcessiong.profile = Locator.GetPlayerCamera().gameObject.GetAddComponent<PostProcessingBehaviour>().profile;

            _thirdPersonCamera.SetActive(true);
            _camera.CopyFrom(Locator.GetPlayerCamera().mainCamera);

            cameraPivot = new GameObject();
            cameraPivot.transform.parent = Locator.GetPlayerCamera().transform;
            cameraPivot.transform.position = Locator.GetPlayerCamera().transform.position;
            cameraPivot.transform.rotation = Locator.GetPlayerCamera().transform.rotation;
            cameraPivot.transform.localPosition = Vector3.up * 0.2f;

            _thirdPersonCamera.transform.parent = cameraPivot.transform;
            _thirdPersonCamera.transform.position = cameraPivot.transform.position;
            _thirdPersonCamera.transform.rotation = cameraPivot.transform.rotation;

            _thirdPersonCamera.name = "ThirdPersonCamera";

            // Now loaded but we default to being disabled
            CameraEnabled = false;
            CameraActive = true;
            JustStartedLoop = true;

            _ejected = false;

            CurrentCamera = Locator.GetPlayerCamera();
        }

        private float GetMinDistance()
        {
            if (PlayerState.AtFlightConsole()) return MIN_SHIP_DISTANCE;
            if (Locator.GetPlayerSuit().IsWearingSuit()) return MIN_PLAYER_SUIT_DISTANCE;
            else return MIN_PLAYER_DISTANCE;
        }

        private float GetMaxDistance()
        {
            if (PlayerState.AtFlightConsole()) return MAX_SHIP_DISTANCE;
            if (Locator.GetPlayerSuit().IsWearingSuit()) return MAX_PLAYER_SUIT_DISTANCE;
            else return MAX_PLAYER_DISTANCE;
        }

        private void OnShipModuleDetached(ShipDetachableModule module)
        {
            if (module.name == "Module_Cockpit_Body") _ejected = true;
        }

        private void OnToolEquiped(PlayerTool tool)
        {
            //if (_pilotingShip) DisableCamera();
        }

        private void OnToolUnequiped(PlayerTool tool)
        {
            //if (_pilotingShip) EnableCamera();
        }

        private void OnExitFlightConsole()
        {
            SetPilotingShip(false);
        }

        private void OnEnterFlightConsole(OWRigidbody _owrb)
        {
            SetPilotingShip(true);
        }

        private void SetPilotingShip(bool piloting)
        {
            // Ensure the distance is within bounds
            float minDistance = GetMinDistance();
            float maxDistance = GetMaxDistance();

            _distance = Mathf.Clamp(_distance, minDistance, maxDistance);
            if (piloting) _desiredDistance = DEFAULT_SHIP_DISTANCE;
            else if (Locator.GetPlayerSuit().IsWearingSuit()) _desiredDistance = DEFAULT_PLAYER_SUIT_DISTANCE;
            else _desiredDistance = DEFAULT_PLAYER_DISTANCE; ;
        }

        private void OnRoastingStickActivate()
        {
            // Put the stick back to normal
            GameObject stick = GameObject.Find("Stick_Root");
            if (stick != null) stick.transform.localScale = new Vector3(1, 1, 1);
            else Main.WriteWarning("Can't find stick");
        }

        private void OnExitRoastingMode()
        {
            isRoastingMarshmallow = false;
            EnableCamera();

            GameObject stick = GameObject.Find("Stick_Root");
            if (stick != null) stick.transform.localScale = new Vector3(0, 0, 0);
            else Main.WriteWarning("Can't find stick");
        }

        private void OnSwitchActiveCamera(OWCamera camera)
        { 
            PreviousCamera = CurrentCamera;
            CurrentCamera = camera;

            Main.WriteInfo($"Switched from {PreviousCamera.name} to {CurrentCamera.name}");

            switch (PreviousCamera.name)
            {
                case "MapCamera":
                    cameraModeLocked = false;
                    break;
                case "LandingCamera":
                    cameraModeLocked = false;
                    if (CameraActive && CameraEnabled)
                    {
                        ActivateCamera(false);
                        OnSwitchActiveCamera(OWCamera);
                    }
                    break;
                case "RemoteViewerCamera":
                    cameraModeLocked = false;
                    break;
                case "FREECAM":
                    cameraModeLocked = false;
                    break;
            }

            switch (CurrentCamera.name)
            {
                case "PlayerCamera":
                    if (CameraActive && CameraEnabled)
                    {
                        ActivateCamera(true);
                        OnSwitchActiveCamera(OWCamera);
                    }
                    break;
                case "MapCamera":
                    cameraModeLocked = true;
                    break;
                case "LandingCamera":
                    cameraModeLocked = true;
                    break;
                case "RemoteViewerCamera":
                    cameraModeLocked = true;
                    break;
                case "FREECAM":
                    CameraActive = false;
                    cameraModeLocked = true;
                    break;
            }
        }

        public void EnableCamera()
        {
            if (CameraEnabled || OWCamera == null) return;
            // If you're quick you can go from sleeping right into roasting before your eyes fully open (enables 3rd person)
            if (isRoastingMarshmallow) return;

            Main.WriteInfo($"Third person camera enabled");

            CameraEnabled = true;
            if (CameraActive) ActivateCamera();
        }

        private void DisableCameraOnDeath(DeathType _t)
        {
            DisableCamera();
        }

        private void DisableCameraOnRoasting(Campfire _f)
        {
            isRoastingMarshmallow = true;
            DisableCamera();
        }

        private void DisableCameraOnSignalscopeZoom(Signalscope _s)
        {
            DisableCamera();
        }

        public void DisableCamera()
        {
            if (!CameraEnabled) return;

            Main.WriteInfo($"Third person camera disabled");

            CameraEnabled = false;
            DeactivateCamera();
        }

        public void ActivateCamera(bool fireSwitchActiveCamera = true)
        {
            Main.WriteInfo("Activate third person camera");

            CameraActive = true;
            GlobalMessenger.FireEvent("ActivateThirdPersonCamera");

            // Don't actually change camera because here we just move it
            if (CurrentCamera.name.Equals("RemoteViewerCamera")) return;

            try
            {
                if (fireSwitchActiveCamera && (OWCamera != Locator.GetActiveCamera() || JustStartedLoop)) GlobalMessenger<OWCamera>.FireEvent("SwitchActiveCamera", OWCamera);
            }
            catch (Exception)
            {
                Main.WriteWarning("Couldn't fire event");
            }

            Locator.GetPlayerCamera().mainCamera.enabled = false;
            _camera.enabled = true;

            // Default the distance to the smallest possible value
            float minDistance = GetMinDistance();
            float maxDistance = GetMaxDistance();

            _desiredDistance = Mathf.Clamp(_desiredDistance, minDistance, maxDistance);
            _distance = Mathf.Clamp(_distance, minDistance, maxDistance);
        }

        public void DeactivateCamera()
        {
            Main.WriteInfo("Deactivate third person camera");

            // Only if the player chose this do we record it as inactive
            if (CameraEnabled) CameraActive = false;

            GlobalMessenger.FireEvent("DeactivateThirdPersonCamera");

            // Don't actually change camera because here we just move it
            if (CurrentCamera.name.Equals("RemoteViewerCamera")) return;

            try
            {
                if (Locator.GetActiveCamera() != Locator.GetPlayerCamera()) GlobalMessenger<OWCamera>.FireEvent("SwitchActiveCamera", Locator.GetPlayerCamera());
            }
            catch (Exception) 
            { 
                Main.WriteWarning("Couldn't fire event"); 
            }

            Locator.GetPlayerCamera().mainCamera.enabled = true;
            _camera.enabled = false;
            _distance = 0f;
        }

        public void Update()
        {
            bool toggle = false;
            if (Keyboard.current != null)
            {
                toggle |= Keyboard.current[Key.V].wasReleasedThisFrame;
            }
            if (Gamepad.current != null)
            {
                toggle |= Gamepad.current[UnityEngine.InputSystem.LowLevel.GamepadButton.DpadLeft].wasReleasedThisFrame;
            }

            if (!CameraEnabled)
            {
                if (toggle)
                {
                    Locator.GetPlayerAudioController().PlayNegativeUISound();
                    toggle = false;
                }
            }

            float scroll = -Mouse.current.scroll.ReadValue().y;

            // Toggle
            if(toggle)
            {
                if (!cameraModeLocked)
                {
                    if (CameraActive)
                    {
                        Locator.GetPlayerAudioController().PlayLockOff();
                        DeactivateCamera();
                    }
                    else
                    {
                        Locator.GetPlayerAudioController().PlayLockOn();
                        ActivateCamera();
                    }
                }
                else Locator.GetPlayerAudioController().PlayNegativeUISound();
            }

            if (CameraActive)
            {
                if (Locator.GetDeathManager().IsPlayerDying()) DisableCamera();

                float maxDistance = GetMaxDistance();
                float minDistance = GetMinDistance();

                // If we change the direction we're scrolling
                if (scroll * (_distance - _desiredDistance) > 0) _desiredDistance = _distance;

                // Apply scrolling to distance
                _desiredDistance = Mathf.Clamp(_desiredDistance + scroll * Time.deltaTime, minDistance, maxDistance);

                // Increment the distance towards the desired distance
                if (_distance != _desiredDistance)
                {
                    //ModHelper.Console.WriteLine($"Zooming from {distance} to {desiredDistance}", MessageType.Debug);
                    float sign = _distance < _desiredDistance ? 1 : -1;
                    float camera_speed = CAMERA_SPEED * (PlayerState.AtFlightConsole() ? 2.5f : 1);
                    _distance = Mathf.Clamp(_distance + sign * camera_speed * Time.deltaTime, 0f, maxDistance);
                    // Did we overshoot?
                    if ((_distance < _desiredDistance && sign == -1) || (_distance > _desiredDistance && sign == 1))
                    {
                        _distance = _desiredDistance;
                    }
                }

                // For raycasting and also moving the camera
                Vector3 origin = cameraPivot.transform.position;
                if (PlayerState.AtFlightConsole() && !_ejected) origin = Locator.GetShipTransform().position + 10f * Locator.GetShipTransform().TransformDirection(Vector3.up);

                Vector3 direction = _thirdPersonCamera.transform.parent.transform.TransformDirection(Vector3.back);

                // When piloting we temporarily disable the raycast collision for the ship
                if (PlayerState.AtFlightConsole()) Locator.GetShipBody().DisableCollisionDetection();
                else Locator.GetPlayerBody().DisableCollisionDetection();
                //int layerMask = (1 << 0) | (1 << 9) | (1 << 10) | (1 << 15) | (1 << 22) | (1 << 27) | (1 << 28);
                int layerMask = OWLayerMask.physicalMask;
                if (Physics.Raycast(origin, direction, out RaycastHit hitInfo, _desiredDistance, layerMask))
                {
                    //Main.WriteInfo($"{hitInfo.collider.gameObject.layer}");
                    _distance = Mathf.Clamp(_distance, 0f, hitInfo.distance * 0.8f); // Try to avoid seeing through curved walls
                }
                if (PlayerState.AtFlightConsole()) Locator.GetShipBody().EnableCollisionDetection();
                else Locator.GetPlayerBody().EnableCollisionDetection();

                // Stop the camera going into your head even if it's inside a wall
                if (_distance < minDistance)
                {
                    _distance = minDistance;
                }

                // Finally, move the camera into place
                _thirdPersonCamera.transform.position = origin + direction * _distance;
            }
        }

        public static Camera GetCamera()
        {
            return _camera;
        }
    }
}
