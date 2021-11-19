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
        public static void DisableThirdPersonCameraEvent()
        {
            GlobalMessenger.FireEvent("DisableThirdPersonCamera");
        }

        public static void EnableThirdPersonCameraEvent()
        {
            GlobalMessenger.FireEvent("EnableThirdPersonCamera");
        }

        public static void EquipTool(PlayerTool __instance)
        {
            GlobalMessenger<PlayerTool>.FireEvent("OnEquipTool", __instance);
        }

        public static void UnequipTool(PlayerTool __instance)
        {
            GlobalMessenger<PlayerTool>.FireEvent("OnUnequipTool", __instance);
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
            GlobalMessenger.FireEvent("OnRoastingStickActivate");
        }

        public static void SetNomaiText1(NomaiTranslatorProp __instance, NomaiText text, int textID)
        {
            GlobalMessenger<NomaiText, int>.FireEvent("SetNomaiText", text, textID);
        }

        public static void SetNomaiText2(NomaiTranslatorProp __instance, NomaiText text)
        {
            GlobalMessenger<NomaiText, int>.FireEvent("SetNomaiText", text, -1);
        }

        public static void SetNomaiAudio(NomaiTranslatorProp __instance, NomaiAudioVolume audio, int textPage)
        {
            GlobalMessenger<NomaiAudioVolume, int>.FireEvent("SetNomaiAudio", audio, textPage);
        }

        public static void GetTextNode(NomaiText __instance, int id)
        {
            // Only fire event if this wasn't called from ScreenTextHandler
            if(__instance != ScreenTextHandler.GetCurrentText() || id != ScreenTextHandler.GetCurrentTextID())
                GlobalMessenger<NomaiText, int>.FireEvent("GetTextNode", __instance, id);
        }

        public static void CheckSetDatabaseCondition(NomaiText __instance)
        {
            // This is called when something gets translated
            GlobalMessenger<NomaiText>.FireEvent("TextedTranslated", __instance);
        }
    }
}
