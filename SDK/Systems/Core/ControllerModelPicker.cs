using System;
using Liminal.Systems;
using UnityEngine;

namespace Liminal.SDK.V2
{
    public class ControllerModelPicker : MonoBehaviour
    {
        public ControllerModelPickerData ControllerModelPickerData;

        private void Update()
        {
            var model = XRDeviceUtils.GetDeviceModelType();
            UpdateModel(ControllerModelPickerData, model);
        }

        private void UpdateModel(ControllerModelPickerData modelPicker, EDeviceModelType modelType)
        {
            modelPicker.Quest3Controller.SetActive(false);
            modelPicker.Quest2Controller.SetActive(false);
            modelPicker.QuestProController.SetActive(false);
            modelPicker.UniversalController.SetActive(false);

            switch (modelType)
            {
                case EDeviceModelType.Quest3:
                    modelPicker.Quest3Controller.SetActive(true);
                    break;
                case EDeviceModelType.QuestPro:
                    modelPicker.QuestProController.SetActive(true);
                    break;
                case EDeviceModelType.Quest2:
                    modelPicker.Quest2Controller.SetActive(true);
                    break;
                default:
                    modelPicker.UniversalController.SetActive(true);
                    break;
            }
        }

    }

    [Serializable]
    public class ControllerModelPickerData
    {
        public GameObject Quest3Controller;
        public GameObject Quest2Controller;
        public GameObject QuestProController;
        public GameObject UniversalController;
    }
}
