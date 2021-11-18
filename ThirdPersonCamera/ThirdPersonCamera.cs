using OWML.ModHelper;
using OWML.Common;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System;

namespace ThirdPersonCamera
{
    public class ThirdPersonCamera
    {
        private static GameObject _thirdPersonCamera;
        private static Camera _camera;
        private OWCamera _OWCamera;

        private static bool pilotingShip = false;

        private float distance = 0f;
        private float desiredDistance = 0f;

        private const float MIN_PLAYER_DISTANCE = 1.0f;
        private const float DEFAULT_PLAYER_DISTANCE = 3.5f;
        private const float MAX_PLAYER_DISTANCE = 5.0f;

        private const float MIN_SHIP_DISTANCE = 10f;
        private const float DEFAULT_SHIP_DISTANCE = 25f;
        private const float MAX_SHIP_DISTANCE = 30f;

        private const float CAMERA_SPEED = 4.0f;

        // Enabled is if we are allowed to be in 3rd person
        // Active is if the player wants to be in 3rd person
        public bool camera_enabled = false;
        public bool camera_active = false;

        private PlayerTool heldTool = null;

        private readonly Main parent;

        private bool resetArmLayerNextTick = false;
        private bool changeTranslatorMaterialNextTick = false;

        private bool zoomedIn = false;

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

            parent.WriteLine("Creating ThirdPersonCamera", MessageType.Info);

            // Different behaviour when piloting the ship or not, so we must track this
            GlobalMessenger.AddListener("ExitFlightConsole", new Callback(OnExitFlightConsole));
            GlobalMessenger<OWRigidbody>.AddListener("EnterFlightConsole", new Callback<OWRigidbody>(OnEnterFlightConsole));

            // Go back to first person on certain actions
            GlobalMessenger<Campfire>.AddListener("EnterRoastingMode", new Callback<Campfire>(DisableCameraOnRoasting));
            GlobalMessenger.AddListener("ExitRoastingMode", new Callback(EnableCamera));

            GlobalMessenger.AddListener("EnterShipComputer", new Callback(DisableCamera));
            GlobalMessenger.AddListener("ExitShipComputer", new Callback(EnableCamera));

            GlobalMessenger.AddListener("EnterMapView", new Callback(DisableCamera));
            GlobalMessenger.AddListener("ExitMapView", new Callback(EnableCamera));

            GlobalMessenger<DeathType>.AddListener("PlayerDeath", new Callback<DeathType>(DisableCameraOnDeath));
            GlobalMessenger.AddListener("TriggerMemoryUplink", new Callback(DisableCamera));
            GlobalMessenger.AddListener("ResetSimulation", new Callback(EnableCamera));

            GlobalMessenger<Signalscope>.AddListener("EnterSignalscopeZoom", new Callback<Signalscope>(DisableCameraOnSignalscopeZoom));
            GlobalMessenger.AddListener("ExitSignalscopeZoom", new Callback(EnableCamera)); 

            // Some custom events
            GlobalMessenger.AddListener("DisableThirdPersonCamera", new Callback(DisableCamera));
            GlobalMessenger.AddListener("EnableThirdPersonCamera", new Callback(EnableCamera));

            // Different behaviour for certain tools
            GlobalMessenger<PlayerTool>.AddListener("OnEquipTool", new Callback<PlayerTool>(OnToolEquiped));
            GlobalMessenger<PlayerTool>.AddListener("OnUnequipTool", new Callback<PlayerTool>(OnToolUnequiped));

            parent.WriteLine("Done creating ThirdPersonCamera", MessageType.Success);
        }

        public void OnDestroy()
        {
            GlobalMessenger.RemoveListener("ExitFlightConsole", new Callback(OnExitFlightConsole));
            GlobalMessenger<OWRigidbody>.RemoveListener("EnterFlightConsole", new Callback<OWRigidbody>(OnEnterFlightConsole));
            GlobalMessenger<Campfire>.RemoveListener("EnterRoastingMode", new Callback<Campfire>(DisableCameraOnRoasting));
            GlobalMessenger.RemoveListener("ExitRoastingMode", new Callback(EnableCamera));
            GlobalMessenger.RemoveListener("EnterShipComputer", new Callback(DisableCamera));
            GlobalMessenger.RemoveListener("ExitShipComputer", new Callback(EnableCamera));
            GlobalMessenger.RemoveListener("EnterMapView", new Callback(DisableCamera));
            GlobalMessenger.RemoveListener("ExitMapView", new Callback(EnableCamera));
            GlobalMessenger<DeathType>.RemoveListener("PlayerDeath", new Callback<DeathType>(DisableCameraOnDeath));
            GlobalMessenger.RemoveListener("TriggerMemoryUplink", new Callback(DisableCamera));
            GlobalMessenger.RemoveListener("ResetSimulation", new Callback(EnableCamera));
            GlobalMessenger<Signalscope>.RemoveListener("EnterSignalscopeZoom", new Callback<Signalscope>(DisableCameraOnSignalscopeZoom));
            GlobalMessenger.RemoveListener("ExitSignalscopeZoom", new Callback(EnableCamera));
            GlobalMessenger.RemoveListener("DisableThirdPersonCamera", new Callback(DisableCamera));
            GlobalMessenger.RemoveListener("EnableThirdPersonCamera", new Callback(EnableCamera));
            GlobalMessenger<PlayerTool>.RemoveListener("OnEquipTool", new Callback<PlayerTool>(OnToolEquiped));
            GlobalMessenger<PlayerTool>.RemoveListener("OnUnequipTool", new Callback<PlayerTool>(OnToolUnequiped));

            parent.WriteLine($"Done destroying {nameof(ThirdPersonCamera)}", MessageType.Success);
        }

