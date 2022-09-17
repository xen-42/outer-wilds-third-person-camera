using UnityEngine;
using UnityEngine.Events;

namespace ThirdPersonCamera
{
    public interface ICommonCameraAPI
    {
        void RegisterCustomCamera(OWCamera OWCamera);
        (OWCamera, Camera) CreateCustomCamera(string name);
		void ExitCamera(OWCamera OWCamera);
		void EnterCamera(OWCamera OWCamera);
		UnityEvent<PlayerTool> EquipTool();
        UnityEvent<PlayerTool> UnequipTool();
    }
}
