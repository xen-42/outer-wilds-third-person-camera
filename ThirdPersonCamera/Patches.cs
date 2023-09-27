using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using ThirdPersonCamera.Handlers;

namespace ThirdPersonCamera
{
    class Patches
    {
        public static void Apply()
        {
            var ModHelper = Main.SharedInstance.ModHelper;

            ModHelper.HarmonyHelper.AddPostfix<StreamingGroup>("OnFinishOpenEyes", typeof(Patches), nameof(Patches.OnFinishOpenEyes));
            ModHelper.HarmonyHelper.AddPostfix<PlayerCameraEffectController>("CloseEyes", typeof(Patches), nameof(Patches.DisableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<GhostGrabController>("OnStartLiftPlayer", typeof(Patches), nameof(Patches.DisableThirdPersonCameraEvent));
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
            ModHelper.HarmonyHelper.AddPrefix<ReferenceFrameTracker>("FindReferenceFrameInLineOfSight", typeof(Patches), nameof(Patches.PreFindReferenceFrameInLineOfSight));
            ModHelper.HarmonyHelper.AddPrefix<ReferenceFrameTracker>("FindReferenceFrameInLineOfSight", typeof(Patches), nameof(Patches.PostFindReferenceFrameInLineOfSight));
            ModHelper.HarmonyHelper.AddPrefix<PlayerCameraController>("UpdateRotation", typeof(Patches), nameof(Patches.UpdateRotation));
            ModHelper.HarmonyHelper.AddPrefix<PlayerCameraController>("Update", typeof(Patches), nameof(Patches.PlayerCameraControllerUpdate));
            ModHelper.HarmonyHelper.AddPrefix<PlayerCameraController>("UpdateInput", typeof(Patches), nameof(Patches.UpdateInput));
            ModHelper.HarmonyHelper.AddPrefix<PlayerCharacterController>("UpdateTurning", typeof(Patches), nameof(Patches.UpdateTurning));
            ModHelper.HarmonyHelper.AddPostfix<ProbeLauncherUI>("OnTakeSnapshot", typeof(Patches), nameof(Patches.OnTakeSnapshot));
            ModHelper.HarmonyHelper.AddPrefix<QuantumObject>("IsLockedByProbeSnapshot", typeof(Patches), nameof(Patches.IsLockedByProbeSnapshot));
            ModHelper.HarmonyHelper.AddPostfix<SignalscopeUI>("UpdateLabels", typeof(Patches), nameof(Patches.UpdateLabels));
            ModHelper.HarmonyHelper.AddPostfix<SignalscopeUI>("UpdateWaveform", typeof(Patches), nameof(Patches.UpdateWaveform));
            ModHelper.HarmonyHelper.AddPostfix<SignalscopeReticleController>("UpdateBrackets", typeof(Patches), nameof(Patches.UpdateBrackets));
            ModHelper.HarmonyHelper.AddPrefix<HUDCamera>("OnSwitchActiveCamera", typeof(Patches), nameof(Patches.HUDCameraOnSwitchActiveCamera));
            ModHelper.HarmonyHelper.AddPrefix<NomaiRemoteCamera>("LateUpdate", typeof(Patches), nameof(Patches.NomaiRemoteCameraLateUpdate));
            ModHelper.HarmonyHelper.AddPrefix<NomaiRemoteCameraPlatform>("Awake", typeof(Patches), nameof(Patches.NomaiRemoteCameraPlatformAwake));
            ModHelper.HarmonyHelper.AddPrefix<PlayerState>("OnInitPlayerForceAlignment", typeof(Patches), nameof(Patches.OnInitPlayerForceAlignment));
            ModHelper.HarmonyHelper.AddPrefix<PlayerState>("OnBreakPlayerForceAlignment", typeof(Patches), nameof(Patches.OnBreakPlayerForceAlignment));
        }

        public static void DisableThirdPersonCameraEvent()
        {
            GlobalMessenger.FireEvent("DisableThirdPersonCamera");
        }

        public static void EnableThirdPersonCameraEvent()
        {
            GlobalMessenger.FireEvent("EnableThirdPersonCamera");
        }

        public static void OnFinishOpenEyes()
        {
            Main.OnFinishOpenEyes();
        }

        public static void OnRetrieveProbe()
        {
            GlobalMessenger.FireEvent("OnRetrieveProbe");
        }

        public static void OnDetach(ShipDetachableModule __instance)
        {
            GlobalMessenger<ShipDetachableModule>.FireEvent("ShipModuleDetached", __instance);
        }

        public static void OnRoastingStickActivate()
        {
            Main.ThirdPersonCamera.OnRoastingStickActivate();
        }

        public static bool OnSwitchActiveCamera(OWCamera activeCamera)
        {
            // If its the third person camera ignore it
            return (activeCamera != ThirdPersonCamera.OWCamera);
        }

        public static void NomaiTranslaterPropUpdate(NomaiTranslatorProp __instance, Text ____textField)
        {
			if (UIHandler.TranslatorText != null)
			{
				UIHandler.TranslatorText.text = ____textField.text;
			}
        }

        public static void ShipNotificationDisplayUpdate(ShipNotificationDisplay __instance, Canvas ____displayCanvas)
        {
            if (__instance.name != "ConsoleDisplay") return;

            string text = "";

            foreach(Text t in ____displayCanvas.GetComponentsInChildren<Text>())
            {
				// What is TestText what why
                if (t.name == "TestText") continue;
                text += t.text.Replace("\n", " ") + "\n";
            }

			UIHandler.ShipText.text = text;
        }

        public static bool GetPossibleReferenceFrame(ReferenceFrameTracker __instance, ReferenceFrame ____possibleReferenceFrame)
        {
            // Stops you from targeting your ship while piloting it
            if (____possibleReferenceFrame.GetOWRigidBody().name == "Player_Body") return false;
            if (PlayerState.AtFlightConsole() && ____possibleReferenceFrame.GetOWRigidBody().name == "Ship_Body") return false;
            return true;
        }

        public static void PreFindReferenceFrameInLineOfSight()
        {
            // Don't let the raycast get the player or (sometimes) the ship
            Locator.GetPlayerBody().DisableCollisionDetection();
            if (PlayerState.AtFlightConsole()) Locator.GetShipBody().DisableCollisionDetection();
        }

        public static void PostFindReferenceFrameInLineOfSight()
        {
            Locator.GetPlayerBody().EnableCollisionDetection();
            if(Locator.GetShipBody() != null)
                Locator.GetShipBody().EnableCollisionDetection();
        }

        public static bool UpdateRotation(PlayerCameraController __instance, ref float ____degreesX, ref float ____degreesY, OWCamera ____playerCamera, bool ____isSnapping,
                ShipCockpitController ____shipController, Quaternion ____rotationX, Quaternion ____rotationY)
        {
            if (!Main.IsThirdPerson() || (!OWInput.IsPressed(InputLibrary.freeLook, 0f) && !Main.KeepFreeLookAngle))
            {
                Main.SharedInstance.IsUsingFreeLook = false;
                return true;
            }

            ____degreesX %= 360f;
            ____degreesY %= 360f;
            if (!____isSnapping)
            {
                bool flag = (____shipController == null || (____shipController.AllowFreeLook() | !____shipController.IsPlayerAtFlightConsole())) && OWInput.IsPressed(InputLibrary.freeLook, 0f);
                bool flag2 = Main.KeepFreeLookAngle;
                if (flag || flag2)
                {
                    if (!Main.SharedInstance.IsUsingFreeLook)
                    {
                        Main.SharedInstance.IsUsingFreeLook = true;
                        Main.OnStartFreeLook();
                    }
                    ____degreesY = Mathf.Clamp(____degreesY, -80f, 80f);
                }
                else
                {
                    if (Main.SharedInstance.IsUsingFreeLook)
                    {
                        Main.SharedInstance.IsUsingFreeLook = false;
                        Main.OnStopFreeLook();
                    }
                    return true;
                }
            }
            ____rotationX = Quaternion.AngleAxis(____degreesX, Vector3.up);
            ____rotationY = Quaternion.AngleAxis(____degreesY, -Vector3.right);
            Quaternion localRotation = ____rotationX * ____rotationY * Quaternion.identity;
            ____playerCamera.transform.localRotation = localRotation;

            return false;
        }

        public static bool UpdateInput(PlayerCameraController __instance, float deltaTime, ShipCockpitController ____shipController, bool ____zoomed, OWCamera ____playerCamera,
                float ____initFOV, ref float ____degreesX, ref float ____degreesY)
        {
            if ((____shipController != null && ____shipController.IsPlayerAtFlightConsole()) || !Main.IsThirdPerson() || !OWInput.IsPressed(InputLibrary.freeLook, 0f)) return true;

            bool flag3 = Locator.GetAlarmSequenceController() != null && Locator.GetAlarmSequenceController().IsAlarmWakingPlayer();
            Vector2 vector = Vector2.one;
            vector *= ((____zoomed || flag3) ? PlayerCameraController.ZOOM_SCALAR : 1f);
            vector *= ____playerCamera.fieldOfView / ____initFOV;

            if (Time.timeScale > 1f)
            {
                vector /= Time.timeScale;
            }

            Vector2 axisValue = OWInput.GetAxisValue(InputLibrary.look, InputMode.All);
            ____degreesX += axisValue.x * 180f * vector.x * deltaTime;
            ____degreesY += axisValue.y * 180f * vector.y * deltaTime;

            return false;
        }

        public static bool PlayerCameraControllerUpdate(PlayerCameraController __instance, ref float ____degreesX)
        {
            // When ending freelook makes you take the shortest path back, also enables it for when you did it out of the ship
            if (OWInput.IsNewlyReleased(InputLibrary.freeLook, InputMode.All))
            {
                Main.OnStopFreeLook();
                if (____degreesX > 180f) ____degreesX -= 360f;
                if (____degreesX < -180f) ____degreesX += 360f;
            }

            if (!Main.IsThirdPerson() || (PlayerState.AtFlightConsole() && !Main.KeepFreeLookAngle)) return true;

            if (OWInput.IsNewlyReleased(InputLibrary.freeLook, InputMode.All) && !Main.KeepFreeLookAngle)
            {
                __instance.CenterCameraOverSeconds(0.33f, true); 
            }
            if (OWTime.IsPaused(OWTime.PauseType.Reading))
            {
                // To call a private method
                MethodInfo methodInfo = __instance.GetType().GetMethod("UpdateCamera", BindingFlags.NonPublic | BindingFlags.Instance);
                methodInfo.Invoke(__instance, new object[] { Time.unscaledDeltaTime });
            }

            return false;
        }

        public static bool UpdateTurning()
        {
            // Return false if turning is locked (we're using freelook)
            return !(Main.IsThirdPerson() && OWInput.IsPressed(InputLibrary.freeLook));
        }

        public static void OnTakeSnapshot(ProbeLauncherUI __instance, ProbeCamera camera, RenderTexture snapshot, Texture2D ____rearSnapshotOverlay, Texture2D ____frontSnapshotOverlay)
        {
            if (camera.GetID() == ProbeCamera.ID.Reverse)
            {
                UIHandler.SetProbeLauncherTexture(____rearSnapshotOverlay);
            }
            else
            {
                UIHandler.SetProbeLauncherTexture(____frontSnapshotOverlay);
            }
            UIHandler.SetProbeLauncherTexture(snapshot);
        }

        public static bool IsLockedByProbeSnapshot(QuantumObject __instance, bool ____visibleInProbeSnapshot, ref bool __result)
        {
            if (Main.IsThirdPerson())
            {
                __result = ____visibleInProbeSnapshot && Locator.GetToolModeSwapper().GetToolMode() == ToolMode.Probe;
                return false;
            }

            return true;
        }

        public static void UpdateLabels(SignalscopeUI __instance, Text ____distanceLabel, Text ____signalscopeLabel)
        {
            UIHandler.SetSignalScopeLabel(____signalscopeLabel.text, ____distanceLabel.text);
        }

        public static void UpdateWaveform(SignalscopeUI __instance, Vector3[] ____linePoints)
        {
            UIHandler.SetSignalScopeWaveform(____linePoints);
        }

        public static void UpdateBrackets(SignalscopeReticleController __instance, Transform ____reticuleBracketsTransform, List<SkinnedMeshRenderer> ____clonedLeftBrackets,
            List<SkinnedMeshRenderer> ____clonedRightBrackets)
        {
            Transform parent = PlayerState.AtFlightConsole() && Main.IsThirdPerson() ? UIHandler.SigScopeReticuleParent.transform : ____reticuleBracketsTransform;
            for (int i = 0; i < ____clonedLeftBrackets.Count; i++)
            {
                ____clonedLeftBrackets[i].transform.parent = parent;
                ____clonedRightBrackets[i].transform.parent = parent;
            }
        }

        public static bool HUDCameraOnSwitchActiveCamera(HUDCamera __instance, OWCamera __0) 
        {
            if(__0.name.Equals("ThirdPersonCamera"))
            {
                MethodInfo methodInfo = typeof(HUDCamera).GetMethod("ResumeHUDRendering", BindingFlags.NonPublic | BindingFlags.Instance);
                methodInfo.Invoke(__instance, new object[] { });
                return false;
            }
            return true;
        }

        public static bool NomaiRemoteCameraLateUpdate(NomaiRemoteCamera __instance, NomaiRemoteCameraPlatform ____owningPlatform, OWCamera ____camera,
            NomaiRemoteCameraPlatform ____controllingPlatform, OWCamera ____controllingCamera)
        {
            if (!Main.IsThirdPerson()) return true;

            if (____owningPlatform && ____controllingPlatform)
            {
                var thirdPersonCamera = ThirdPersonCamera.GetCamera();
                __instance.transform.position = NomaiRemoteCameraPlatform.TransformPoint(thirdPersonCamera.transform.position, ____controllingPlatform, ____owningPlatform);
                __instance.transform.rotation = NomaiRemoteCameraPlatform.TransformRotation(thirdPersonCamera.transform.rotation, ____controllingPlatform, ____owningPlatform);
                ____camera.fieldOfView = thirdPersonCamera.fieldOfView;
            }

            return false;
        }

        public static bool NomaiRemoteCameraPlatformAwake(NomaiRemoteCameraPlatform __instance, GameObject ____hologramGroup, Transform ____playerHologram)
        {
            var head = ____playerHologram.GetChild(0).Find("player_mesh_noSuit:Traveller_HEA_Player").Find("player_mesh_noSuit:Player_Head");
            var helmet = ____playerHologram.GetChild(0).Find("Traveller_Mesh_v01:Traveller_Geo").Find("Traveller_Mesh_v01:PlayerSuit_Helmet");

            if (head != null) head.gameObject.layer = 27;
            else Main.WriteWarning("Couldn't find hologram head");
            if (helmet != null) helmet.gameObject.layer = 27;
            else Main.WriteWarning("Couldn't find hologram helmet");
            return true;
        }

        public static void OnInitPlayerForceAlignment()
        {
            Main.OnInitPlayerForceAlignment();
        }

        public static void OnBreakPlayerForceAlignment()
        {
            Main.OnBreakPlayerForceAlignment();
        }
    }
}
