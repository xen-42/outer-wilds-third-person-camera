using OWML.ModHelper;
using OWML.Common;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System;
using System.Linq;

namespace ThirdPersonCamera
{
    public class ThirdPersonCamera
    {
        private static GameObject _thirdPersonCamera;
        private static Camera _camera;
        private OWCamera _OWCamera;

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

        private PlayerTool _heldTool = null;

        private readonly Main parent;

        private bool _resetArmLayerNextTick = false;
        private bool _changeToolMaterialNextTick = false;

        private bool isRoastingMarshmallow = false;

        private bool _ejected = false;

        // Things to disappear
        private string[] helmetGUI = {
            "HelmetFrame",
            "HelmetVisorEffects",
            "HelmetRainDroplets",
            "HelmetRainStreaks",
            "HUD_HelmetCracks",
            "DarkMatterBubble"
        };

        private Transform HUDparent = null;

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
            GlobalMessenger.AddListener("OnRetrieveProbe", new Callback(SetToolMaterials));
            GlobalMessenger<ShipDetachableModule>.AddListener("ShipModuleDetached", new Callback<ShipDetachableModule>(OnShipModuleDetached));
            GlobalMessenger.AddListener("OnRoastingStickActivate", new Callback(OnRoastingStickActivate));

            // Different behaviour for certain tools
            GlobalMessenger<PlayerTool>.AddListener("OnEquipTool", new Callback<PlayerTool>(OnToolEquiped));
            GlobalMessenger<PlayerTool>.AddListener("OnUnequipTool", new Callback<PlayerTool>(OnToolUnequiped));

            // GUI
            GlobalMessenger.AddListener("ChangeGUIMode", new Callback(this.OnChangeGUIMode));

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
            GlobalMessenger.RemoveListener("OnRetrieveProbe", new Callback(SetToolMaterials));
            GlobalMessenger<ShipDetachableModule>.RemoveListener("ShipModuleDetached", new Callback<ShipDetachableModule>(OnShipModuleDetached));
            GlobalMessenger.RemoveListener("OnRoastingStickActivate", new Callback(OnRoastingStickActivate));
            GlobalMessenger<PlayerTool>.RemoveListener("OnEquipTool", new Callback<PlayerTool>(OnToolEquiped));
            GlobalMessenger<PlayerTool>.RemoveListener("OnUnequipTool", new Callback<PlayerTool>(OnToolUnequiped));

            GlobalMessenger.RemoveListener("StartViewingProjector", new Callback(DisableCamera));
            GlobalMessenger.RemoveListener("EndViewingProjector", new Callback(EnableCamera));

            GlobalMessenger.RemoveListener("ChangeGUIMode", new Callback(this.OnChangeGUIMode));


            parent.WriteSuccess($"Done destroying {nameof(ThirdPersonCamera)}");
        }

        public void PreInit()
        {
            parent.WriteInfo("PreInit ThirdPersonCamera");

            // Have to do this here or else the skybox breaks
            _thirdPersonCamera = new GameObject();
            _thirdPersonCamera.SetActive(false);

            _camera = _thirdPersonCamera.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.Color;
            _camera.backgroundColor = Color.black;
            _camera.fieldOfView = 80f;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 40000f;
            _camera.depth = 0f;

            _camera.enabled = false;

            _OWCamera = _thirdPersonCamera.AddComponent<OWCamera>();
            _OWCamera.renderSkybox = true;

            _desiredDistance = DEFAULT_PLAYER_DISTANCE;
        }

        public void Init()
        {
            parent.WriteInfo("Init ThirdPersonCamera");

            _thirdPersonCamera.transform.position = Locator.GetPlayerTransform().position;

            // Crashes without this idk
            FlashbackScreenGrabImageEffect temp = _thirdPersonCamera.AddComponent<FlashbackScreenGrabImageEffect>();
            temp._downsampleShader = Locator.GetPlayerCamera().gameObject.GetComponent<FlashbackScreenGrabImageEffect>()._downsampleShader;

            PlanetaryFogImageEffect _image = _thirdPersonCamera.AddComponent<PlanetaryFogImageEffect>();
            _image.fogShader = Locator.GetPlayerCamera().gameObject.GetComponent<PlanetaryFogImageEffect>().fogShader;

            PostProcessingBehaviour _postProcessiong = _thirdPersonCamera.AddComponent<PostProcessingBehaviour>();
            _postProcessiong.profile = Locator.GetPlayerCamera().gameObject.GetAddComponent<PostProcessingBehaviour>().profile;

            _thirdPersonCamera.SetActive(true);
            _camera.cullingMask = Locator.GetPlayerCamera().mainCamera.cullingMask;

            _thirdPersonCamera.transform.parent = Locator.GetPlayerCamera().transform;
            _thirdPersonCamera.transform.position = Locator.GetPlayerCamera().transform.position;
            _thirdPersonCamera.transform.rotation = Locator.GetPlayerCamera().transform.rotation;

            _thirdPersonCamera.name = "ThirdPersonCamera";

            // Now loaded but we default to being disabled
            CameraEnabled = false;
            CameraActive = false;

            HUDparent = GameObject.Find("Helmet").transform.parent;
        }

