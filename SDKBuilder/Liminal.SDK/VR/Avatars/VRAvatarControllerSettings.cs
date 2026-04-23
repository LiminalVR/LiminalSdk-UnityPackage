using System.Collections.Generic;
using Liminal.SDK.VR.Pointers;
using UnityEngine;

namespace Liminal.SDK.VR.Avatars
{
    [RequireComponent(typeof(VRAvatarController))]
    [DisallowMultipleComponent]
    public class VRAvatarControllerSettings : MonoBehaviour
    {
        public EUpdateRate UpdateRate;

        public ControllerVisualSettings ControllerVisualSettings;
        public PointerVisualSettings PointerVisualSettings;

        private bool _applied;

        private MeshRenderer[] _renderers;

        private void Update()
        {
            _renderers = _renderers ?? GetComponentsInChildren<MeshRenderer>();
            if (_renderers == null)
                return;

            switch (UpdateRate)
            {
                case EUpdateRate.OnStart:
                    if (!_applied)
                    {
                        UpdateControllerSettings(ControllerVisualSettings);
                        UpdatePointerSettings(PointerVisualSettings);
                        _applied = true;
                    }
                    break;

                case EUpdateRate.OnUpdate:
                    UpdateControllerSettings(ControllerVisualSettings);
                    UpdatePointerSettings(PointerVisualSettings);
                    break;
            }

        }

        public void UpdateControllerSettings(ControllerVisualSettings settings)
        {
            foreach (var meshRenderer in _renderers)
                meshRenderer.enabled = settings.Visible;
        }

        private Dictionary<LaserPointerVisual, MeshRenderer> _pointerReticleMap = new Dictionary<LaserPointerVisual, MeshRenderer>();

        public void UpdatePointerSettings(PointerVisualSettings settings)
        {
            var pointers = GetComponentsInChildren<LaserPointerVisual>(includeInactive: true);
            foreach (var pointer in pointers)
            {
                pointer.transform.localPosition = settings.LocalPosition;
                pointer.transform.localEulerAngles = settings.LocalEulerAngle;

                pointer.DefaultReticleDistance = settings.DefaultReticleDistance;
                pointer.GrowTime = settings.GrowTime;
                pointer.MaxDistance = settings.MaxDistance;
                pointer.HideReticleWhenInvalid = settings.HideReticleWhenInvalid;

                if (!_pointerReticleMap.ContainsKey(pointer))
                {
                    var reticle = pointer.GetComponentInChildren<ReticleVisual>(includeInactive: true);
                    var renderer = reticle.GetComponent<MeshRenderer>();

                    _pointerReticleMap.Add(pointer, renderer);
                }

                if (_pointerReticleMap.ContainsKey(pointer))
                {
                    if(_pointerReticleMap[pointer] != null)
                    _pointerReticleMap[pointer].enabled = settings.ReticleVisibility;
                }
            }
        }

        public enum EUpdateRate
        {
            OnStart,
            OnUpdate
        }
    }
}