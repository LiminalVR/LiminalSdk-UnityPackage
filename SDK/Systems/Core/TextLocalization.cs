using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Liminal.SDK.V2
{
    public class TextLocalization : MonoBehaviour
    {
        public Text UnityText;
        public TMPro.TextMeshProUGUI TextMeshProText;

        public List<LocalizationData> LocalizationDataList;

        public bool UpdateEveryFrame;

        private void OnValidate()
        {
            UnityText = GetComponent<Text>();
            TextMeshProText = GetComponent<TMPro.TextMeshProUGUI>();
        }

        private void Start()
        {
            UpdateText();
        }

        private void Update()
        {
            if(UpdateEveryFrame)
                UpdateText();
        }

        private void UpdateText()
        {
            var type = LiminalControllerManager.IsHandTracking == true ? ELocalizationType.Hands : ELocalizationType.Controllers;
            var data = LocalizationDataList.FirstOrDefault(x => x.Type == type);

            switch (type)
            {
                case ELocalizationType.Controllers:
                    SetText(data.Text);
                    break;
                case ELocalizationType.Hands:
                    SetText(data.Text);
                    break;
            }
        }

        private void SetText(string text)
        {
            if (UnityText != null)
                UnityText.text = text;

            if (TextMeshProText != null)
                TextMeshProText.text = text;
        }
    }

    [Serializable]
    public class LocalizationData
    {
        public ELocalizationType Type;

        [TextArea(5,10)]
        public string Text;
    }

    public enum ELocalizationType
    {
        Controllers,
        Hands
    }
}
