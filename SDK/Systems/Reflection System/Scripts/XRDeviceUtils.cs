﻿using UnityEngine;

namespace Liminal.Systems
{
    using System.Collections.Generic;
    using UnityEngine.XR;

    public static class XRDeviceUtils
    {
        public static HashSet<EDeviceModelType> PlanarReflectionSupported = new HashSet<EDeviceModelType>
        {
            EDeviceModelType.Go,
            EDeviceModelType.HtcVive,
            EDeviceModelType.Quest,
            EDeviceModelType.AcerAH101,
            EDeviceModelType.Rift,
            EDeviceModelType.RiftS,
            EDeviceModelType.HtcVivePro,
            EDeviceModelType.Quest2,
            EDeviceModelType.QuestPro,
            EDeviceModelType.Quest3,
        };

        public static EDeviceModelType GetDeviceModelType()
        {
            var name = SystemInfo.deviceName;

            // For some reason quest 3 is unknown after we complete all the signing stages.
            if (name.Contains("Quest 3") || name.Contains("unknown"))
                return EDeviceModelType.Quest3;

            if (name.Equals("Meta Quest Pro"))
                return EDeviceModelType.QuestPro;

            var model = XRDevice.model;
            var type = EDeviceModelType.Unknown;
            model = model.ToLower();

            if (model.Contains("rift"))
            {
                if (model.Contains("rift s"))
                    type = EDeviceModelType.RiftS;
                else
                    type = EDeviceModelType.Rift;
            }

            if (model.Contains("vive"))
            {
                if (model.Contains("pro"))
                    type = EDeviceModelType.HtcVivePro;
                else if (model.Contains("cosmos"))
                    type = EDeviceModelType.HtcViveCosmos;
                else
                    type = EDeviceModelType.HtcVive;
            }

            if (model.Contains("go"))
                type = EDeviceModelType.Go;

            if (model.Contains("quest"))
            {
                var graphicsCardName = SystemInfo.graphicsDeviceName;

                if (graphicsCardName.Contains("650"))
                    type = EDeviceModelType.Quest2;
                else
                    type = EDeviceModelType.Quest;
            }

            if (model.Contains("AcerAH101"))
                type = EDeviceModelType.AcerAH101;

            return type;
        }

        public static bool SupportsPlanarReflection()
        {
            var model = GetDeviceModelType();

#if UNITY_STANDALONE
            if (model == EDeviceModelType.Quest || model == EDeviceModelType.Quest2)
                return false;
#endif

            return PlanarReflectionSupported.Contains(model);
        }
    }
}