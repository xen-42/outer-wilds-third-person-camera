using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ThirdPersonCamera
{
    public class PlayerMeshHandler
    {
        private bool _isToolHeld = false;
        private bool _setArmVisibleNextTick = false;

        public PlayerMeshHandler()
        {
            GlobalMessenger.AddListener("DeactivateThirdPersonCamera", new Callback(OnDeactivateThirdPersonCamera));
            GlobalMessenger.AddListener("ActivateThirdPersonCamera", new Callback(OnActivateThirdPersonCamera));
            GlobalMessenger<PlayerTool>.AddListener("OnEquipTool", new Callback<PlayerTool>(OnToolEquiped));
            GlobalMessenger<PlayerTool>.AddListener("OnUnequipTool", new Callback<PlayerTool>(OnToolUnequiped));
            GlobalMessenger.AddListener("RemoveHelmet", new Callback(OnRemoveHelmet));
        }

        private void OnDeactivateThirdPersonCamera()
        {
            SetArmVisibility(!_isToolHeld);
            SetHeadVisibility(false);
        }

        private void OnActivateThirdPersonCamera()
        {
            SetArmVisibility(true);
            SetHeadVisibility(true);
        }

        private void OnToolEquiped(PlayerTool _)
        {
            _isToolHeld = true;
            if (Main.IsThirdPerson()) _setArmVisibleNextTick = true;
        }

        private void OnToolUnequiped(PlayerTool _)
        {
            _isToolHeld = false;
            SetArmVisibility(true);
        }

        private void SetArmVisibility(bool visible)
        {
            GameObject suitArm = GameObject.Find("Traveller_Mesh_v01:PlayerSuit_RightArm");
            GameObject fleshArm = GameObject.Find("player_mesh_noSuit:Player_RightArm");

            if (suitArm == null && fleshArm == null)
            {
                Main.WriteError("Can't find arm");
            }
            else
            {
                GameObject arm = suitArm ?? fleshArm;

                arm.layer = visible ? 0 : 22;
            }
        }

        private void OnRemoveHelmet()
        {
            SetHeadVisibility(true);
        }

        private void SetHeadVisibility(bool visible)
        {
            if (Locator.GetPlayerSuit().IsWearingHelmet()) return;

            GameObject head = GameObject.Find("player_mesh_noSuit:Player_Head");
            if (head != null)
            {
                if (head.layer != 0) head.layer = 0;
            } 
            else Main.WriteWarning("Couldn't find the player's head");
        }

        public void Update()
        {
            if (_setArmVisibleNextTick)
            {
                SetArmVisibility(true);
                _setArmVisibleNextTick = false;
            }
        }
    }
}
