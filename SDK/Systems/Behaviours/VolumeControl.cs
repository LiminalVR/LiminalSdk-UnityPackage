using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Liminal.Tools.Common
{
    public class VolumeControl : MonoBehaviour
    {
        [Tooltip("If volumeSlider is empty, will use slider this script it attached to.")]
        public Slider volumeSlider;

        [Header("Only use 1 of the options below")]
        [Tooltip("If using AudioMixerGroup, name the exposed parameter the same as the group.")]
        public AudioMixerGroup audioGroup;
        [Tooltip("If AudioSource is empty, will use source this script it attached to.")]
        public AudioSource audioSource;

        void Start()
        {
            if (audioSource != null && audioGroup != null)
            {
                Debug.LogError($"{name}.VolumeControl has both AudioGroup and AudioSource set, only use 1");
                return;
            }

            if (audioSource == null && audioGroup == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    Debug.LogError($"{name}.VolumeControl is not attached to an AudioSource");
                    return;
                }
            }

            if (volumeSlider == null)
            {
                volumeSlider = GetComponent<Slider>();
                if (volumeSlider == null)
                {
                    Debug.LogError($"{name}.VolumeControl has volume slider not set.");
                    return;
                }
            }

            if (audioSource != null)
            {
                volumeSlider.SetValueWithoutNotify(audioSource.volume);
            }

            if (audioGroup != null)
            {
                float volume;
                audioGroup.audioMixer.GetFloat(audioGroup.name, out volume);
                var volumeNormalised = Mathf.Sqrt(Mathf.Pow(10, volume / 20));
                volumeSlider.SetValueWithoutNotify(volumeNormalised);
            }

            volumeSlider.minValue = 0;
            volumeSlider.maxValue = 1;
            volumeSlider.wholeNumbers = false;
            volumeSlider.onValueChanged.AddListener(VolumeChanged);
        }

        void VolumeChanged(float newVolume)
        {
            if (audioSource != null)
            {
                audioSource.volume = newVolume;
            }

            if (audioGroup != null)
            {
                float scaledVol = Mathf.Clamp(Mathf.Log10(newVolume * newVolume) * 20, -80, 0);
                audioGroup.audioMixer.SetFloat(audioGroup.name, scaledVol);
            }
        }
    }
}
