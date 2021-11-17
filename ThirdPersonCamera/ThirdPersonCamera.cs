using OWML.ModHelper;
using OWML.Common;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System;

namespace ThirdPersonCamera
{
    public class ThirdPersonCamera : ModBehaviour
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
        private bool camera_enabled = false;
        private bool camera_active = false;

        private bool holdingTool = false;

        private void Awake()
        {
            // You won't be able to access OWML's mod helper in Awake.
            // So you probably don't want to do anything here.
            // Use Start() instead.
        }

        private void Start()
        {
            ModHelper.Console.WriteLine($"{nameof(ThirdPersonCamera)} is loaded!", MessageType.Success);

            // Need to create the camera immediately or the skybox breaks?
            SceneManager.sceneLoaded += OnSceneLoaded;

            // This happens when the player first appears, so we use it to set most stuff up
            ModHelper.Events.Subscribe<Flashlight>(Events.AfterStart);
            ModHelper.Events.Event += OnEvent;

            // Different behaviour when piloting the ship or not, so we must track this
            GlobalMessenger.AddListener("ExitFlightConsole", new Callback(() => SetPilotingShip(false)));
            GlobalMessenger<OWRigidbody>.AddListener("EnterFlightConsole", new Callback<OWRigidbody>((_) => SetPilotingShip(true)));

            // Go back to first person on certain actions
            GlobalMessenger<Campfire>.AddListener("EnterRoastingMode", new Callback<Campfire>((_) => DisableCamera()));
            GlobalMessenger.AddListener("ExitRoastingMode", new Callback(() => EnableCamera()));

            GlobalMessenger.AddListener("EnterShipComputer", new Callback(() => DisableCamera()));
            GlobalMessenger.AddListener("ExitShipComputer", new Callback(() => EnableCamera()));

            //GlobalMessenger<bool>.AddListener("StartSleepingAtCampfire", new Callback<bool>((_) => DisableCamera()));
            //GlobalMessenger.AddListener("StopSleepingAtCampfire", new Callback(() => EnableCamera()));

            GlobalMessenger.AddListener("EnterMapView", new Callback(() => DisableCamera()));
            GlobalMessenger.AddListener("ExitMapView", new Callback(() => EnableCamera()));

            GlobalMessenger<DeathType>.AddListener("PlayerDeath", new Callback<DeathType>((_) => DisableCamera()));
            GlobalMessenger.AddListener("TriggerMemoryUplink", new Callback(() => DisableCamera()));
            

            // Some custom events
            GlobalMessenger.AddListener("DisableThirdPersonCamera", new Callback(() => DisableCamera()));
            GlobalMessenger.AddListener("EnableThirdPersonCamera", new Callback(() => EnableCamera()));

            GlobalMessenger<Type>.AddListener("OnEquipTool", new Callback<Type>((Type t) => OnToolEquipChange(t, true)));
            GlobalMessenger<Type>.AddListener("OnUnequipTool", new Callback<Type>((Type t) => OnToolEquipChange(t, false)));

            ModHelper.HarmonyHelper.AddPostfix<PlayerTool>("EquipTool", typeof(ThirdPersonCameraPatch), nameof(ThirdPersonCameraPatch.EquipTool));
            ModHelper.HarmonyHelper.AddPostfix<PlayerTool>("UnequipTool", typeof(ThirdPersonCameraPatch), nameof(ThirdPersonCameraPatch.UnequipTool));
            
            // OpenEyes doesn't work?
            //ModHelper.HarmonyHelper.AddPrefix<PlayerCameraEffectController>("OpenEyes", typeof(ThirdPersonCameraPatch), nameof(ThirdPersonCameraPatch.OpenEyes));
            ModHelper.HarmonyHelper.AddPrefix<StreamingGroup>("OnFinishOpenEyes", typeof(ThirdPersonCameraPatch), nameof(ThirdPersonCameraPatch.OnFinishOpenEyes));
            ModHelper.HarmonyHelper.AddPrefix<PlayerCameraEffectController>("CloseEyes", typeof(ThirdPersonCameraPatch), nameof(ThirdPersonCameraPatch.CloseEyes));
            
            /*
            GlobalMessenger.AddListener("LockMovement", new Callback(() => OnMovementLockChanged(true)));
            GlobalMessenger.AddListener("UnlockMovement", new Callback(() => OnMovementLockChanged(false)));

            ModHelper.HarmonyHelper.AddPostfix<PlayerCharacterController>("LockMovement", typeof(ThirdPersonCameraPatch), nameof(ThirdPersonCameraPatch.LockMovement));
            ModHelper.HarmonyHelper.AddPostfix<PlayerCharacterController>("UnlockMovement", typeof(ThirdPersonCameraPatch), nameof(ThirdPersonCameraPatch.UnlockMovement));
            */
        }

