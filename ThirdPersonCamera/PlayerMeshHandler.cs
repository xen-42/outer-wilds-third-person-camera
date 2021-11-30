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

        private GameObject suitArm;
        private GameObject fleshArm;
        private GameObject helmetMesh;
        private GameObject head;

        public PlayerMeshHandler()
        {
            GlobalMessenger.AddListener("DeactivateThirdPersonCamera", new Callback(OnDeactivateThirdPersonCamera));
            GlobalMessenger.AddListener("ActivateThirdPersonCamera", new Callback(OnActivateThirdPersonCamera));
            GlobalMessenger<PlayerTool>.AddListener("OnEquipTool", new Callback<PlayerTool>(OnToolEquiped));
            GlobalMessenger<PlayerTool>.AddListener("OnUnequipTool", new Callback<PlayerTool>(OnToolUnequiped));
            GlobalMessenger.AddListener("RemoveHelmet", new Callback(OnRemoveHelmet));
            GlobalMessenger.AddListener("PutOnHelmet", new Callback(OnPutOnHelmet));
        }

        public void OnDestroy()
        {
            GlobalMessenger.RemoveListener("DeactivateThirdPersonCamera", new Callback(OnDeactivateThirdPersonCamera));
            GlobalMessenger.RemoveListener("ActivateThirdPersonCamera", new Callback(OnActivateThirdPersonCamera));
            GlobalMessenger<PlayerTool>.RemoveListener("OnEquipTool", new Callback<PlayerTool>(OnToolEquiped));
            GlobalMessenger<PlayerTool>.RemoveListener("OnUnequipTool", new Callback<PlayerTool>(OnToolUnequiped));
            GlobalMessenger.RemoveListener("RemoveHelmet", new Callback(OnRemoveHelmet));
            GlobalMessenger.RemoveListener("PutOnHelmet", new Callback(OnPutOnHelmet));
        }

        public void Init()
        {
            var campfire = GameObject.Find("/Moon_Body/Sector_THM/Interactables_THM/Effects_HEA_Campfire/Props_HEA_Campfire/Campfire_Flames");

            _fireRenderer = GameObject.Instantiate(campfire.GetComponent<MeshRenderer>(), Locator.GetPlayerBody().transform);
            _fireRenderer.transform.localScale = new Vector3(0.8f, 2f, 0.8f);
            _fireRenderer.transform.localPosition = new Vector3(0, -1.2f, -0.25f);
            _fireRenderer.enabled = false;

            _hazardDetector = Locator.GetPlayerController().GetComponentInChildren<HazardDetector>();
            _hazardDetector.OnHazardsUpdated += OnHazardsUpdated;

            _propID_Fade = Shader.PropertyToID("_Fade");

            try
            {
                if (Main.UseCustomDreamerModel)
                {
                    var playerTransform = Locator.GetPlayerBody().transform;
                    var ghostMaterial = GameObject.Find("SIM_GhostBirdBody").GetComponent<MeshRenderer>().material;
                    var ghostShader = Shader.Find("Outer Wilds/Environment/Invisible Planet/Cyberspace");

                    var ghostTorso = Main.SharedInstance.ModHelper.Assets.Get3DObject("assets\\ghost_torso.obj", "assets\\tex.png");
                    var ghostHead = Main.SharedInstance.ModHelper.Assets.Get3DObject("assets\\ghost_head.obj", "assets\\tex.png");
                    var ghostEyes = Main.SharedInstance.ModHelper.Assets.Get3DObject("assets\\ghost_eyes.obj", "assets\\tex.png");
                    var depth = Main.SharedInstance.ModHelper.Assets.GetTexture("assets\\tex.png");

                    ghostTorso.SetActive(true);
                    ghostTorso.transform.parent = playerTransform;
                    ghostTorso.transform.localPosition = 0.3f * Vector3.up + 0.3f * Vector3.back;
                    ghostTorso.transform.localRotation = Quaternion.AngleAxis(5, Vector3.right);
                    ghostTorso.transform.localScale = 0.25f * Vector3.one;
                    ghostTorso.layer = 28;
                    ghostTorso.name = "PlayerGhostTorso";
                    ghostTorso.GetComponent<MeshRenderer>().material = new Material(ghostShader);
                    ghostTorso.GetComponent<MeshRenderer>().material.CopyPropertiesFromMaterial(ghostMaterial);

                    ghostHead.SetActive(true);
                    ghostHead.transform.parent = playerTransform;
                    ghostHead.transform.position = Locator.GetPlayerCamera().transform.position + playerTransform.TransformDirection(Vector3.back) * 0.3f;
                    ghostHead.transform.localRotation = Quaternion.AngleAxis(-90, Vector3.up);
                    ghostHead.transform.localScale = 0.15f * Vector3.one;
                    ghostHead.layer = 28;
                    ghostHead.name = "PlayerGhostHead";
                    ghostHead.GetComponent<MeshRenderer>().material = new Material(ghostShader);
                    ghostHead.GetComponent<MeshRenderer>().material.CopyPropertiesFromMaterial(ghostMaterial);

                    ghostEyes.SetActive(true);
                    ghostEyes.transform.parent = ghostHead.transform;
                    ghostEyes.transform.localPosition = Vector3.zero;
                    ghostEyes.transform.rotation = ghostHead.transform.rotation;
                    ghostEyes.transform.localScale = Vector3.one;
                    ghostEyes.layer = 28;
                    ghostEyes.name = "PlayerGhostEyes";
                    ghostEyes.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
                    ghostEyes.GetComponent<MeshRenderer>().material.EnableKeyword("_EMISSION");
                    ghostEyes.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.white);
                }
            }
            catch(Exception)
            {
                Main.WriteWarning("Couldn't make dreamer model. Do you own the DLC?");
            }
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
            try
            {
                if (suitArm == null) suitArm = Locator.GetPlayerTransform().Find("Traveller_HEA_Player_v2/Traveller_Mesh_v01:Traveller_Geo/Traveller_Mesh_v01:PlayerSuit_RightArm").gameObject;
                if (fleshArm == null) fleshArm = Locator.GetPlayerTransform().Find("Traveller_HEA_Player_v2/player_mesh_noSuit:Traveller_HEA_Player/player_mesh_noSuit:Player_RightArm").gameObject;
            }
            catch(Exception)
            {
                Main.WriteWarning("Couldn't find arm");
            }

            if(suitArm != null) suitArm.layer = visible ? 0 : 22;
            if(fleshArm != null) fleshArm.layer = visible ? 0 : 22;
        }

        private void OnRemoveHelmet()
        {
            SetHeadVisibility(true);
        }

        private void OnPutOnHelmet()
        {
            SetHeadVisibility(true);
        }

        private void SetHeadVisibility(bool visible)
        {
            if (Locator.GetPlayerSuit().IsWearingHelmet())
            {
                if (helmetMesh == null)
                {
                    helmetMesh = Locator.GetPlayerTransform().Find("Traveller_HEA_Player_v2/Traveller_Mesh_v01:Traveller_Geo/Traveller_Mesh_v01:PlayerSuit_Helmet").gameObject;
                    helmetMesh.layer = 0;
                }
            }
            else
            {
                if (head == null)
                {
                    try
                    {
                        head = Locator.GetPlayerTransform().Find("Traveller_HEA_Player_v2/player_mesh_noSuit:Traveller_HEA_Player/player_mesh_noSuit:Player_Head").gameObject;
                        head.layer = 0;
                    }
                    catch(Exception)
                    {
                        Main.WriteWarning("Couldn't find players head.");
                    }
                }
            }
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