        private void OnShipModuleDetached(ShipDetachableModule module)
        {
            parent.WriteInfo($"{module.name} was ejected");
            if (module.name == "Module_Cockpit_Body") _ejected = true;
        }

        private void OnToolEquiped(PlayerTool tool)
        {
            parent.WriteInfo($"Picked up: {tool.name}, {tool.GetType().Name}, {tool.gameObject.layer}");

            _heldTool = tool;

            SetToolRenderQueue(tool);

            // Arm is now invisible
            if (CameraActive && CameraEnabled)
            {
                SetArmVisibility(true);
                _resetArmLayerNextTick = true;
            }
            else SetArmVisibility(false);

            try
            {
                // We make some of the models look larger for the 3rd person view

                string[] exemptTools = { "NomaiTranslatorProp", "ProbeLauncher", "TutorialCamera_Base", "TutorialProbeLauncher_Base" };
                if (!exemptTools.Contains(tool.name))
                {
                    tool.transform.localScale = new Vector3(2, 2, 2);
                }
            }
            catch(Exception)
            {
                parent.WriteWarning($"Couldn't find player tool");
            }
        }

        private void OnToolUnequiped(PlayerTool tool)
        {
            parent.WriteInfo($"Dropped tool");
            _heldTool = null;
            SetArmVisibility(true);
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
            parent.WriteInfo($"Now {(piloting ? "entering" : "exiting")} the ship. Adjusting camera");

            _pilotingShip = piloting;

            // Ensure the distance is within bounds
            float minDistance = piloting ? MIN_SHIP_DISTANCE : MIN_PLAYER_DISTANCE;
            float maxDistance = piloting ? MAX_SHIP_DISTANCE : MAX_PLAYER_DISTANCE;
            _distance = Mathf.Clamp(_distance, minDistance, maxDistance);
            _desiredDistance = _pilotingShip ? DEFAULT_SHIP_DISTANCE : DEFAULT_PLAYER_DISTANCE;
        }

        private void SetToolRenderQueue(PlayerTool tool)
        {
            if (tool == null) return;

            bool thirdPerson = (CameraActive && CameraEnabled);

            MeshRenderer[] meshRenderers = tool.GetComponentsInChildren<MeshRenderer>();
            if (tool.name == "Signalscope")
            {
                foreach (MeshRenderer meshRenderer in meshRenderers)
                {
                    meshRenderer.material.renderQueue = thirdPerson ? 0 : 2500;
                }
            }

            string[] laterTickTools = { "NomaiTranslatorProp", "ProbeLauncher", "ItemCarryTool", "TutorialProbeLauncher_Base" };
            if (laterTickTools.Contains(tool.name)) 
            {
                _changeToolMaterialNextTick = true;
            }
        }

