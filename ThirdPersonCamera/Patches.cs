using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ThirdPersonCamera
{
    class Patches
    {
        public static void EquipTool(PlayerTool __instance)
        {
            GlobalMessenger<PlayerTool>.FireEvent("OnEquipTool", __instance);
        }

        public static void UnequipTool(PlayerTool __instance)
        {
            GlobalMessenger<PlayerTool>.FireEvent("OnUnequipTool", __instance);
        }

        public static void CloseEyes()
        {
            GlobalMessenger.FireEvent("DisableThirdPersonCamera");
        }

        public static void OnFinishOpenEyes()
        {
            GlobalMessenger.FireEvent("EnableThirdPersonCamera");
        }

        public static void OnStartLiftPlayer()
        {
            GlobalMessenger.FireEvent("DisableThirdPersonCamera");
        }

        public static void OnExitLanternBounds()
        {
            // Makes it apply the simulation camera to the third person camera idk why
            GlobalMessenger.FireEvent("DisableThirdPersonCamera");
            GlobalMessenger.FireEvent("EnableThirdPersonCamera");
        }

        public static void OnStartGrapple()
        {
            GlobalMessenger.FireEvent("DisableThirdPersonCamera");
        }

        public static void OnFinishGrapple()
        {
            GlobalMessenger.FireEvent("EnableThirdPersonCamera");
        }
    }
}