        private void OnToolEquipChange(Type t, bool equiped)
        {
            ModHelper.Console.WriteLine($"Picked up: {t.Name}", MessageType.Info);

            if (t.Name == "ItemTool")
            {
                try
                {
                    GameObject tool = Locator.GetPlayerBody().GetComponentInChildren<ItemTool>().gameObject;
                    ModHelper.Console.WriteLine($"{tool.name}", MessageType.Info);
                    tool.transform.localScale = new Vector3(2,2,2);
                    ModHelper.Console.WriteLine($"{tool.layer}", MessageType.Info);
                    ModHelper.Console.WriteLine($"{tool.transform.position}", MessageType.Info);
                }
                catch(Exception e)
                {
                    ModHelper.Console.WriteLine($"Couldn't find player tool", MessageType.Warning);
                }
            }
            else
            {
                holdingTool = equiped;
                if (equiped) DisableCamera();
                else EnableCamera();
            }
        }

        private void OnMovementLockChanged(bool locked)
        {
            ModHelper.Console.WriteLine($"Movement is now {(locked? "locked" : "unlocked")}", MessageType.Info);
        }

        private void OnEvent(MonoBehaviour behaviour, Events ev)
        {
            if (behaviour.GetType() == typeof(Flashlight) && ev == Events.AfterStart)
            {
                // Set up the mod
                SetUpCamera();
            }
        }

        private void SetPilotingShip(bool piloting)
        {
            pilotingShip = piloting;
            ModHelper.Console.WriteLine($"Now {(piloting ? "entering" : "exiting")} the ship. Adjusting camera", MessageType.Info);

            // Ensure the distance is within bounds
            float minDistance = piloting ? MIN_SHIP_DISTANCE : MIN_PLAYER_DISTANCE;
            float maxDistance = piloting ? MAX_SHIP_DISTANCE : MAX_PLAYER_DISTANCE;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            desiredDistance = pilotingShip ? DEFAULT_SHIP_DISTANCE : DEFAULT_PLAYER_DISTANCE;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "SolarSystem") return;

            ModHelper.Console.WriteLine($"{nameof(ThirdPersonCamera)} OnSceneLoaded", MessageType.Success);

            // Have to do this here or else the skybox breaks
            _thirdPersonCamera = new GameObject();
            _thirdPersonCamera.SetActive(false);

            _camera = _thirdPersonCamera.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.Color;
            _camera.backgroundColor = Color.black;
            _camera.fieldOfView = 90f;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 40000f;
            _camera.depth = 0f;
            _camera.enabled = false;


            _OWCamera = _thirdPersonCamera.AddComponent<OWCamera>();
            _OWCamera.renderSkybox = true;

            desiredDistance = DEFAULT_PLAYER_DISTANCE;

