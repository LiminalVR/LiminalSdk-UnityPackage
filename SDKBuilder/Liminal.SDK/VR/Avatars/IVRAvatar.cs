using Liminal.Core.Fader;
using Liminal.SDK.VR.Avatars.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Liminal.SDK.VR.Avatars
{
    /// <summary>
    /// Base interface for VRAvatar instances.
    /// </summary>
    public interface IVRAvatar
    {
        /// <summary>
        /// Indicates if the avatar is currently active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Gets the avatar's transform.
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// Gets the container for auxiliary systems.
        /// </summary>
        Transform Auxiliaries { get; }

        /// <summary>
        /// Gets the Head limb.
        /// </summary>
        IVRAvatarHead Head { get; }
        
        /// <summary>
        /// Gets the primary hand limb.
        /// </summary>
        IVRAvatarHand PrimaryHand { get; }

        /// <summary>
        /// Gets the secondary hand limb.
        /// </summary>
        IVRAvatarHand SecondaryHand { get; }

        /// <summary>
        /// Gets a list of all hand limbs.
        /// </summary>
        IList<IVRAvatarHand> Hands { get; }

        /// <summary>
        /// Gets a list of all limbs.
        /// </summary>
        IList<IVRAvatarLimb> Limbs { get; }

        /// <summary>
        /// Gets the <see cref="IScreenFader"/> that controls the screen fades for this avatar.
        /// </summary>
        IScreenFader ScreenFader { get; }

        /// <summary>
        /// Gets the forward looking direction vector for the avatar. This is a shortcut to the head's active eye camera forward.
        /// </summary>
        Vector3 LookForward { get; }

        /// <summary>
        /// Gets the looking rotation for the avatar. This is a shortcut to the head's active eye camera rotation.
        /// </summary>
        Quaternion LookRotation { get; }

        /// <summary>
        /// Initializes the avatar extension components.
        /// </summary>
        void InitializeExtensions();

        /// <summary>
        /// Gets the limb for the specified <see cref="VRAvatarLimbAlias"/> alias.
        /// </summary>
        /// <param name="alias">The limb alias.</param>
        /// <returns>The <see cref="IVRAvatarLimb"/> for the specified alias, or null if no limb is available for the supplied alias value.</returns>
        IVRAvatarLimb GetLimb(VRAvatarLimbAlias alias);

        /// <summary>
        /// Gets the first <see cref="IVRAvatarLimb"/> of the specified type attached to the avatar.
        /// </summary>
        /// <param name="type">The <see cref="VRAvatarLimbType"/> of the limb to retrieve.</param>
        /// <returns>The first <see cref="IVRAvatarLimb"/> of the specified type.</returns>
        IVRAvatarLimb GetLimb(VRAvatarLimbType type);

        /// <summary>
        /// Gets the <see cref="IVRAvatarLimb"/> assigned to the specified <see cref="IVRDeviceComponent"/>.
        /// </summary>
        /// <param name="deviceComponent">The VR device component of the limb to retrieve.</param>
        /// <returns>The <see cref="IVRAvatarLimb"/> assigned to the specified <see cref="IVRDeviceComponent"/>.</returns>
        IVRAvatarLimb GetLimb(IVRDeviceComponent deviceComponent);

        /// <summary>
        /// Get the first extension of the specified type, or null if no extension exists of this type.
        /// </summary>
        /// <typeparam name="TExtension">The type of the extension.</typeparam>
        /// <returns>The extension of the specified type, or null if no extension exists of this type.</returns>
        TExtension GetExtension<TExtension>() where TExtension : IVRAvatarExtension;

        /// <summary>
        /// Gets all extensions of the specified type and places them into the supplied list and returns the number of objects that were added to the list.
        /// The list is cleared before any objects are added.
        /// </summary>
        /// <typeparam name="TExtension">The type of the extensions to add.</typeparam>
        /// <param name="list">The list to add the extensions to.</param>
        /// <returns>The number of objects that were added to the list.</returns>
        int GetExtensions<TExtension>(IList<TExtension> list) where TExtension : IVRAvatarExtension;

        /// <summary>
        /// Get the first extension of the specified type, or null if no extension exists of this type.
        /// </summary>
        /// <param name="type">The type of the extension.</param>
        /// <returns>The extension of the specified type, or null if no extension exists of this type.</returns>
        IVRAvatarExtension GetExtension(Type type);

        /// <summary>
        /// Gets all extensions of the specified type and places them into the supplied list and returns the number of objects that were added to the list.
        /// The list is cleared before any objects are added.
        /// </summary>
        /// <param name="type">The type of the extensions to add.</param>
        /// <param name="list">The list to add the extensions to.</param>
        /// <returns>The number of objects that were added to the list.</returns>
        int GetExtensions(Type type, IList<IVRAvatarExtension> list);

        /// <summary>
        /// Sets the active state for the avatar.
        /// </summary>
        /// <param name="activeState">The active state for the avatar.</param>
        void SetActive(bool activeState);

        /// <summary>
        /// Indicates if the avatar has a <see cref="IVRAvatarLimb"/> of the specified <see cref="VRAvatarLimbType"/> type.
        /// </summary>
        /// <param name="type">The <see cref="VRAvatarLimbType"/> of the limb to search for.</param>
        /// <returns>A boolean indicating if the avatar has a <see cref="IVRAvatarLimb"/> of the specified <see cref="VRAvatarLimbType"/> type.</returns>
        bool HasLimb(VRAvatarLimbType type);

        /// <summary>
        /// Sets the active state for the hand limbs.
        /// </summary>
        /// <param name="active">The active state for the hand limbs.</param>
        void SetHandsActive(bool active);

        /// <summary>
        /// Sets the active state for all limbs of the specified <see cref="VRAvatarLimbType"/>.
        /// </summary>
        /// <param name="limbType">The <see cref="VRAvatarLimbType"/>.</param>
        /// <param name="activeState">The active state for all limbs of the supplied type.</param>
        void SetLimbActiveState(VRAvatarLimbType limbType, bool activeState);
    }
}
