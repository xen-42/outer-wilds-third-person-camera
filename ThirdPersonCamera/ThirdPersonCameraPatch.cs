using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ThirdPersonCamera
{
    class ThirdPersonCameraPatch
    {
        public static void EquipTool(PlayerTool __instance)
        {
            GlobalMessenger<Type>.FireEvent("OnEquipTool", __instance.GetType());
        }

        public static void UnequipTool(PlayerTool __instance)
        {
            GlobalMessenger<Type>.FireEvent("OnUnequipTool", __instance.GetType());
        }

        public static void CloseEyes()
        {
            GlobalMessenger.FireEvent("DisableThirdPersonCamera");
        }

        public static void OpenEyes(PlayerCameraEffectController __instance, float animDuration, AnimationCurve wakeCurve)
        {
            GlobalMessenger.FireEvent("EnableThirdPersonCamera");
        }

        public static void OnFinishOpenEyes()
        {
            GlobalMessenger.FireEvent("EnableThirdPersonCamera");
        }

        public static void LockMovement()
        {
            GlobalMessenger.FireEvent("LockMovement");
        }

        public static void UnlockMovement()
        {
            GlobalMessenger.FireEvent("UnlockMovement");
        }
    }
}