        public void PreInit()
        {
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

            desiredDistance = DEFAULT_PLAYER_DISTANCE;
        }

        public void Init()
        {
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
            camera_enabled = false;

            HUDparent = GameObject.Find("Helmet").transform.parent;
        }

        public void RefreshCamera()
        {
            DeactivateCamera();
            ActivateCamera();
        }

        private void OnToolEquiped(PlayerTool tool)
        {
            parent.WriteLine($"Picked up: {tool.name}, {tool.GetType().Name}, {tool.gameObject.layer}", MessageType.Info);

            heldTool = tool;

            SetToolRenderQueue(tool);

            // Arm is now invisible
            if (camera_active && camera_enabled)
            {
                SetArmVisibility(true);
                resetArmLayerNextTick = true;
            }
            else SetArmVisibility(false);

            try
            {
                // We make some of the models look larger for the 3rd person view
                if (tool.name != "ProbeLauncher" && tool.name != "NomaiTranslatorProp" && tool.name != "TutorialCamera_Base")
                {
                    tool.transform.localScale = new Vector3(2, 2, 2);
                }
            }
            catch(Exception)
            {
                parent.WriteLine($"Couldn't find player tool", MessageType.Warning);
            }
        }

        private void OnToolUnequiped(PlayerTool tool)
        {
            // If we were zoomed in and then uneqiped the signalscope
            if(zoomedIn)
            {
                zoomedIn = false;
                EnableCamera();
            }

            parent.WriteInfo($"Dropped tool");
            heldTool = null;
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
            parent.WriteLine($"Now {(piloting ? "entering" : "exiting")} the ship. Adjusting camera", MessageType.Info);

            pilotingShip = piloting;

            // Ensure the distance is within bounds
            float minDistance = piloting ? MIN_SHIP_DISTANCE : MIN_PLAYER_DISTANCE;
            float maxDistance = piloting ? MAX_SHIP_DISTANCE : MAX_PLAYER_DISTANCE;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            desiredDistance = pilotingShip ? DEFAULT_SHIP_DISTANCE : DEFAULT_PLAYER_DISTANCE;
        }

        private void SetToolRenderQueue(PlayerTool tool)
        {
            if (tool == null) return;

            bool thirdPerson = (camera_active && camera_enabled);

            MeshRenderer[] meshRenderers = tool.GetComponentsInChildren<MeshRenderer>();
            if (tool.name == "Signalscope")
            {
                foreach (MeshRenderer meshRenderer in meshRenderers)
                {
                    meshRenderer.material.renderQueue = thirdPerson ? 0 : 2500;
                }
            }

            if (tool.name == "ProbeLauncher")
            {
                foreach (MeshRenderer meshRenderer in meshRenderers)
                {
                    meshRenderer.material.renderQueue = thirdPerson ? 0 : 2500;
                }
            }

            if (tool.name == "NomaiTranslatorProp")
            {
                changeTranslatorMaterialNextTick = true;
            }

            if (tool.name == "ItemCarryTool")
            {
                // This is maybe the artifact I hope
                foreach (MeshRenderer meshRenderer in meshRenderers)
                {
                    // Have to keep their relative order
                    if(thirdPerson)
                    {
                        if (meshRenderer.material.renderQueue >= 2000) meshRenderer.material.renderQueue -= 2000;
                    }
                    else
                    {
                        if (meshRenderer.material.renderQueue < 2000) meshRenderer.material.renderQueue += 2000;
                    }
                }
            }
        }

