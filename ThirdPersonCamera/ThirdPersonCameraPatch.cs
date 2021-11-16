using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThirdPersonCamera
{
    class ThirdPersonCameraPatch
    {
        public static void EquipTool()
        {
            GlobalMessenger.FireEvent("DisableThirdPersonCamera");
        }

        public static void UnequipTool()
        {
            GlobalMessenger.FireEvent("EnableThirdPersonCamera");
        }

        public static void WakeUp()
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
