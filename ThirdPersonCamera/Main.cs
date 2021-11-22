﻿using OWML.ModHelper;
using OWML.Common;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ThirdPersonCamera
{
    public class Main : ModBehaviour
    {
        private bool loaded = false;
        private bool afterMemoryUplink = false;

        private static Main SharedInstance;
        public static ThirdPersonCamera ThirdPersonCamera { get; private set; }
        public static ScreenTextHandler ScreenTextHandler { get; private set; }
        public static PlayerMeshHandler PlayerMeshHandler { get; private set; }
        public static ToolMaterialHandler ToolMaterialHandler { get; private set; }
        public static HUDHandler HUDHandler { get; private set; }

        private void Start()
        {
            SharedInstance = this;

            WriteSuccess($"ThirdPersonCamera is loaded!");

            // Helpers
            ThirdPersonCamera = new ThirdPersonCamera();
            ScreenTextHandler = new ScreenTextHandler();
            PlayerMeshHandler = new PlayerMeshHandler();
            ToolMaterialHandler = new ToolMaterialHandler();
            HUDHandler = new HUDHandler();

            // Patches
            ModHelper.HarmonyHelper.AddPostfix<StreamingGroup>("OnFinishOpenEyes", typeof(Patches), nameof(Patches.OnFinishOpenEyes));
            ModHelper.HarmonyHelper.AddPostfix<PlayerCameraEffectController>("CloseEyes", typeof(Patches), nameof(Patches.DisableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<PlayerTool>("EquipTool", typeof(Patches), nameof(Patches.EquipTool));
            ModHelper.HarmonyHelper.AddPostfix<PlayerTool>("UnequipTool", typeof(Patches), nameof(Patches.UnequipTool));
            ModHelper.HarmonyHelper.AddPostfix<GhostGrabController>("OnStartLiftPlayer", typeof(Patches), nameof(Patches.DisableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<DreamWorldController>("ExitLanternBounds", typeof(Patches), nameof(Patches.DisableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<DreamWorldController>("EnterLanternBounds", typeof(Patches), nameof(Patches.EnableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<ProbeLauncher>("RetrieveProbe", typeof(Patches), nameof(Patches.OnRetrieveProbe));
            ModHelper.HarmonyHelper.AddPrefix<MindProjectorTrigger>("OnProjectionStart", typeof(Patches), nameof(Patches.DisableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<MindProjectorTrigger>("OnProjectionComplete", typeof(Patches), nameof(Patches.EnableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<MindProjectorTrigger>("OnTriggerVolumeExit", typeof(Patches), nameof(Patches.EnableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<ShipDetachableModule>("Detach", typeof(Patches), nameof(Patches.OnDetach));
            ModHelper.HarmonyHelper.AddPostfix<LanternZoomPoint>("StartZoomIn", typeof(Patches), nameof(Patches.DisableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<LanternZoomPoint>("FinishRetroZoom", typeof(Patches), nameof(Patches.EnableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<RoastingStickController>("OnEnterRoastingMode", typeof(Patches), nameof(Patches.OnRoastingStickActivate));

            ModHelper.HarmonyHelper.AddPrefix<QuantumObject>("OnSwitchActiveCamera", typeof(Patches), nameof(Patches.OnSwitchActiveCamera));

            ModHelper.HarmonyHelper.AddPostfix<TimelineObliterationController>("OnCrackEffectComplete", typeof(Patches), nameof(Patches.DisableThirdPersonCameraEvent));

            ModHelper.HarmonyHelper.AddPostfix<NomaiTranslatorProp>("Update", typeof(Patches), nameof(Patches.NomaiTranslaterPropUpdate));
            ModHelper.HarmonyHelper.AddPostfix<ShipNotificationDisplay>("Update", typeof(Patches), nameof(Patches.ShipNotificationDisplayUpdate));
            ModHelper.HarmonyHelper.AddPrefix<ReferenceFrameTracker>("GetPossibleReferenceFrame", typeof(Patches), nameof(Patches.GetPossibleReferenceFrame));
            ModHelper.HarmonyHelper.AddPrefix<ReferenceFrameTracker>("FindReferenceFrameInLineOfSight", typeof(Patches), nameof(Patches.PreFindReferenceFrameInLineOfSight));
            ModHelper.HarmonyHelper.AddPrefix<ReferenceFrameTracker>("FindReferenceFrameInLineOfSight", typeof(Patches), nameof(Patches.PostFindReferenceFrameInLineOfSight));
            ModHelper.HarmonyHelper.AddPrefix<PlayerCameraController>("UpdateRotation", typeof(Patches), nameof(Patches.UpdateRotation));
            ModHelper.HarmonyHelper.AddPrefix<PlayerCameraController>("Update", typeof(Patches), nameof(Patches.PlayerCameraControllerUpdate));
            ModHelper.HarmonyHelper.AddPrefix<PlayerCameraController>("UpdateInput", typeof(Patches), nameof(Patches.UpdateInput));
            ModHelper.HarmonyHelper.AddPrefix<PlayerCharacterController>("UpdateTurning", typeof(Patches), nameof(Patches.UpdateTurning));

            // Events
            ModHelper.Events.Subscribe<Flashlight>(Events.AfterStart);

            SceneManager.sceneLoaded += OnSceneLoaded;
            ModHelper.Events.Event += OnEvent;
        }

        public void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            ModHelper.Events.Event -= OnEvent;

            ThirdPersonCamera.OnDestroy();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "SolarSystem")
            {
                loaded = false;
                afterMemoryUplink = false;
            } 
            else if (loaded)
            {
                // Already loaded but we're being put into the SolarSystem scene again
                // We must have done the memory uplink (universe is reset after)
                afterMemoryUplink = true;
            }

            ThirdPersonCamera.PreInit();
        }

        private void OnEvent(MonoBehaviour behaviour, Events ev)
        {
            if (behaviour.GetType() == typeof(Flashlight) && ev == Events.AfterStart)
            {
                try
                {
                    ThirdPersonCamera.Init();
                    ScreenTextHandler.Init();
                    loaded = true;

                    if (afterMemoryUplink) ThirdPersonCamera.CameraEnabled = true;

                    // This actually doesn't seem to affect the player camera
                    GameObject helmetMesh = GameObject.Find("Traveller_Mesh_v01:PlayerSuit_Helmet");
                    helmetMesh.layer = 0;

                    GameObject probeLauncher = Locator.GetPlayerBody().GetComponentInChildren<ProbeLauncher>().gameObject;
                    probeLauncher.layer = 0;
                }
                catch (Exception)
                {
                    WriteError("Init failed");
                }
            }
        }

        private void Update()
        {
            if (!loaded) return;

            ThirdPersonCamera.Update();
            PlayerMeshHandler.Update();
            ToolMaterialHandler.Update();
            HUDHandler.Update();
        }

        public static bool IsThirdPerson()
        {
            return ThirdPersonCamera.CameraEnabled && ThirdPersonCamera.CameraActive;
        }

        public static void WriteError(string msg)
        {
            SharedInstance.ModHelper.Console.WriteLine(msg, MessageType.Error);
        }

        public static void WriteWarning(string msg)
        {
            SharedInstance.ModHelper.Console.WriteLine(msg, MessageType.Warning);
        }

        public static void WriteInfo(string msg)
        {
            SharedInstance.ModHelper.Console.WriteLine(msg, MessageType.Info);
        }

        public static void WriteSuccess(string msg)
        {
            SharedInstance.ModHelper.Console.WriteLine(msg, MessageType.Success);
        }
    }
}
