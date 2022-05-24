using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    public interface ICommonCameraAPI
    {
        void RegisterCustomCamera(OWCamera OWCamera);
        (OWCamera, Camera) CreateCustomCamera(string name);
    }
}
