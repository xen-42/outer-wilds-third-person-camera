using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ThirdPersonCamera
{
    public class ToolMaterialHandler
    {
        private Main parent;

        private PlayerTool _heldTool;

        // Some tools must be set one tick after equip
        private readonly string[] laterTickTools = { "NomaiTranslatorProp", "ProbeLauncher", "ItemCarryTool", "TutorialProbeLauncher_Base" };
        private bool _thirdPersonMaterialNextTick = false;

        // We make some tools larger in 3rd person view
        private readonly string[] resizingExemptTools = { "NomaiTranslatorProp", "ProbeLauncher", "TutorialCamera_Base", "TutorialProbeLauncher_Base" };

        public ToolMaterialHandler(Main _main)
        {
            parent = _main;

            GlobalMessenger.AddListener("OnRetrieveProbe", new Callback(OnRetrieveProbe));
            GlobalMessenger<PlayerTool>.AddListener("OnEquipTool", new Callback<PlayerTool>(OnToolEquiped));
            GlobalMessenger<PlayerTool>.AddListener("OnUnequipTool", new Callback<PlayerTool>(OnToolUnequiped));
            GlobalMessenger.AddListener("DeactivateThirdPersonCamera", new Callback(OnDeactivateThirdPersonCamera));
            GlobalMessenger.AddListener("ActivateThirdPersonCamera", new Callback(OnActivateThirdPersonCamera));
        }

        public void OnDestroy()
        {
            GlobalMessenger.RemoveListener("OnRetrieveProbe", new Callback(OnRetrieveProbe));
            GlobalMessenger<PlayerTool>.RemoveListener("OnEquipTool", new Callback<PlayerTool>(OnToolEquiped));
            GlobalMessenger<PlayerTool>.RemoveListener("OnUnequipTool", new Callback<PlayerTool>(OnToolUnequiped));
            GlobalMessenger.RemoveListener("DeactivateThirdPersonCamera", new Callback(OnDeactivateThirdPersonCamera));
            GlobalMessenger.RemoveListener("ActivateThirdPersonCamera", new Callback(OnActivateThirdPersonCamera));
        }

        public void OnRetrieveProbe()
        {
            SetToolMaterials(parent.IsThirdPerson());
        }

        public void OnToolEquiped(PlayerTool tool)
        {
            _heldTool = tool;

            parent.WriteInfo(_heldTool.name + " " + _heldTool.tag + " " + _heldTool.GetType());

            if (!parent.IsThirdPerson()) return;

            // Some tool materials must be set next tick
            if (laterTickTools.Contains(_heldTool.name)) _thirdPersonMaterialNextTick = true;
            else SetToolMaterials(true);

            // We make some of the models look larger for the 3rd person view
            if (!resizingExemptTools.Contains(tool.name)) tool.transform.localScale = new Vector3(2, 2, 2);
        }

        public void OnToolUnequiped(PlayerTool tool) 
        {
            // Put them back to normal when unequiping
            //SetToolMaterials(false);
            _heldTool = null;
        }

        public void OnDeactivateThirdPersonCamera()
        {
            SetToolMaterials(false);
            // Double check we're still holding it
            if (_heldTool != null && !_heldTool.IsEquipped()) _heldTool = null;
        }

        public void OnActivateThirdPersonCamera()
        {
            SetToolMaterials(true);
        }

        private void SetToolMaterials(bool thirdPerson)
        {
            if (_heldTool == null) return;
            MeshRenderer[] meshRenderers = _heldTool.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                // Have to keep their relative order
                foreach (Material m in meshRenderer.materials)
                {
                    parent.WriteInfo($"{m.name}, {m.renderQueue}");
                    if (thirdPerson && m.renderQueue >= 2000) m.renderQueue -= 2000;
                    else if (!thirdPerson && m.renderQueue < 2000) m.renderQueue += 2000;
                }
            }
        }

        public void Update()
        {
            if(_heldTool is ItemTool && Locator.GetToolModeSwapper().GetItemCarryTool() == null)
            {
                // We dropped it?
                SetToolMaterials(false);
                _heldTool = null;
            }

            if (_thirdPersonMaterialNextTick)
            {
                SetToolMaterials(true);
                _thirdPersonMaterialNextTick = false;
            }
        }
    }
}
