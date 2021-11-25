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

        private MeshRenderer _fireRenderer;
        private HazardDetector _hazardDetector;
        private float fade = 0f;
        private bool inFire = false;

        private int _propID_Fade;

        public PlayerMeshHandler()
        {
            GlobalMessenger.AddListener("DeactivateThirdPersonCamera", new Callback(OnDeactivateThirdPersonCamera));
            GlobalMessenger.AddListener("ActivateThirdPersonCamera", new Callback(OnActivateThirdPersonCamera));
            GlobalMessenger<PlayerTool>.AddListener("OnEquipTool", new Callback<PlayerTool>(OnToolEquiped));
            GlobalMessenger<PlayerTool>.AddListener("OnUnequipTool", new Callback<PlayerTool>(OnToolUnequiped));
            GlobalMessenger.AddListener("RemoveHelmet", new Callback(OnRemoveHelmet));
        }

        public void OnDestroy()
        {
            GlobalMessenger.RemoveListener("DeactivateThirdPersonCamera", new Callback(OnDeactivateThirdPersonCamera));
            GlobalMessenger.RemoveListener("ActivateThirdPersonCamera", new Callback(OnActivateThirdPersonCamera));
            GlobalMessenger<PlayerTool>.RemoveListener("OnEquipTool", new Callback<PlayerTool>(OnToolEquiped));
            GlobalMessenger<PlayerTool>.RemoveListener("OnUnequipTool", new Callback<PlayerTool>(OnToolUnequiped));
            GlobalMessenger.RemoveListener("RemoveHelmet", new Callback(OnRemoveHelmet));
        }

        public void Init()
        {
            // /Controller_Campfire
            //var g = GameObject.FindGameObjectsWithTag("Fire")[0];
            var campfire = GameObject.Find("/Moon_Body/Sector_THM/Interactables_THM/Effects_HEA_Campfire/Props_HEA_Campfire/Campfire_Flames");
            foreach (Transform t in campfire.transform)
            {
                Main.WriteInfo($"{t.name}");
            }

            _fireRenderer = GameObject.Instantiate(campfire.GetComponent<MeshRenderer>(), Locator.GetPlayerBody().transform);
            _fireRenderer.transform.localScale = new Vector3(0.8f, 2f, 0.8f);
            _fireRenderer.transform.localPosition = new Vector3(0, -1.2f, -0.25f);
            _fireRenderer.enabled = false;

            _hazardDetector = Locator.GetPlayerController().GetComponentInChildren<HazardDetector>();
            _hazardDetector.OnHazardsUpdated += OnHazardsUpdated;

            _propID_Fade = Shader.PropertyToID("_Fade");
        }

        private void OnHazardsUpdated()
        {
            var _inFire = _hazardDetector.InHazardType(HazardVolume.HazardType.FIRE);
            if (_inFire && !_fireRenderer.enabled)
            {   
                inFire = true;
                _fireRenderer.material.SetFloat(_propID_Fade, fade);
                if(Main.IsThirdPerson()) _fireRenderer.enabled = true;
            } 
            else
            {
                inFire = false;
            }
        }

        private void OnDeactivateThirdPersonCamera()
        {
            SetArmVisibility(!_isToolHeld);
            SetHeadVisibility(false);
            _fireRenderer.enabled = false;
        }

        private void OnActivateThirdPersonCamera()
        {
            SetArmVisibility(true);
            SetHeadVisibility(true);
            _fireRenderer.enabled = fade > 0f;
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

            fade = Mathf.MoveTowards(fade, inFire ? 1f : 0f, Time.deltaTime / (inFire ? 1f : 0.5f));
            _fireRenderer.material.SetAlpha(fade);
            float fireWidth = PlayerState.IsWearingSuit() ? 1.2f : 0.8f;
            _fireRenderer.transform.localScale = new Vector3(fireWidth, 2f, fireWidth) * (0.75f + 0.25f * Mathf.Sqrt(fade));

            if (!inFire && fade <= 0f)
            {
                fade = 0f;
                _fireRenderer.enabled = false;
            }
        }
    }
}
