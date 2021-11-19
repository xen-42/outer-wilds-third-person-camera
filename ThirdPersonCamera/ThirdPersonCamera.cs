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
        private readonly Main parent;

        private static GameObject _thirdPersonCamera;
        private static Camera  _camera;
        private OWCamera _OWCamera;

        private GameObject cameraPivot;

        private static bool _pilotingShip = false;

        private float _distance = 0f;
        private float _desiredDistance = 0f;

        private const float MIN_PLAYER_DISTANCE = 1.5f;
        private const float DEFAULT_PLAYER_DISTANCE = 3.5f;
        private const float MAX_PLAYER_DISTANCE = 5.0f;

        private const float MIN_SHIP_DISTANCE = 10f;
        private const float DEFAULT_SHIP_DISTANCE = 25f;
        private const float MAX_SHIP_DISTANCE = 30f;

        private const float CAMERA_SPEED = 4.0f;

        // Enabled is if we are allowed to be in 3rd person
        // Active is if the player wants to be in 3rd person
        public bool CameraEnabled { get; set; } = false;
        public bool CameraActive { get; private set; } = false;

        private bool isRoastingMarshmallow = false;

        private bool _ejected = false;

        private bool cameraModeLocked = false;

        private Transform HUDparent = null;

        private bool overTheShoulder = false;

        public ThirdPersonCamera(Main _main)
        {
            parent = _main;

            parent.WriteInfo("Creating ThirdPersonCamera");

            // Different behaviour when piloting the ship or not, so we must track this
            GlobalMessenger.AddListener("ExitFlightConsole", new Callback(OnExitFlightConsole));
            GlobalMessenger<OWRigidbody>.AddListener("EnterFlightConsole", new Callback<OWRigidbody>(OnEnterFlightConsole));

            // Go back to first person on certain actions
            GlobalMessenger<Campfire>.AddListener("EnterRoastingMode", new Callback<Campfire>(DisableCameraOnRoasting));
            GlobalMessenger.AddListener("ExitRoastingMode", new Callback(OnExitRoastingMode));

            GlobalMessenger.AddListener("EnterShipComputer", new Callback(DisableCamera));
            GlobalMessenger.AddListener("ExitShipComputer", new Callback(EnableCamera));

            GlobalMessenger.AddListener("EnterMapView", new Callback(OnEnterMapView));
            GlobalMessenger.AddListener("ExitMapView", new Callback(OnExitMapView));

            GlobalMessenger<DeathType>.AddListener("PlayerDeath", new Callback<DeathType>(DisableCameraOnDeath));
            GlobalMessenger.AddListener("TriggerMemoryUplink", new Callback(DisableCamera));
            GlobalMessenger.AddListener("ResetSimulation", new Callback(EnableCamera));

            GlobalMessenger<Signalscope>.AddListener("EnterSignalscopeZoom", new Callback<Signalscope>(DisableCameraOnSignalscopeZoom));
            GlobalMessenger.AddListener("ExitSignalscopeZoom", new Callback(EnableCamera));

            //GlobalMessenger<OWCamera>.AddListener("SwitchActiveCamera", OnSwitchActiveCamera);

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

            GlobalMessenger.AddListener("EnterLandingView", new Callback(this.OnEnterLandingView));
            GlobalMessenger.AddListener("ExitLandingView", new Callback(this.OnExitLandingView));

            parent.WriteSuccess("Done creating ThirdPersonCamera");
        }

        public void OnDestroy()
        {
            GlobalMessenger.RemoveListener("ExitFlightConsole", new Callback(OnExitFlightConsole));
            GlobalMessenger<OWRigidbody>.RemoveListener("EnterFlightConsole", new Callback<OWRigidbody>(OnEnterFlightConsole));
            GlobalMessenger<Campfire>.RemoveListener("EnterRoastingMode", new Callback<Campfire>(DisableCameraOnRoasting));
            GlobalMessenger.RemoveListener("ExitRoastingMode", new Callback(EnableCamera));
            GlobalMessenger.RemoveListener("EnterShipComputer", new Callback(DisableCamera));
            GlobalMessenger.RemoveListener("ExitShipComputer", new Callback(EnableCamera));
            //GlobalMessenger.RemoveListener("EnterMapView", new Callback(DisableCamera));
            //GlobalMessenger.RemoveListener("ExitMapView", new Callback(EnableCamera));
            GlobalMessenger<DeathType>.RemoveListener("PlayerDeath", new Callback<DeathType>(DisableCameraOnDeath));
            GlobalMessenger.RemoveListener("TriggerMemoryUplink", new Callback(DisableCamera));
            GlobalMessenger.RemoveListener("ResetSimulation", new Callback(EnableCamera));
            GlobalMessenger<Signalscope>.RemoveListener("EnterSignalscopeZoom", new Callback<Signalscope>(DisableCameraOnSignalscopeZoom));
            GlobalMessenger.RemoveListener("ExitSignalscopeZoom", new Callback(EnableCamera));
            //GlobalMessenger<OWCamera>.RemoveListener("SwitchActiveCamera", OnSwitchActiveCamera);
            GlobalMessenger.RemoveListener("DisableThirdPersonCamera", new Callback(DisableCamera));
            GlobalMessenger.RemoveListener("EnableThirdPersonCamera", new Callback(EnableCamera));

            GlobalMessenger<ShipDetachableModule>.RemoveListener("ShipModuleDetached", new Callback<ShipDetachableModule>(OnShipModuleDetached));
            GlobalMessenger.RemoveListener("OnRoastingStickActivate", new Callback(OnRoastingStickActivate));
            GlobalMessenger<PlayerTool>.RemoveListener("OnEquipTool", new Callback<PlayerTool>(OnToolEquiped));
            GlobalMessenger<PlayerTool>.RemoveListener("OnUnequipTool", new Callback<PlayerTool>(OnToolUnequiped));

            GlobalMessenger.RemoveListener("StartViewingProjector", new Callback(DisableCamera));
            GlobalMessenger.RemoveListener("EndViewingProjector", new Callback(EnableCamera));

            GlobalMessenger.RemoveListener("EnterLandingView", new Callback(this.OnEnterLandingView));
            GlobalMessenger.RemoveListener("ExitLandingView", new Callback(this.OnExitLandingView));

            parent.WriteSuccess($"Done destroying {nameof(ThirdPersonCamera)}");
        }

        public void PreInit()
        {
            parent.WriteInfo("PreInit ThirdPersonCamera");

            // Have to do this here or else the skybox breaks
            _thirdPersonCamera = new GameObject();
            _thirdPersonCamera.SetActive(false);

            _camera = _thirdPersonCamera.AddComponent<Camera>();
            _camera.enabled = false;

            _OWCamera = _thirdPersonCamera.AddComponent<OWCamera>();
            _OWCamera.renderSkybox = true;

            _desiredDistance = DEFAULT_PLAYER_DISTANCE;
        }

        public void Init()
        {
            parent.WriteInfo("Init ThirdPersonCamera");

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

            _thirdPersonCamera.transform.parent = cameraPivot.transform;
            _thirdPersonCamera.transform.position = cameraPivot.transform.position;
            _thirdPersonCamera.transform.rotation = cameraPivot.transform.rotation;

            _thirdPersonCamera.name = "ThirdPersonCamera";

            // Now loaded but we default to being disabled
            CameraEnabled = false;
            CameraActive = false;

            HUDparent = GameObject.Find("Helmet").transform.parent;
        }

        private void OnEnterLandingView()
        {
            cameraModeLocked = true;
        }

        private void OnExitLandingView()
        {
            cameraModeLocked = false;
        }

        private void OnShipModuleDetached(ShipDetachableModule module)
        {
            if (module.name == "Module_Cockpit_Body") _ejected = true;
        }

        private void OnToolEquiped(PlayerTool tool)
        {
            if (_pilotingShip) DisableCamera();
        }

        private void OnToolUnequiped(PlayerTool tool)
        {
            if (_pilotingShip) EnableCamera();
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
            _pilotingShip = piloting;

            // Ensure the distance is within bounds
            float minDistance = piloting ? MIN_SHIP_DISTANCE : MIN_PLAYER_DISTANCE;
            float maxDistance = piloting ? MAX_SHIP_DISTANCE : MAX_PLAYER_DISTANCE;

            _distance = Mathf.Clamp(_distance, minDistance, maxDistance);
            _desiredDistance = _pilotingShip ? DEFAULT_SHIP_DISTANCE : DEFAULT_PLAYER_DISTANCE;

            SetCameraPivotPosition(!_pilotingShip && overTheShoulder);
        }

        private void OnRoastingStickActivate()
        {
            // Put the stick back to normal
            GameObject stick = GameObject.Find("Stick_Root");
            if (stick != null) stick.transform.localScale = new Vector3(1, 1, 1);
            else parent.WriteWarning("Can't find stick");
        }

        private void OnExitRoastingMode()
        {
            isRoastingMarshmallow = false;
            EnableCamera();

            GameObject stick = GameObject.Find("Stick_Root");
            if (stick != null) stick.transform.localScale = new Vector3(0, 0, 0);
            else parent.WriteWarning("Can't find stick");
        }

        private void SetCameraPivotPosition(bool defaultPosition)
        {
            // I wanted to do this but your tools like raycast out of themselves and not out of the camera
            // So I'd have to edit all of them to raycast from the camera pivot point

            //if (defaultPosition) cameraPivot.transform.localPosition = new Vector3(0, 0, 0);
            //else cameraPivot.transform.localPosition = cameraPivot.transform.TransformDirection(Vector3.left);
        }

        public void EnableCamera()
        {
            if (CameraEnabled) return;
            // If you're quick you can go from sleeping right into roasting before your eyes fully open (enables 3rd person)
            if (isRoastingMarshmallow) return;

            parent.WriteInfo($"Third person camera enabled");

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

            parent.WriteInfo($"Third person camera disabled");

            CameraEnabled = false;
            DeactivateCamera();
        }

        public void OnEnterMapView()
        {
            CameraEnabled = false;
        }

        public void OnExitMapView()
        {
            CameraEnabled = true;
        }

        private void ActivateCamera()
        {
            CameraActive = true;
            if (Locator.GetActiveCamera() == _OWCamera) return;

            GlobalMessenger<OWCamera>.FireEvent("SwitchActiveCamera", _OWCamera);
            Locator.GetPlayerCamera().mainCamera.enabled = false;
            _camera.enabled = true;

            // Default the distance to the smallest possible value
            float minDistance = _pilotingShip ? MIN_SHIP_DISTANCE : MIN_PLAYER_DISTANCE;
            float maxDistance = _pilotingShip ? MAX_SHIP_DISTANCE : MAX_PLAYER_DISTANCE;

            _desiredDistance = Mathf.Clamp(_desiredDistance, minDistance, maxDistance);
            _distance = Mathf.Clamp(_distance, minDistance, maxDistance);

            SetCameraPivotPosition(!overTheShoulder || _pilotingShip);

            GlobalMessenger.FireEvent("ActivateThirdPersonCamera");
        }

        private void DeactivateCamera()
        {
            // Only if the player chose this do we record it as inactive
            if (CameraEnabled) CameraActive = false;

            if (Locator.GetActiveCamera() != _OWCamera) return;

            GlobalMessenger<OWCamera>.FireEvent("SwitchActiveCamera", Locator.GetPlayerCamera());

            Locator.GetPlayerCamera().mainCamera.enabled = true;
            _camera.enabled = false;
            _distance = 0f;

            SetCameraPivotPosition(true);

            GlobalMessenger.FireEvent("DeactivateThirdPersonCamera");
        }

        public void Update()
        {
            if (!CameraEnabled) return;

            float scroll = -Mouse.current.scroll.ReadValue().y;

            // If a keyboard/gamepad aren't plugged in then these are null
            bool toggle = false;
            bool toggleShoulder = false;
            if (Keyboard.current != null)
            {
                toggle |= Keyboard.current[Key.V].wasReleasedThisFrame;
                toggleShoulder |= Keyboard.current[Key.B].wasReleasedThisFrame;
            }
            if (Gamepad.current != null)
            {
                toggle |= Gamepad.current[UnityEngine.InputSystem.LowLevel.GamepadButton.DpadLeft].wasReleasedThisFrame;
                toggleShoulder |= Gamepad.current[UnityEngine.InputSystem.LowLevel.GamepadButton.DpadRight].wasReleasedThisFrame;
            }

            // Toggle
            if (!cameraModeLocked)
            {
                if(toggle)
                {
                    if (CameraActive) DeactivateCamera();
                    else ActivateCamera();
                }
                if(toggleShoulder)
                {
                    overTheShoulder = !overTheShoulder;
                    SetCameraPivotPosition(!(overTheShoulder && parent.IsThirdPerson() && !_pilotingShip));
                }
            }

            if (CameraActive)
            {
                if (Locator.GetDeathManager().IsPlayerDying()) DisableCamera();

                float maxDistance = _pilotingShip ? MAX_SHIP_DISTANCE : MAX_PLAYER_DISTANCE;
                float minDistance = _pilotingShip ? MIN_SHIP_DISTANCE : MIN_PLAYER_DISTANCE;

                // If we change the direction we're scrolling
                if (scroll * (_distance - _desiredDistance) > 0) _desiredDistance = _distance;

                // Apply scrolling to distance
                _desiredDistance = Mathf.Clamp(_desiredDistance + scroll * Time.deltaTime, minDistance, maxDistance);

                // Increment the distance towards the desired distance
                if (_distance != _desiredDistance)
                {
                    //ModHelper.Console.WriteLine($"Zooming from {distance} to {desiredDistance}", MessageType.Debug);
                    float sign = _distance < _desiredDistance ? 1 : -1;
                    float camera_speed = CAMERA_SPEED * (_pilotingShip ? 2.5f : 1);
                    _distance = Mathf.Clamp(_distance + sign * camera_speed * Time.deltaTime, 0f, maxDistance);
                    // Did we overshoot?
                    if ((_distance < _desiredDistance && sign == -1) || (_distance > _desiredDistance && sign == 1))
                    {
                        _distance = _desiredDistance;
                    }
                }

                // For raycasting and also moving the camera
                Vector3 origin = cameraPivot.transform.position;
                if (_pilotingShip && !_ejected) origin = Locator.GetShipTransform().position + 10f * Locator.GetShipTransform().TransformDirection(Vector3.up);

                Vector3 direction = _thirdPersonCamera.transform.parent.transform.TransformDirection(Vector3.back);

                // When piloting we temporarily disable the raycast collision for the ship
                if (_pilotingShip) Locator.GetShipBody().DisableCollisionDetection();
                else Locator.GetPlayerBody().DisableCollisionDetection();
                int layerMask = (1 << 0) | (1 << 9) | (1 << 10) | (1 << 22) | (1 << 27) | (1 << 28);
                if (Physics.Raycast(origin, direction, out RaycastHit hitInfo, _desiredDistance, layerMask))
                {
                    _distance = Mathf.Clamp(_distance, 0f, hitInfo.distance) * 0.9f; // Try to avoid seeing through curved walls
                }
                if (_pilotingShip) Locator.GetShipBody().EnableCollisionDetection();
                else Locator.GetPlayerBody().EnableCollisionDetection();

                // Stop the camera going into your head even if it's inside a wall
                if (_distance < minDistance) _distance = minDistance;

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
