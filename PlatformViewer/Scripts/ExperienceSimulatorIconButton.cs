using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App
{
    public class ExperienceSimulatorIconButton : MonoBehaviour
    {
        public int Id;
        public Button Button;
        public TextMeshProUGUI Label;

        public void Bind(int experienceId)
        {
            Id = experienceId;
            Label.text = Id.ToString();
        }

        [ContextMenu("Trigger Button Click")]
        public void TriggerButtonClick()
        {
            Button.onClick.Invoke();
        }
    }
}