            holdingTool = false;
        }

        private void SetUpCamera()
        {
            ModHelper.Console.WriteLine($"{nameof(ThirdPersonCamera)} SetUpCamera", MessageType.Success);

            _thirdPersonCamera.transform.position = Locator.GetPlayerTransform().position;

            // Crashes without this idk
            FlashbackScreenGrabImageEffect temp = _thirdPersonCamera.AddComponent<FlashbackScreenGrabImageEffect>();
            temp._downsampleShader = Locator.GetPlayerCamera().gameObject.GetComponent<FlashbackScreenGrabImageEffect>()._downsampleShader;

            PlanetaryFogImageEffect _image = _thirdPersonCamera.AddComponent<PlanetaryFogImageEffect>();
            _image.fogShader = Locator.GetPlayerCamera().gameObject.GetComponent<PlanetaryFogImageEffect>().fogShader;

            PostProcessingBehaviour _postProcessiong = _thirdPersonCamera.AddComponent<PostProcessingBehaviour>();
            _postProcessiong.profile = Locator.GetPlayerCamera().gameObject.GetAddComponent<PostProcessingBehaviour>().profile;

            _thirdPersonCamera.SetActive(true);
            _camera.cullingMask = Locator.GetPlayerCamera().mainCamera.cullingMask & ~(1 << 27) | (1 << 22); // The head and ghost matter have the same culling mask

            _thirdPersonCamera.transform.parent = Locator.GetPlayerCamera().transform;
            _thirdPersonCamera.transform.position = Locator.GetPlayerCamera().transform.position;
            _thirdPersonCamera.transform.rotation = Locator.GetPlayerCamera().transform.rotation;

            _thirdPersonCamera.name = "THIRDPERSONCAMERA";

            // Now loaded but we default to being disabled
            camera_enabled = false;
        }

        private void SetProbeLauncherVisibility(bool visible)
        {
            GameObject probeLauncher = Locator.GetPlayerBody().GetComponentInChildren<ProbeLauncher>().gameObject;
            if (probeLauncher == null)
            {
                ModHelper.Console.WriteLine($"Couldn't find ProbeLauncher", MessageType.Warning);
                return;
            }

            probeLauncher.transform.localScale = visible ? new Vector3(1, 1, 1) : new Vector3(0, 0, 0);
        }

        private void EnableCamera()
        {
            // Check that they should be allowed
            if (holdingTool) return;

            ModHelper.Console.WriteLine($"Third person camera enabled", MessageType.Debug);

            camera_enabled = true;
            if (camera_active) ActivateCamera();
        }

        private void DisableCamera()
        {
            ModHelper.Console.WriteLine($"Third person camera disabled", MessageType.Debug);

            camera_enabled = false;
            DeactivateCamera();
        }

        private void ActivateCamera()
        {
            camera_active = true;

            ModHelper.Console.WriteLine($"Third person camera now active", MessageType.Debug);

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

            SetProbeLauncherVisibility(false);
        }

        private void DeactivateCamera()
        {
            // Only if the player chose this
            if (camera_enabled) camera_active = false;

            ModHelper.Console.WriteLine($"Third person camera deactivated", MessageType.Debug);

            if (Locator.GetActiveCamera() == _OWCamera)
            {
                GlobalMessenger<OWCamera>.FireEvent("SwitchActiveCamera", Locator.GetPlayerCamera());
                Locator.GetPlayerCamera().mainCamera.enabled = true;
                _camera.enabled = false;
            }

            distance = 0f;

            SetProbeLauncherVisibility(true);
        }

        private void Update()
        {
            if (!camera_enabled) return;

            try
            {
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
                    if (Physics.Raycast(origin, direction, out RaycastHit hitInfo, desiredDistance))
                    {
                        distance = Mathf.Clamp(distance, 0f, hitInfo.distance) * 0.9f; // Try to avoid seeing through curved walls
                    }
                    if (pilotingShip) Locator.GetShipBody().EnableCollisionDetection();

                    // Stop the camera going into your head even if it's inside a wall
                    if (distance < minDistance) distance = minDistance;

                    // Finally, move the camera into place
                    _thirdPersonCamera.transform.position = origin + direction * distance;
                }
            }
            catch(Exception e)
            {
                // What happened?
                ModHelper.Console.WriteLine($"Something went wrong in update: {e.Message}. {e.StackTrace}.", MessageType.Error);
                camera_enabled = false;
            }
        }
    }
}
