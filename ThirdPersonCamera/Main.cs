using OWML.Common;
using OWML.ModHelper;
using OWML.Utils;
using System;
using System.Collections.Generic;
using ThirdPersonCamera.Handlers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonCamera
{
    public class Main : ModBehaviour
    {
        public static bool IsLoaded { get; private set; } = false;
        private bool _initNextTick = false;
        public bool IsUsingFreeLook;

        public static Main SharedInstance { get; private set; }
        public static ThirdPersonCamera ThirdPersonCamera { get; private set; }
        public static UIHandler UIHandler { get; private set; }
        public static HUDHandler HUDHandler { get; private set; }

        public static bool KeepFreeLookAngle { get; private set; }
        public static bool UseThirdPersonByDefault { get; private set; }

        public static float DefaultPlayerDistance { get; private set; }
        public static float DefaultPlayerSuitDistance { get; private set; }
        public static float DefaultShipDistance { get; private set; }

        private static Transform _probeLauncher;
        private static Transform _signalScope;
        private static Transform _translator;
        private static Transform _itemCarryTool;
        private static Transform _vesselCoreStow;

        private static readonly Dictionary<Transform, Vector3> _toolInitialPosition = new Dictionary<Transform, Vector3>();

        public static bool IsAtEye;
        public static bool IsFirstLoop;
        public static bool IsWakingAtMuseum;
        private static bool _hasJustDied;

        public static ICommonCameraAPI CommonCameraAPI { get; private set; }

        private void Start()
        {
            SharedInstance = this;

            try
            {
                CommonCameraAPI = ModHelper.Interaction.GetModApi<ICommonCameraAPI>("xen.CommonCameraUtility");
            }
            catch (Exception e)
            {
                WriteError($"CommonCameraAPI was not found. ThirdPersonCamera will not run. {e.Message}, {e.StackTrace}");
                enabled = false;
            }

            WriteSuccess($"ThirdPersonCamera is loaded!");

            // Helpers
            ThirdPersonCamera = new ThirdPersonCamera();
            UIHandler = new UIHandler();
            HUDHandler = new HUDHandler();

            // Patches
            Patches.Apply();

            GlobalMessenger<DeathType>.AddListener("PlayerDeath", new Callback<DeathType>(JustDied));

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            ThirdPersonCamera.OnDestroy();
            UIHandler.OnDestroy();
            HUDHandler.OnDestroy();

            GlobalMessenger<DeathType>.RemoveListener("PlayerDeath", new Callback<DeathType>(JustDied));
        }

        private void JustDied(DeathType _)
        {
            _hasJustDied = true;
        }

        public override void Configure(IModConfig config)
        {
            base.Configure(config);
            KeepFreeLookAngle = config.GetSettingsValue<bool>("Keep free look angle");
            UseThirdPersonByDefault = config.GetSettingsValue<bool>("Use 3rd person by default");

            DefaultPlayerDistance = config.GetSettingsValue<float>("Default camera zoom (no suit)");
            DefaultPlayerSuitDistance = config.GetSettingsValue<float>("Default camera zoom (suit)");
            DefaultShipDistance = config.GetSettingsValue<float>("Default camera zoom (ship)");
            
            if(ThirdPersonCamera != null)
            {
                ThirdPersonCamera.SetDefaultDistanceSettings(PlayerState.AtFlightConsole());
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            IsAtEye = scene.name.Equals("EyeOfTheUniverse");

            if (!scene.name.Equals("SolarSystem") && !scene.name.Equals("EyeOfTheUniverse"))
            {
                IsLoaded = false;
                IsFirstLoop = false;
                return;
            }

            var loopCount = PlayerData.LoadLoopCount();
            WriteInfo($"Start loop {loopCount}");
            IsWakingAtMuseum = (IsFirstLoop && loopCount == 1 && !_hasJustDied);
            _hasJustDied = false;
            IsFirstLoop = (loopCount == 1) && !IsWakingAtMuseum;

            PreInit();
        }

        private void OnSceneUnloaded(Scene scene)
        {
            IsLoaded = false;
        }

        private void PreInit()
        {
            try
            {
                ThirdPersonCamera.PreInit();
                WriteSuccess("ThirdPersonCamera pre-initialization succeeded");
                _initNextTick = true;
                IsUsingFreeLook = false;
            }
            catch(Exception e)
            {
                WriteError($"ThirdPersonCamera pre-initialization failed. {e.Message}. {e.StackTrace}");
            }
        }

        private void Init()
        {
            try
            {
                IsLoaded = true;

                Locator.GetPlayerBody().gameObject.AddComponent<PromptHandler>();

                ThirdPersonCamera.Init();
                if(!IsAtEye) UIHandler.Init();
                HUDHandler.Init();

                if (IsAtEye || IsWakingAtMuseum)
                {
                    ThirdPersonCamera.CameraEnabled = true;
                    if (ThirdPersonCamera.CameraActive) ThirdPersonCamera.ActivateCamera();
                    else ThirdPersonCamera.DeactivateCamera();
                }

                try
                {
                    ModHelper.Interaction.GetMod("xen.DayDream").GetValue<List<OWCamera>>("Cameras").Add(ThirdPersonCamera.OWCamera);
                }
                catch (Exception) { }

                WriteSuccess("ThirdPersonCamera initialization succeeded");
            }
            catch (Exception e)
            {
                WriteError($"ThirdPersonCamera initialization failed. {e.Message}. {e.StackTrace}");
            }
        }

        private void Update()
        {
            if(_initNextTick)
            {
                Init();
                _initNextTick = false;
            }

            if (!IsLoaded) return;

            ThirdPersonCamera.Update();
            HUDHandler.Update();
        }

        public static bool IsThirdPerson()
        {
            // Also going to count if they're using static camera because why not
            return (ThirdPersonCamera.CameraEnabled && ThirdPersonCamera.CameraActive) || Locator.GetActiveCamera().name == "StaticCamera";
        }

        public static void OnFinishOpenEyes()
        {
            if (ThirdPersonCamera.JustStartedLoop)
            {
                Main.WriteInfo("Opening eyes for the first time");
                ThirdPersonCamera.JustStartedLoop = false;
                ThirdPersonCamera.EnableCamera();
                if (UseThirdPersonByDefault)
                {
                    ThirdPersonCamera.ActivateCamera();
                    Locator.GetPlayerCameraController().CenterCameraOverSeconds(1.0f, true);
                }
            }
            else
            {
                ThirdPersonCamera.EnableCamera();
            }
        }

        public static void OnInitPlayerForceAlignment()
        {
            // No longer zero G
            if(Main.SharedInstance.IsUsingFreeLook)
            {
                Locator.GetPlayerController().UnlockMovement();
            }
        }

        public static void OnBreakPlayerForceAlignment()
        {
            // In zero G
            if (Main.SharedInstance.IsUsingFreeLook)
            {
                Locator.GetPlayerController().LockMovement(true);
            }
        }

        public static void OnStartFreeLook()
        {
            if (PlayerState.InZeroG())
            {
                Locator.GetPlayerController().LockMovement(true);
            }

            if (_probeLauncher == null)
            {
                _probeLauncher = Locator.GetPlayerTransform().Find("PlayerCamera/ProbeLauncher");
                _toolInitialPosition[_probeLauncher] = _probeLauncher.localPosition;
            }
            if (_signalScope == null)
            {
                _signalScope = Locator.GetPlayerTransform().Find("PlayerCamera/Signalscope");
                _toolInitialPosition[_signalScope] = _signalScope.localPosition;
            }
            if (_translator == null)
            {
                _translator = Locator.GetPlayerTransform().Find("PlayerCamera/NomaiTranslatorProp");
                _toolInitialPosition[_translator] = _translator.localPosition;
            }
            if (_itemCarryTool == null)
            {
                _itemCarryTool = Locator.GetPlayerTransform().Find("PlayerCamera/ItemCarryTool");
                _toolInitialPosition[_itemCarryTool] = _itemCarryTool.localPosition;
            }
            if (_vesselCoreStow == null)
            {
                _vesselCoreStow = Locator.GetPlayerTransform().Find("PlayerCamera/VesselCoreStowTransform");
                _toolInitialPosition[_vesselCoreStow] = _vesselCoreStow.localPosition;
            }

            foreach (var item in new Transform[] { _probeLauncher, _signalScope, _translator, _itemCarryTool, _vesselCoreStow })
            {
                var pos = item.transform.position;
                item.transform.parent = Locator.GetPlayerTransform();
                item.transform.position = pos;
            }
        }

        public static void OnStopFreeLook()
        {
            if (!IsLoaded) return;
            try
            {
                if (PlayerState.InZeroG())
                {
                    Locator.GetPlayerController().UnlockMovement();
                }

                foreach (var item in new Transform[] { _probeLauncher, _signalScope, _translator, _itemCarryTool, _vesselCoreStow })
                {
                    item.transform.parent = Locator.GetPlayerCamera().transform;
                    item.transform.localPosition = _toolInitialPosition[item];
                }
            }
            catch(Exception e)
            {
                WriteError($"{e.StackTrace}, {e.Message}");
            }
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