        private void SetToolMaterials()
        {
            if (_heldTool == null) return;

            bool thirdPerson = (CameraActive && CameraEnabled);

            MeshRenderer[] meshRenderers = _heldTool.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                // Have to keep their relative order
                if (thirdPerson)
                {
                    foreach(Material m in meshRenderer.materials)
                    {
                        if (m.renderQueue >= 2000) m.renderQueue -= 2000;
                        //parent.WriteInfo($"{m.renderQueue}, {m.shader.name}, {m.shader.renderQueue}");
                    }
                }
                else
                {
                    foreach (Material m in meshRenderer.materials)
                    {
                        if (m.renderQueue < 2000) m.renderQueue += 2000;
                    }
                }
            }
        }

        private void ModifyHelmetHUD(bool visible)
        {
            // Disappear things
            foreach(string s in helmetGUI)
            {
                GameObject go = GameObject.Find(s);
                if (go == null) parent.WriteError($"Couldn't find {s}");
                else go.transform.localScale = visible ? new Vector3(1, 1, 1) : new Vector3(0, 0, 0);
            }

            // Reparent the rest of the GUI to the 3rd person camera
            // Unity preserves the global position not local when reparenting
            GameObject helmet = GameObject.Find("Helmet");
            Vector3 localPosition = helmet.transform.localPosition;
            helmet.transform.SetParent(visible ? HUDparent : _camera.transform);
            helmet.transform.localPosition = localPosition;

            // TEMPORARY just erase it all
            helmet.transform.localScale = visible ? new Vector3(1, 1, 1) : new Vector3(0, 0, 0);

            // The head
            if (!Locator.GetPlayerSuit().IsWearingHelmet())
            {
                try
                {
                    GameObject head = GameObject.Find("player_mesh_noSuit:Player_Head");
                    if (head.layer != 0) head.layer = 0;
                    head.transform.localScale = visible ? new Vector3(0, 0, 0) : new Vector3(1, 1, 1);
                }
                catch (Exception)
                {
                    parent.WriteError("Couldn't find the player's head");
                }
            }
        }

        private void OnRoastingStickActivate()
        {
            // Put the stick back to normal
            GameObject.Find("Stick_Root").transform.localScale = new Vector3(1, 1, 1);
        }

        private void OnExitRoastingMode()
        {
            isRoastingMarshmallow = false;
            EnableCamera();

            GameObject.Find("Stick_Root").transform.localScale = new Vector3(0, 0, 0);
        }

        private void OnChangeGUIMode()
        {
            parent.WriteInfo($"Gui mode changed. HUD Markers visible {GUIMode.AreHUDMarkersVisible()}. {GUIMode.IsFPSMode()}");
        }

        public void EnableCamera()
        {
            if (CameraEnabled) return;
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

            parent.WriteInfo($"Third person camera now active");

            if(Locator.GetActiveCamera() != _OWCamera)
            {
                GlobalMessenger<OWCamera>.FireEvent("SwitchActiveCamera", _OWCamera);
                Locator.GetPlayerCamera().mainCamera.enabled = false;
                _camera.enabled = true;
            }

            // Default the distance to the smallest possible value
            float minDistance = _pilotingShip ? MIN_SHIP_DISTANCE : MIN_PLAYER_DISTANCE;
            float maxDistance = _pilotingShip ? MAX_SHIP_DISTANCE : MAX_PLAYER_DISTANCE;

            _desiredDistance = Mathf.Clamp(_desiredDistance, minDistance, maxDistance);
            _distance = Mathf.Clamp(_distance, minDistance, maxDistance);

            ModifyHelmetHUD(false);
            SetArmVisibility(true);
            SetToolRenderQueue(_heldTool);
        }

        private void DeactivateCamera()
        {
            // Only if the player chose this
            if (CameraEnabled) CameraActive = false;

            parent.WriteInfo($"Third person camera deactivated");

            if (Locator.GetActiveCamera() == _OWCamera)
            {
                GlobalMessenger<OWCamera>.FireEvent("SwitchActiveCamera", Locator.GetPlayerCamera());
                Locator.GetPlayerCamera().mainCamera.enabled = true;
                _camera.enabled = false;
            }

            _distance = 0f;

            ModifyHelmetHUD(true);
            // Double check that theyre holding thing and ya
            if (_heldTool != null && !_heldTool.IsEquipped()) _heldTool = null;
            SetArmVisibility(_heldTool == null);
            SetToolRenderQueue(_heldTool);
        }

        private void SetArmVisibility(bool visible)
        {
            GameObject suitArm = GameObject.Find("Traveller_Mesh_v01:PlayerSuit_RightArm");
            GameObject fleshArm = GameObject.Find("player_mesh_noSuit:Player_RightArm");

            if(suitArm == null && fleshArm == null)
            {
                parent.WriteError("Can't find arm");
            }
            else
            {
                GameObject arm = suitArm ?? fleshArm;

                arm.layer = visible ? 0 : 22;
            }
        }

        public void Update()
        {
            if (!CameraEnabled) return;

            if (_resetArmLayerNextTick)
            {
                SetArmVisibility(true);
                _resetArmLayerNextTick = false;
            }

            if(_changeToolMaterialNextTick)
            {
                SetToolMaterials();
                _changeToolMaterialNextTick = false;
            }

            float scroll = -Mouse.current.scroll.ReadValue().y;

            // If a keyboard/gamepad aren't plugged in then these are null
            bool toggle = false;
            if (Keyboard.current != null) toggle |= Keyboard.current[Key.V].wasReleasedThisFrame;
            if (Gamepad.current != null) toggle |= Gamepad.current[UnityEngine.InputSystem.LowLevel.GamepadButton.DpadLeft].wasReleasedThisFrame;

            // Toggle
            if (toggle)
            {
                if (CameraActive)
                {
                    DeactivateCamera();
                }
                else
                {
                    ActivateCamera();
                }
            }

            if(CameraActive)
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
                Vector3 origin = Locator.GetPlayerCamera().transform.position;
                if (_pilotingShip && !_ejected) origin = Locator.GetShipTransform().position + 10f * Locator.GetShipTransform().TransformDirection(Vector3.up);

                Vector3 direction = _thirdPersonCamera.transform.parent.transform.TransformDirection(Vector3.back);

                // When piloting we temporarily disable the raycast collision for the ship
                if (_pilotingShip) Locator.GetShipBody().DisableCollisionDetection();
                else Locator.GetPlayerBody().DisableCollisionDetection();
                if (Physics.Raycast(origin, direction, out RaycastHit hitInfo, _desiredDistance, ~(1<<2)))
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
    }
}
