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
        private MeshRenderer _dreamFireRenderer;
        private MeshRenderer _activeFireRenderer;
        private HazardDetector _hazardDetector;
        private float fade = 0f;
        private bool inFire = false;

        private int _propID_Fade;

        private GameObject suitArm;
        private GameObject fleshArm;
        private GameObject helmetMesh;
        private GameObject head;

        private AssetBundle assetBundle;
        private GameObject ghostPrefab;

        public static bool InGreenFire = false;

        public PlayerMeshHandler()
        {
            GlobalMessenger<PlayerTool>.AddListener("OnEquipTool", new Callback<PlayerTool>(OnToolEquiped));
            GlobalMessenger<PlayerTool>.AddListener("OnUnequipTool", new Callback<PlayerTool>(OnToolUnequiped));
            GlobalMessenger.AddListener("RemoveHelmet", new Callback(OnRemoveHelmet));
            GlobalMessenger.AddListener("PutOnHelmet", new Callback(OnPutOnHelmet));
            GlobalMessenger<OWCamera>.AddListener("SwitchActiveCamera", new Callback<OWCamera>(OnSwitchActiveCamera));
        }

        public void OnDestroy()
        {
            GlobalMessenger<PlayerTool>.RemoveListener("OnEquipTool", new Callback<PlayerTool>(OnToolEquiped));
            GlobalMessenger<PlayerTool>.RemoveListener("OnUnequipTool", new Callback<PlayerTool>(OnToolUnequiped));
            GlobalMessenger.RemoveListener("RemoveHelmet", new Callback(OnRemoveHelmet));
            GlobalMessenger.RemoveListener("PutOnHelmet", new Callback(OnPutOnHelmet));
            GlobalMessenger<OWCamera>.RemoveListener("SwitchActiveCamera", new Callback<OWCamera>(OnSwitchActiveCamera));
        }

        public void Init()
        {
            try
            {
                var campfire = GameObject.Find("/Moon_Body/Sector_THM/Interactables_THM/Effects_HEA_Campfire/Props_HEA_Campfire/Campfire_Flames");

                _fireRenderer = GameObject.Instantiate(campfire.GetComponent<MeshRenderer>(), Locator.GetPlayerBody().transform);
                _fireRenderer.transform.localScale = new Vector3(0.8f, 2f, 0.8f);
                _fireRenderer.transform.localPosition = new Vector3(0, -1.2f, -0.25f);
                _fireRenderer.enabled = false;
            }
            catch (Exception) { }

            try
            {
                var dreamCampfire = GameObject.Find("/RingWorld_Body/Sector_RingInterior/Sector_Zone1/Sector_DreamFireHouse_Zone1/Interactables_DreamFireHouse_Zone1/DreamFireChamber/Prefab_IP_DreamCampfire/Props_IP_DreamFire/DreamFire_Flames");
                _dreamFireRenderer = GameObject.Instantiate(dreamCampfire.GetComponent<MeshRenderer>(), Locator.GetPlayerBody().transform);
                _dreamFireRenderer.transform.localScale = new Vector3(0.8f, 2f, 0.8f);
                _dreamFireRenderer.transform.localPosition = new Vector3(0, -1.6f, -0.25f);
                _dreamFireRenderer.enabled = false;
            }
            catch(Exception) { }

            _hazardDetector = Locator.GetPlayerController().GetComponentInChildren<HazardDetector>();
            _hazardDetector.OnHazardsUpdated += OnHazardsUpdated;

            _propID_Fade = Shader.PropertyToID("_Fade");

            try
            {
                if (Main.UseCustomDreamerModel)
                {
                    if (assetBundle == null) assetBundle = Main.SharedInstance.ModHelper.Assets.LoadBundle("assets/ghost-hatchling");
                    if (ghostPrefab == null)
                    {
                        ghostPrefab = assetBundle.LoadAsset<GameObject>("Assets/Player Ghost/Ghost.prefab");
                        ghostPrefab.SetActive(false);
                    }

                    var ghost = GameObject.Instantiate(ghostPrefab, Locator.GetPlayerTransform());
                    ghost.transform.localScale = 0.25f * Vector3.one;
                    ghost.transform.localPosition = Vector3.zero;
                    ghost.SetActive(true);

                    var backRoot = Locator.GetPlayerTransform().Find("Traveller_HEA_Player_v2/Traveller_Rig_v01:Traveller_Trajectory_Jnt/Traveller_Rig_v01:Traveller_ROOT_Jnt/Traveller_Rig_v01:Traveller_Spine_01_Jnt/Traveller_Rig_v01:Traveller_Spine_02_Jnt");
                    var headRoot = Locator.GetPlayerTransform().Find("Traveller_HEA_Player_v2/Traveller_Rig_v01:Traveller_Trajectory_Jnt/Traveller_Rig_v01:Traveller_ROOT_Jnt/Traveller_Rig_v01:Traveller_Spine_01_Jnt/Traveller_Rig_v01:Traveller_Spine_02_Jnt/Traveller_Rig_v01:Traveller_Spine_Top_Jnt/Traveller_Rig_v01:Traveller_Neck_01_Jnt/Traveller_Rig_v01:Traveller_Neck_Top_Jnt/Traveller_Rig_v01:Traveller_Head_Top_Jnt");
                    var leftHandRoot = Locator.GetPlayerTransform().Find("Traveller_HEA_Player_v2/Traveller_Rig_v01:Traveller_Trajectory_Jnt/Traveller_Rig_v01:Traveller_ROOT_Jnt/Traveller_Rig_v01:Traveller_Spine_01_Jnt/Traveller_Rig_v01:Traveller_Spine_02_Jnt/Traveller_Rig_v01:Traveller_Spine_Top_Jnt/Traveller_Rig_v01:Traveller_LF_Arm_Clavicle_Jnt/Traveller_Rig_v01:Traveller_LF_Arm_Shoulder_Jnt/Traveller_Rig_v01:Traveller_LF_Arm_Elbow_Jnt/Traveller_Rig_v01:Traveller_LF_Arm_Wrist_Jnt");
                    var rightHandRoot = Locator.GetPlayerTransform().Find("Traveller_HEA_Player_v2/Traveller_Rig_v01:Traveller_Trajectory_Jnt/Traveller_Rig_v01:Traveller_ROOT_Jnt/Traveller_Rig_v01:Traveller_Spine_01_Jnt/Traveller_Rig_v01:Traveller_Spine_02_Jnt/Traveller_Rig_v01:Traveller_Spine_Top_Jnt/Traveller_Rig_v01:Traveller_RT_Arm_Clavicle_Jnt/Traveller_Rig_v01:Traveller_RT_Arm_Shoulder_Jnt/Traveller_Rig_v01:Traveller_RT_Arm_Elbow_Jnt/Traveller_Rig_v01:Traveller_RT_Arm_Wrist_Jnt");

                    var head = ghost.transform.Find("Head");
                    var eyes = ghost.transform.Find("Eyes");
                    eyes.transform.parent = head;
                    head.transform.parent = headRoot;
                    head.transform.localRotation = Quaternion.Euler(0, 0, 90);
                    head.transform.localScale = 1.25f * Vector3.one;
                    head.transform.localPosition = new Vector3(2, -1, 0);


                    var torso = ghost.transform.Find("Torso");
                    torso.transform.parent = backRoot;
                    torso.transform.localRotation = Quaternion.Euler(0, 0, 90);
                    torso.transform.localScale = new Vector3(1.5f, 2.5f, 2f);
                    torso.transform.localPosition = new Vector3(-0.5f, 0, 0);

                    var rightHand = ghost.transform.Find("RightHand");
                    rightHand.transform.parent = rightHandRoot;
                    rightHand.transform.localRotation = Quaternion.Euler(0, 0, 90);
                    rightHand.transform.localScale = 1.5f * Vector3.one;
                    rightHand.transform.localPosition = Vector3.zero;

                    var leftHand = ghost.transform.Find("LeftHand");
                    leftHand.transform.parent = leftHandRoot;
                    leftHand.transform.localRotation = Quaternion.Euler(0, 0, 90);
                    leftHand.transform.localScale = 1.5f * Vector3.one;
                    leftHand.transform.localPosition = Vector3.zero;

                    eyes.gameObject.layer = 28;
                    head.gameObject.layer = 28;
                    torso.gameObject.layer = 28;
                    rightHand.gameObject.layer = 28;
                    leftHand.gameObject.layer = 28;

                    GameObject.Destroy(ghost);
                }
            }
            catch(Exception)
            {
                Main.WriteWarning("Couldn't make dreamer model. Do you own the DLC?");
            }
        }

        private void OnHazardsUpdated()
        {
            var _inFire = _hazardDetector.GetLatestHazardType() == HazardVolume.HazardType.FIRE;

            _activeFireRenderer = (InGreenFire && _dreamFireRenderer != null) ? _dreamFireRenderer : _fireRenderer;

            if (_inFire && !_activeFireRenderer.enabled)
            {   
                inFire = true;
                _activeFireRenderer.material.SetFloat(_propID_Fade, fade);
                if(Main.IsThirdPerson()) _activeFireRenderer.enabled = true;
            } 
            else
            {
                inFire = false;
            }
        }

        private void OnSwitchActiveCamera(OWCamera camera)
        {
            if(camera.name == "ThirdPersonCamera" || camera.name == "StaticCamera")
            {
                SetArmVisibility(true);
                SetHeadVisible();
                if(_activeFireRenderer != null) _activeFireRenderer.enabled = inFire || fade > 0f;
            }
            else
            {
                SetArmVisibility(!_isToolHeld);
                if(_activeFireRenderer != null) _activeFireRenderer.enabled = false;
            }
        }

        private void OnToolEquiped(PlayerTool _)
        {
            _isToolHeld = true;
            if (Main.IsThirdPerson() || Locator.GetActiveCamera().name == "StaticCamera") _setArmVisibleNextTick = true;
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
            SetHeadVisible();
        }

        private void OnPutOnHelmet()
        {
            SetHeadVisible();
        }

        private void SetHeadVisible()
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

            if(_activeFireRenderer != null && _activeFireRenderer.enabled)
            {
                fade = Mathf.MoveTowards(fade, inFire ? 1f : 0f, Time.deltaTime / (inFire ? 1f : 0.5f));
                _activeFireRenderer.material.SetAlpha(fade);
                float fireWidth = PlayerState.IsWearingSuit() ? 1.2f : 0.8f;
                if (InGreenFire && _dreamFireRenderer != null) fireWidth = 1.6f;
                _activeFireRenderer.transform.localScale = new Vector3(fireWidth, 2f, fireWidth) * (0.75f + 0.25f * Mathf.Sqrt(fade));

                if (!inFire && fade <= 0f)
                {
                    fade = 0f;
                    _activeFireRenderer.enabled = false;
                }
            }
        }
    }
}
