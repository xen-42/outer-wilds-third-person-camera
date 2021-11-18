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

        public ThirdPersonCamera ThirdPersonCamera { get; private set; }
        public DreamWorldManager DreamWorldManager { get; private set; }

        private void Start()
        {
            WriteLine($"{nameof(ThirdPersonCamera)} is loaded!", MessageType.Success);

            ThirdPersonCamera = new ThirdPersonCamera(this);
            DreamWorldManager = new DreamWorldManager(this);

            // Patches
            ModHelper.HarmonyHelper.AddPrefix<StreamingGroup>("OnFinishOpenEyes", typeof(Patches), nameof(Patches.OnFinishOpenEyes));
            ModHelper.HarmonyHelper.AddPrefix<PlayerCameraEffectController>("CloseEyes", typeof(Patches), nameof(Patches.CloseEyes));
            ModHelper.HarmonyHelper.AddPostfix<PlayerTool>("EquipTool", typeof(Patches), nameof(Patches.EquipTool));
            ModHelper.HarmonyHelper.AddPostfix<PlayerTool>("UnequipTool", typeof(Patches), nameof(Patches.UnequipTool));
            ModHelper.HarmonyHelper.AddPostfix<GhostGrabController>("OnStartLiftPlayer", typeof(Patches), nameof(Patches.OnStartLiftPlayer));
            ModHelper.HarmonyHelper.AddPostfix<DreamWorldController>("ExitLanternBounds", typeof(Patches), nameof(Patches.OnExitLanternBounds));

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
            DreamWorldManager.OnDestroy();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "SolarSystem")
            {
                loaded = false;
                ThirdPersonCamera.camera_active = false;
            }

            // I'm paranoid
            try
            {
                ThirdPersonCamera.PreInit();
            }
            catch (Exception)
            {
                WriteLine("PreInit failed", MessageType.Error);
            }
        }

        private void OnEvent(MonoBehaviour behaviour, Events ev)
        {
            if (behaviour.GetType() == typeof(Flashlight) && ev == Events.AfterStart)
            {
                try
                {
                    ThirdPersonCamera.Init();
                    DreamWorldManager.Init();
                    loaded = true;

                    // This actually doesn't seem to affect the player camera
                    GameObject helmetMesh = GameObject.Find("Traveller_Mesh_v01:PlayerSuit_Helmet");
                    helmetMesh.layer = 0;

                    GameObject probeLauncher = Locator.GetPlayerBody().GetComponentInChildren<ProbeLauncher>().gameObject;
                    if (probeLauncher == null) WriteLine($"Couldn't find ProbeLauncher", MessageType.Warning);
                    else if (probeLauncher.layer != 0) probeLauncher.layer = 0;

                }
                catch (Exception)
                {
                    WriteLine("Init failed", MessageType.Error);
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

        public void WriteLine(string msg, MessageType msgType)
        {
            ModHelper.Console.WriteLine(msg, msgType);
        }

        public void WriteError(string msg)
        {
            ModHelper.Console.WriteLine(msg, MessageType.Error);
        }

        public void WriteInfo(string msg)
        {
            ModHelper.Console.WriteLine(msg, MessageType.Info);
        }
    }
}
