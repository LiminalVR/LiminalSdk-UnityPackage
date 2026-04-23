using Liminal.SDK.Extensions;
using Liminal.SDK.VR.Avatars.Controllers;
using Liminal.SDK.VR.EventSystems;
using Liminal.SDK.VR.Input;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Liminal.SDK.VR.Avatars
{
    /// <summary>
    /// A collection of useful extension methods for the VRAvatar system.
    /// </summary>
    public static class VRAvatarExtensions
    {
        /// <summary>
        /// Gets the <see cref="IVRAvatarLimb"/> of the avatar that owns the <see cref="Pointers.IVRPointer"/> that triggered the event.
        /// </summary>
        /// <param name="avatar">The avatar.</param>
        /// <param name="eventData">The event data.</param>
        /// <returns>The <see cref="IVRAvatarLimb"/> that owns the pointer that triggered the event.</returns>
        public static IVRAvatarLimb GetLimb(this IVRAvatar avatar, PointerEventData eventData)
        {
            return GetLimb(avatar, eventData as VRPointerEventData);
        }
        
        /// <summary>
        /// Gets the <see cref="IVRAvatarLimb"/> of the avatar that owns the <see cref="Pointers.IVRPointer"/> that triggered the event.
        /// </summary>
        /// <param name="avatar">The avatar.</param>
        /// <param name="eventData">The event data.</param>
        /// <returns>The <see cref="IVRAvatarLimb"/> that owns the pointer that triggered the event.</returns>
        public static IVRAvatarLimb GetLimb(this IVRAvatar avatar, VRPointerEventData eventData)
        {
            if (avatar == null)
                throw new ArgumentNullException("avatar");

            if (eventData == null)
                return null;
            
            var ptr = eventData.Pointer;
            if (ptr == null)
                return null;

            return avatar.GetLimb(ptr.DeviceComponent);
        }

        /// <summary>
        /// Gets the <see cref="IVRAvatarHand"/> of the avatar that owns the <see cref="Pointers.IVRPointer"/> that triggered the event.
        /// </summary>
        /// <param name="avatar">The avatar.</param>
        /// <param name="eventData">The event data.</param>
        /// <returns>The <see cref="IVRAvatarHand"/> that owns the pointer that triggered the event.</returns>
        public static IVRAvatarHand GetHand(this IVRAvatar avatar, PointerEventData eventData)
        {
            return GetHand(avatar, eventData as VRPointerEventData);
        }

        /// <summary>
        /// Gets the <see cref="IVRAvatarHand"/> of the avatar that owns the <see cref="Pointers.IVRPointer"/> that triggered the event.
        /// </summary>
        /// <param name="avatar">The avatar.</param>
        /// <param name="eventData">The event data.</param>
        /// <returns>The <see cref="IVRAvatarHand"/> that owns the pointer that triggered the event.</returns>
        public static IVRAvatarHand GetHand(this IVRAvatar avatar, VRPointerEventData eventData)
        {
            if (avatar == null)
                throw new ArgumentNullException("avatar");

            if (eventData == null)
                return null;

            var ptr = eventData.Pointer;
            if (ptr == null)
                return null;

            return avatar.GetHand(ptr.DeviceComponent);
        }

        /// <summary>
        /// Gets the <see cref="IVRAvatarHand"/> for the specified input device handedness.
        /// </summary>
        /// <param name="avatar">The avatar.</param>
        /// <param name="hand">The input device handedness.</param>
        /// <returns>The <see cref="IVRAvatarHand"/> for the specified input device handedness</returns>
        public static IVRAvatarHand GetHand(this IVRAvatar avatar, VRInputDeviceHand hand)
        {
            switch (hand)
            {
                case VRInputDeviceHand.Left:
                    return (avatar.PrimaryHand.LimbType == VRAvatarLimbType.LeftHand)
                        ? avatar.PrimaryHand
                        : avatar.SecondaryHand;

                case VRInputDeviceHand.Right:
                    return (avatar.PrimaryHand.LimbType == VRAvatarLimbType.RightHand)
                        ? avatar.PrimaryHand
                        : avatar.SecondaryHand;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the <see cref="IVRAvatarHand"/> assigned to the specified <see cref="IVRDeviceComponent"/>. If the limb assigned to the device component does not implement <see cref="IVRAvatarHand"/>, a null reference will be returned.
        /// </summary>
        /// <param name="avatar">The avatar.</param>
        /// <param name="deviceComponent">The VR device component of the limb to retrieve.</param>
        /// <returns>The <see cref="IVRAvatarHand"/> assigned to the specified <see cref="IVRDeviceComponent"/>, or null if the assigned limb does not implement <see cref="IVRAvatarHand"/>.</returns>
        public static IVRAvatarHand GetHand(this IVRAvatar avatar, IVRDeviceComponent deviceComponent)
        {
            if (deviceComponent == null)
                return null;

            for (int i = 0; i < avatar.Limbs.Count; ++i)
            {
                var limb = avatar.Limbs[i];
                if (limb != null && limb.DeviceComponent == deviceComponent)
                    return limb as IVRAvatarHand;
            }

            return null;
        }

        /// <summary>
        /// Creates a GameObject and places it under the avatar's Auxiliary object.
        /// </summary>
        /// <param name="avatar">The avatar.</param>
        /// <param name="name">The name of the GameObject to create.</param>
        /// <param name="hideFlags">Optional hide flags to set on the object.</param>
        /// <returns>The newly created auxiliary GameObject.</returns>
        public static GameObject CreateAuxiliaryObject(this IVRAvatar avatar, string name, HideFlags hideFlags = HideFlags.None)
        {
            var gameObject = new GameObject(name) { hideFlags = hideFlags };
            gameObject.transform.SetParentAndIdentity(avatar.Auxiliaries);
            return gameObject;
        }

        /// <summary>
        /// Gets the <see cref="VRControllerVisual"/> for the limb that triggered a pointer event.
        /// </summary>
        /// <param name="avatar">The avatar.</param>
        /// <param name="eventData">The pointer event data.</param>
        /// <returns>The <see cref="VRControllerVisual"/> for the limb that triggered the pointer event, or null if no controller is found.</returns>
        public static VRControllerVisual GetControllerVisual(this IVRAvatar avatar, PointerEventData eventData)
        {
            return GetControllerVisual(GetLimb(avatar, eventData));
        }

        /// <summary>
        /// Gets the <see cref="VRControllerVisual"/> for the limb that triggered a VR pointer event.
        /// </summary>
        /// <param name="avatar">The avatar.</param>
        /// <param name="eventData">The pointer event data.</param>
        /// <returns>The <see cref="VRControllerVisual"/> for the limb that triggered the VR pointer event, or null if no controller is found.</returns>
        public static VRControllerVisual GetControllerVisual(this IVRAvatar avatar, VRPointerEventData eventData)
        {
            return GetControllerVisual(GetLimb(avatar, eventData));
        }

        /// <summary>
        /// Gets the <see cref="VRControllerVisual"/> for the <see cref="IVRInputDevice"/> assigned to the specified hand.
        /// </summary>
        /// <param name="avatar">The avatar.</param>
        /// <param name="hand">The avatar hand.</param>
        /// <returns>The <see cref="VRControllerVisual"/> for the specified hand, or null if no controller is found.</returns>
        public static VRControllerVisual GetControllerVisual(this IVRAvatar avatar, VRInputDeviceHand hand)
        {
            return GetControllerVisual(GetHand(avatar, hand));
        }

        /// <summary>
        /// Gets the <see cref="VRControllerVisual"/> for the specified <see cref="IVRAvatarLimb"/>.
        /// </summary>
        /// <param name="limb">The avatar limb.</param>
        /// <returns>The <see cref="VRControllerVisual"/> for the specified limb, or null if no controller is found.</returns>
        public static VRControllerVisual GetControllerVisual(this IVRAvatarLimb limb)
        {
            if (limb == null || limb.Transform == null)
                return null;

            var controller = limb.Transform.GetComponentInChildren<VRAvatarController>();
            if (controller == null)
                return null;

            return controller.ControllerVisual;
        }

        /// <summary>
        /// Instantiates a <see cref="VRControllerVisual"/> for this limb.
        /// </summary>
        /// <param name="limb">The limb for the controller.</param>
        /// <returns>The newly instantiated controller visual for this limb, or null if no controller visual was able to be created.</returns>
        public static VRControllerVisual InstantiateControllerVisual(this IVRAvatarLimb limb)
        {
            if (limb == null)
                throw new ArgumentNullException("limb");

            if (limb.Avatar == null || limb.Avatar.Transform == null)
                return null;

            var deviceAvatar = limb.Avatar.Transform.GetComponent<IVRDeviceAvatar>();
            if (deviceAvatar == null)
                return null;

            return deviceAvatar.InstantiateControllerVisual(limb);
        }
    }
}