        private void SetTranslatorMaterials()
        {
            bool thirdPerson = (camera_active && camera_enabled);

            MeshRenderer[] meshRenderers = Locator.GetPlayerBody().gameObject.GetComponentsInChildren<MeshRenderer>();
            foreach(MeshRenderer meshRenderer in meshRenderers)
            {
                if (meshRenderer.name.Contains("Translator"))
                {
                    meshRenderer.material.renderQueue = thirdPerson ? 0 : 2500;
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

        private void EnableCamera()
        {
            parent.WriteLine($"Third person camera enabled", MessageType.Info);

            camera_enabled = true;
            if (camera_active) ActivateCamera();
        }

        private void DisableCameraOnDeath(DeathType _t)
        {
            DisableCamera();
        }

        private void DisableCameraOnRoasting(Campfire _f)
        {
            DisableCamera();
        }

        private void DisableCameraOnSignalscopeZoom(Signalscope _s)
        {
            DisableCamera();
        }

        private void DisableCamera()
        {
            parent.WriteLine($"Third person camera disabled", MessageType.Info);

            camera_enabled = false;
            DeactivateCamera();
        }

        private void ActivateCamera()
        {
            camera_active = true;

            parent.WriteLine($"Third person camera now active", MessageType.Info);

            if(Locator.GetActiveCamera() != _OWCamera)
            {
                GlobalMessenger<OWCamera>.FireEvent("SwitchActiveCamera", _OWCamera);
                Locator.GetPlayerCamera().mainCamera.enabled = false;
                _camera.enabled = true;
            }

            // Default the distance to the smallest possible value
            float minDistance = pilotingShip ? MIN_SHIP_DISTANCE : MIN_PLAYER_DISTANCE;
            float maxDistance = pilotingShip ? MAX_SHIP_DISTANCE : MAX_PLAYER_DISTANCE;

            desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);
            distance = Mathf.Clamp(distance, minDistance, maxDistance);

            ModifyHelmetHUD(false);
            SetArmVisibility(true);
            SetToolRenderQueue(heldTool);
        }

        private void DeactivateCamera()
        {
            // Only if the player chose this
            if (camera_enabled) camera_active = false;

            parent.WriteLine($"Third person camera deactivated", MessageType.Info);

            if (Locator.GetActiveCamera() == _OWCamera)
            {
                GlobalMessenger<OWCamera>.FireEvent("SwitchActiveCamera", Locator.GetPlayerCamera());
                Locator.GetPlayerCamera().mainCamera.enabled = true;
                _camera.enabled = false;
            }

            distance = 0f;

            ModifyHelmetHUD(true);
            SetArmVisibility(heldTool != null);
            SetToolRenderQueue(heldTool);
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
            
            if (OWInput.IsNewlyReleased(InputLibrary.toolActionPrimary, InputMode.ScopeZoom))
            {
                parent.WriteInfo("Pressed");
                zoomedIn = false;
                EnableCamera();
            }

            if (!camera_enabled) return;

            if (resetArmLayerNextTick)
            {
                SetArmVisibility(true);
                resetArmLayerNextTick = false;
            }

            if(changeTranslatorMaterialNextTick)
            {
                SetTranslatorMaterials();
                changeTranslatorMaterialNextTick = false;
            }

            float scroll = -Mouse.current.scroll.ReadValue().y;

            // Toggle
            if(Keyboard.current[Key.V].wasReleasedThisFrame || Gamepad.current[UnityEngine.InputSystem.LowLevel.GamepadButton.DpadLeft].wasReleasedThisFrame)
            {
                if (camera_active)
                {
                    DeactivateCamera();
                }
                else
                {
                    ActivateCamera();
                }
            }

            if(camera_active)
            {
                if (Locator.GetDeathManager().IsPlayerDying()) DisableCamera();

                float maxDistance = pilotingShip ? MAX_SHIP_DISTANCE : MAX_PLAYER_DISTANCE;
                float minDistance = pilotingShip ? MIN_SHIP_DISTANCE : MIN_PLAYER_DISTANCE;

                // If we change the direction we're scrolling
                if (scroll * (distance - desiredDistance) > 0) desiredDistance = distance;

                // Apply scrolling to distance
                desiredDistance = Mathf.Clamp(desiredDistance + scroll * Time.deltaTime, minDistance, maxDistance);

                // Increment the distance towards the desired distance
                if (distance != desiredDistance)
                {
                    //ModHelper.Console.WriteLine($"Zooming from {distance} to {desiredDistance}", MessageType.Debug);
                    float sign = distance < desiredDistance ? 1 : -1;
                    float camera_speed = CAMERA_SPEED * (pilotingShip ? 2.5f : 1);
                    distance = Mathf.Clamp(distance + sign * camera_speed * Time.deltaTime, 0f, maxDistance);
                    // Did we overshoot?
                    if ((distance < desiredDistance && sign == -1) || (distance > desiredDistance && sign == 1))
                    {
                        distance = desiredDistance;
                    }
                }

                // For raycasting and also moving the camera
                Vector3 origin = Locator.GetPlayerCamera().transform.position;
                if (pilotingShip) origin = Locator.GetShipTransform().position + 10f * Locator.GetShipTransform().TransformDirection(Vector3.up);

                Vector3 direction = _thirdPersonCamera.transform.parent.transform.TransformDirection(Vector3.back);

                // When piloting we temporarily disable the raycast collision for the ship
                if (pilotingShip) Locator.GetShipBody().DisableCollisionDetection();
                else Locator.GetPlayerBody().DisableCollisionDetection();
                if (Physics.Raycast(origin, direction, out RaycastHit hitInfo, desiredDistance))
                {
                    distance = Mathf.Clamp(distance, 0f, hitInfo.distance) * 0.9f; // Try to avoid seeing through curved walls
                }
                if (pilotingShip) Locator.GetShipBody().EnableCollisionDetection();
                else Locator.GetPlayerBody().EnableCollisionDetection();

                // Stop the camera going into your head even if it's inside a wall
                if (distance < minDistance) distance = minDistance;

                // Finally, move the camera into place
                _thirdPersonCamera.transform.position = origin + direction * distance;
            }
        }
    }
}
