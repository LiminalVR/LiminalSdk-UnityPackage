using UnityEngine;
using UnityEngine.EventSystems;

namespace Liminal.SDK.V2
{
    public class ClickTestObject : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log("Clicked");
        }
    }
}