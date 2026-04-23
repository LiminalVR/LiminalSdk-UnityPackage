using Liminal.SDK.VR.Pointers;
using System.Collections.Generic;
using System.Linq;
using Liminal.SDK.VR.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Liminal.SDK.VR.EventSystems
{
    /// <summary>
    /// A concrete implementation of <see cref="BaseInputModule"/> that handles input from <see cref="IVRPointer"/> instances.
    /// </summary>
    [DisallowMultipleComponent]
    public class VRPointerInputModule : BaseInputModule
    {
        public LayerMask InteractableLayers = -1;

        #region Static

        /// <summary>
        /// The camera used for raycasting into the scene.
        /// </summary>
        public static Camera RaycastEventCamera;

        public static readonly List<PointerData> _pointerDataList = new List<PointerData>();

        /// <summary>
        /// Adds a <see cref="IVRPointer"/> to the module for processing. If the pointer already exists in the internal pointer list, it will not be added again.
        /// </summary>
        /// <param name="pointer">The <see cref="IVRPointer"/> to add to the module.</param>
        public static void AddPointer(IVRPointer pointer)
        {
            // TODO Add some form of identity to the pointers for better debugging purposes.

            if (_pointerDataList.Any(x => x.Pointer == pointer))
                return;

            _pointerDataList.Add(new PointerData()
            {
                Pointer = pointer,
            });
        }

        /// <summary>
        /// Removes a <see cref="IVRPointer"/> from the input module's processing routine. Returns a boolean indicating if the pointer was removed from the internal pointer list.
        /// </summary>
        /// <param name="pointer">The <see cref="IVRPointer"/> to remove.</param>
        /// <returns>A boolean indicating if the pointer was removed.</returns>
        public static bool RemovePointer(IVRPointer pointer)
        {
            return (_pointerDataList.RemoveAll(x => x.Pointer == pointer) > 0);
        }

        #endregion

        public class PointerData
        {
            public IVRPointer Pointer;
            public VRPointerEventData PointerEvent;
            public GameObject CurrentControl;
            public GameObject CurrentPressed;
            public GameObject CurrentDragging;
            public Vector3 LastWorldPositionPressed;

            public override string ToString()
            {
                return Pointer.DeviceComponent.Name;
            }
        };

        #region MonoBehaviour

        protected override void Start()
        {
            base.Start();

            // Create a new camera that will be used for raycasts
            if (RaycastEventCamera == null)
            {
                RaycastEventCamera = new GameObject("VRInputModuleCamera")
                //{ hideFlags = HideFlags.HideAndDontSave }
                    .AddComponent<Camera>();

                RaycastEventCamera.clearFlags = CameraClearFlags.Nothing;
                RaycastEventCamera.enabled = false;
                RaycastEventCamera.nearClipPlane = 0.01f;
                DontDestroyOnLoad(RaycastEventCamera.gameObject);
            }
        }

        #endregion

        /// <summary>
        /// Process the current tick for the module.
        /// </summary>
        public override void Process()
        {
            for (int i = 0; i < _pointerDataList.Count; ++i)
            {
                ProcessPointer(_pointerDataList[i]);
            }
        }

        private void ProcessPointer(PointerData pointerData)
        {
            if (pointerData == null)
                return;

            var pointer = pointerData.Pointer;

            if (!pointer.IsActive)
                return;


            var ptrTransform = pointer.Transform;
            if (ptrTransform == null)
                return;

            var ev = pointerData.PointerEvent;
            if (ev == null)
            {
                ev = new VRPointerEventData(eventSystem);
                pointerData.PointerEvent = ev;
            }

            pointerData.LastWorldPositionPressed = ev.pointerCurrentRaycast.worldPosition;
            ev.Reset();
            ev.Pointer = pointer;
            ev.delta = Vector2.zero;
            ev.scrollDelta = Vector2.zero;
            
            ev.position = new Vector2(RaycastEventCamera.pixelWidth * 0.5f, RaycastEventCamera.pixelHeight * 0.5f);

            // Setup event camera to cast for controller position and orientation
            RaycastEventCamera.transform.SetPositionAndRotation(ptrTransform.position, ptrTransform.rotation);

            // Trigger a raycast for this pointer
            eventSystem.RaycastAll(ev, m_RaycastResultCache);
            ev.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
            m_RaycastResultCache.Clear();

            if (ev.pointerCurrentRaycast.gameObject != null)
            {
                var layer = ev.pointerCurrentRaycast.gameObject.layer;
                if (InteractableLayers != (InteractableLayers | (1 << layer))) return;
            }

            // Send RaycastResult to the pointer
            var result = ev.pointerCurrentRaycast;
            pointer.CurrentRaycastResult = (result.distance > 0) ? result : default(RaycastResult);

            // Send enter and exit events to the pointer
            var targetControl = ev.pointerCurrentRaycast.gameObject;
            if (pointerData.CurrentControl != targetControl)
            {
                // Hovered control has changed
                // Exit the current control
                if (pointerData.CurrentControl != null)
                    pointer.OnPointerExit(pointerData.CurrentControl);

                pointerData.CurrentControl = targetControl;

                // Enter the new control
                if (targetControl != null)
                    pointer.OnPointerEnter(targetControl);
            }

            // Handle enter and exit events on the UI controls that are currently hit
            HandlePointerExitAndEnter(ev, pointerData.CurrentControl);

            var go = ptrTransform.gameObject;
            if (pointer.GetButtonDown())
            {
                ClearSelection();

                ev.pressPosition = ev.position;
                ev.pointerPressRaycast = ev.pointerCurrentRaycast;
                ev.pointerPress = null;

                pointerData.LastWorldPositionPressed = ev.pointerCurrentRaycast.worldPosition;

                if (pointerData.CurrentControl != null)
                {
                    // Pointer down/press
                    pointerData.CurrentPressed = pointerData.CurrentControl;
                    ev.Current = pointerData.CurrentPressed;
                    var newPressed = ExecuteEvents.ExecuteHierarchy(pointerData.CurrentPressed, ev, ExecuteEvents.pointerDownHandler);
                    ExecuteEvents.Execute(go, ev, ExecuteEvents.pointerDownHandler);

                    if (newPressed == null)
                    {
                        // Execute click handler
                        // Some elements may not have pointerDown handler, only a click handler, so we need to make
                        // sure they are still able to be activated
                        newPressed = ExecuteEvents.ExecuteHierarchy(pointerData.CurrentPressed, ev, ExecuteEvents.pointerClickHandler);
                        ExecuteEvents.Execute(go, ev, ExecuteEvents.pointerClickHandler);

                        if (newPressed != null)
                        {
                            pointerData.CurrentPressed = newPressed;
                        }
                    }
                    else
                    {
                        pointerData.CurrentPressed = newPressed;

                        // Head-tracking can be jittery, so process clicks when pressing down also
                        // This makes it easier to click buttons in VR
                        ExecuteEvents.Execute(newPressed, ev, ExecuteEvents.pointerClickHandler);
                        ExecuteEvents.Execute(go, ev, ExecuteEvents.pointerClickHandler);

                    }

                    if (newPressed != null)
                    {
                        ev.pointerPress = newPressed;
                        pointerData.CurrentPressed = newPressed;
                        Select(pointerData.CurrentPressed);
                    }

                    ExecuteEvents.Execute(pointerData.CurrentPressed, ev, ExecuteEvents.beginDragHandler);
                    ExecuteEvents.Execute(go, ev, ExecuteEvents.beginDragHandler);

                    ev.pointerDrag = pointerData.CurrentPressed;
                    pointerData.CurrentDragging = pointerData.CurrentPressed;
                }
            }

            if (pointer.GetButtonUp())
            {
                if (pointerData.CurrentDragging != null)
                {
                    // End dragging
                    ev.Current = pointerData.CurrentDragging;
                    ExecuteEvents.Execute(pointerData.CurrentDragging, ev, ExecuteEvents.endDragHandler);
                    ExecuteEvents.Execute(go, ev, ExecuteEvents.endDragHandler);
                    if (pointerData.CurrentControl != null)
                    {
                        ExecuteEvents.ExecuteHierarchy(pointerData.CurrentControl, ev, ExecuteEvents.dropHandler);
                    }
                    ev.pointerDrag = null;
                    pointerData.CurrentDragging = null;
                }

                if (pointerData.CurrentPressed)
                {
                    // Pointer up/release
                    ev.Current = pointerData.CurrentPressed;
                    ExecuteEvents.Execute(pointerData.CurrentPressed, ev, ExecuteEvents.pointerUpHandler);
                    ExecuteEvents.Execute(go, ev, ExecuteEvents.pointerUpHandler);
                    ev.rawPointerPress = null;
                    ev.pointerPress = null;
                    pointerData.CurrentPressed = null;
                }
            }

            if (pointerData.CurrentDragging != null)
            {
                // Dragging
                ev.Current = pointerData.CurrentDragging;
                
                ExecuteEvents.Execute(pointerData.CurrentDragging, ev, ExecuteEvents.dragHandler);
                ExecuteEvents.Execute(go, ev, ExecuteEvents.dragHandler);

                var currentWorldPosition = ev.pointerCurrentRaycast.worldPosition;
                ev.scrollDelta = (currentWorldPosition - pointerData.LastWorldPositionPressed) * 50;

                if (!Mathf.Approximately(ev.scrollDelta.sqrMagnitude, 0.0f) && ev.scrollDelta.sqrMagnitude < 100)
                {
                    var root = ExecuteEvents.GetEventHandler<IScrollHandler>(ev.pointerCurrentRaycast.gameObject);
                    ExecuteEvents.Execute(root, ev, ExecuteEvents.scrollHandler);
                    pointerData.LastWorldPositionPressed = currentWorldPosition;
                    ev.scrollDelta = Vector2.zero;
                }
            }
            else
            {
                if (pointer is InputDevicePointer inputPointer && inputPointer.InputDevice != null)
                {
                    var axis = inputPointer.InputDevice.GetAxis2D(VRAxis.One);
                    ev.scrollDelta += axis * Time.deltaTime * 200;

                    if (Mathf.Approximately(axis.sqrMagnitude, 0))
                        ev.scrollDelta = Vector2.zero;

                    if (!Mathf.Approximately(ev.scrollDelta.sqrMagnitude, 0.0f) && ev.scrollDelta.sqrMagnitude < 100)
                    {
                        var root = ExecuteEvents.GetEventHandler<IScrollHandler>(ev.pointerCurrentRaycast.gameObject);
                        ExecuteEvents.Execute(root, ev, ExecuteEvents.scrollHandler);
                        ev.scrollDelta = Vector2.zero;
                    }
                }
            }

            if (eventSystem.currentSelectedGameObject != null)
            {
                // Update selected object (focus)
                ev.Current = eventSystem.currentSelectedGameObject;
                ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, GetBaseEventData(), ExecuteEvents.updateSelectedHandler);
            }
        }
        

        private void ClearSelection()
        {
            if (eventSystem.currentSelectedGameObject != null)
            {
                eventSystem.SetSelectedGameObject(null);
            }
        }

        private void Select(GameObject go)
        {
            ClearSelection();

            if (ExecuteEvents.GetEventHandler<ISelectHandler>(go))
            {
                eventSystem.SetSelectedGameObject(go);
            }
        }
    }
}
