using OWML.ModHelper;
using OWML.Common;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

namespace ThirdPersonCamera
{
    public class Main : ModBehaviour
    {
        private bool loaded = false;
        private bool afterMemoryUplink = false;

        public ThirdPersonCamera ThirdPersonCamera { get; private set; }

        private void Start()
        {
            WriteSuccess($"{nameof(ThirdPersonCamera)} is loaded!");

            ThirdPersonCamera = new ThirdPersonCamera(this);

            // Patches
            ModHelper.HarmonyHelper.AddPostfix<StreamingGroup>("OnFinishOpenEyes", typeof(Patches), nameof(Patches.OnFinishOpenEyes));
            ModHelper.HarmonyHelper.AddPostfix<PlayerCameraEffectController>("CloseEyes", typeof(Patches), nameof(Patches.CloseEyes));
            ModHelper.HarmonyHelper.AddPostfix<PlayerTool>("EquipTool", typeof(Patches), nameof(Patches.EquipTool));
            ModHelper.HarmonyHelper.AddPostfix<PlayerTool>("UnequipTool", typeof(Patches), nameof(Patches.UnequipTool));
            ModHelper.HarmonyHelper.AddPostfix<GhostGrabController>("OnStartLiftPlayer", typeof(Patches), nameof(Patches.OnStartLiftPlayer));
            ModHelper.HarmonyHelper.AddPostfix<DreamWorldController>("ExitLanternBounds", typeof(Patches), nameof(Patches.OnExitLanternBounds));
            ModHelper.HarmonyHelper.AddPostfix<DreamWorldController>("EnterLanternBounds", typeof(Patches), nameof(Patches.OnEnterLanternBounds));
            ModHelper.HarmonyHelper.AddPostfix<ProbeLauncher>("RetrieveProbe", typeof(Patches), nameof(Patches.OnRetrieveProbe));
            
            ModHelper.HarmonyHelper.AddPrefix<MindProjectorTrigger>("OnProjectionStart", typeof(Patches), nameof(Patches.OnProjectionStart));
            ModHelper.HarmonyHelper.AddPostfix<MindProjectorTrigger>("OnProjectionComplete", typeof(Patches), nameof(Patches.OnProjectionComplete));
            ModHelper.HarmonyHelper.AddPostfix<MindProjectorTrigger>("OnTriggerVolumeExit", typeof(Patches), nameof(Patches.OnTriggerVolumeExit));

            ModHelper.HarmonyHelper.AddPostfix<ShipDetachableModule>("Detach", typeof(Patches), nameof(Patches.OnDetach));

            ModHelper.HarmonyHelper.AddPostfix<LanternZoomPoint>("StartZoomIn", typeof(Patches), nameof(Patches.OnStartGrapple));
            ModHelper.HarmonyHelper.AddPostfix<LanternZoomPoint>("FinishRetroZoom", typeof(Patches), nameof(Patches.OnFinishGrapple));

            // Attach onto two events
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
            } else if (loaded)
            {
                // Already loaded but we're being put into the SolarSystem scene again
                // We must have done the memory uplink (universe is reset after)
                afterMemoryUplink = true;
            }

            // I'm paranoid
            try
            {
                ThirdPersonCamera.PreInit();
            }
            catch (Exception)
            {
                WriteError("PreInit failed");
            }
        }

        private void OnEvent(MonoBehaviour behaviour, Events ev)
        {
            if (behaviour.GetType() == typeof(Flashlight) && ev == Events.AfterStart)
            {
                try
                {
                    ThirdPersonCamera.Init();
                    loaded = true;

                    if (afterMemoryUplink) ThirdPersonCamera.CameraEnabled = true;

                    // This actually doesn't seem to affect the player camera
                    GameObject helmetMesh = GameObject.Find("Traveller_Mesh_v01:PlayerSuit_Helmet");
                    helmetMesh.layer = 0;

                    GameObject probeLauncher = Locator.GetPlayerBody().GetComponentInChildren<ProbeLauncher>().gameObject;
                    if (probeLauncher == null) WriteWarning($"Couldn't find ProbeLauncher");
                    else if (probeLauncher.layer != 0) probeLauncher.layer = 0;

                }
                catch (Exception)
                {
                    WriteError("Init failed");
                }
            }
        }

        public static GameObject[] FindGameObjectsWithLayer(int layer)
        {
            List<GameObject> objects = new List<GameObject>();
            foreach(GameObject go in FindObjectsOfType<GameObject>())
            {
                if (go.layer == layer) objects.Add(go);
            }
            if (objects.Count == 0) return null;
            return objects.ToArray();
        }

        private void Update()
        {
            if (!loaded) return;

            ThirdPersonCamera.Update();
        }

        public void WriteError(string msg)
        {
            ModHelper.Console.WriteLine(msg, MessageType.Error);
        }

        public void WriteWarning(string msg)
        {
            ModHelper.Console.WriteLine(msg, MessageType.Warning);
        }

        public void WriteInfo(string msg)
        {
            ModHelper.Console.WriteLine(msg, MessageType.Info);
        }

        public void WriteSuccess(string msg)
        {
            ModHelper.Console.WriteLine(msg, MessageType.Success);
        }
    }
}
