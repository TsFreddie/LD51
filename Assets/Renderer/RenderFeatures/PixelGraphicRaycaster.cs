using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class PixelGraphicRaycaster : BaseRaycaster
{
    public PixelScaler PixelScaler;

    protected const int kNoEventMaskSet = -1;

    /// <summary>
    /// Type of raycasters to check against to check for canvas blocking elements.
    /// </summary>
    public enum BlockingObjects
    {
        /// <summary>
        /// Perform no raycasts.
        /// </summary>
        None = 0,
        /// <summary>
        /// Perform a 2D raycast check to check for blocking 2D elements
        /// </summary>
        TwoD = 1,
        /// <summary>
        /// Perform a 3D raycast check to check for blocking 3D elements
        /// </summary>
        ThreeD = 2,
        /// <summary>
        /// Perform a 2D and a 3D raycasts to check for blocking 2D and 3D elements.
        /// </summary>
        All = 3,
    }

    /// <summary>
    /// Priority of the raycaster based upon sort order.
    /// </summary>
    /// <returns>
    /// The sortOrder priority.
    /// </returns>
    public override int sortOrderPriority
    {
        get
        {
            // We need to return the sorting order here as distance will all be 0 for overlay.
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return canvas.sortingOrder;

            return base.sortOrderPriority;
        }
    }

    /// <summary>
    /// Priority of the raycaster based upon render order.
    /// </summary>
    /// <returns>
    /// The renderOrder priority.
    /// </returns>
    public override int renderOrderPriority
    {
        get
        {
            // We need to return the sorting order here as distance will all be 0 for overlay.
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return canvas.rootCanvas.renderOrder;

            return base.renderOrderPriority;
        }
    }

    [FormerlySerializedAs("ignoreReversedGraphics")]
    [SerializeField]
    private bool m_IgnoreReversedGraphics = true;
    [FormerlySerializedAs("blockingObjects")]
    [SerializeField]
    private BlockingObjects m_BlockingObjects = BlockingObjects.None;

    /// <summary>
    /// Whether Graphics facing away from the raycaster are checked for raycasts.
    /// </summary>
    public bool ignoreReversedGraphics { get { return m_IgnoreReversedGraphics; } set { m_IgnoreReversedGraphics = value; } }

    /// <summary>
    /// The type of objects that are checked to determine if they block graphic raycasts.
    /// </summary>
    public BlockingObjects blockingObjects { get { return m_BlockingObjects; } set { m_BlockingObjects = value; } }

    [SerializeField]
    protected LayerMask m_BlockingMask = kNoEventMaskSet;

    /// <summary>
    /// The type of objects specified through LayerMask that are checked to determine if they block graphic raycasts.
    /// </summary>
    public LayerMask blockingMask { get { return m_BlockingMask; } set { m_BlockingMask = value; } }

    private Canvas m_Canvas;

    protected PixelGraphicRaycaster()
    {
    }

    private Canvas canvas
    {
        get
        {
            if (m_Canvas != null)
                return m_Canvas;

            m_Canvas = GetComponent<Canvas>();
            return m_Canvas;
        }
    }

    [NonSerialized] private List<Graphic> m_RaycastResults = new List<Graphic>();

    /// <summary>
    /// A version of Display.RelativeMouseAt that scales the position when the main display has a different rendering resolution to the system resolution.
    /// By default, the mouse position is relative to the main render area, we need to adjust this so it is relative to the system resolution
    /// in order to correctly determine the position on other displays.
    /// </summary>
    /// <returns></returns>
    public static Vector3 RelativeMouseAtScaled(Vector2 position)
    {
            #if !UNITY_EDITOR && !UNITY_WSA
            // If the main display is now the same resolution as the system then we need to scale the mouse position. (case 1141732)
            if (Display.main.renderingWidth != Display.main.systemWidth || Display.main.renderingHeight != Display.main.systemHeight)
            {
                // The system will add padding when in full-screen and using a non-native aspect ratio. (case UUM-7893)
                // For example Rendering 1920x1080 with a systeem resolution of 3440x1440 would create black bars on each side that are 330 pixels wide.
                // we need to account for this or it will offset our coordinates when we are not on the main display.
                var systemAspectRatio = Display.main.systemWidth / (float)Display.main.systemHeight;

                var sizePlusPadding = new Vector2(Display.main.renderingWidth, Display.main.renderingHeight);
                var padding = Vector2.zero;
                if (Screen.fullScreen)
                {
                    var aspectRatio = Screen.width / (float)Screen.height;
                    if (Display.main.systemHeight * aspectRatio < Display.main.systemWidth)
                    {
                        // Horizontal padding
                        sizePlusPadding.x = Display.main.renderingHeight * systemAspectRatio;
                        padding.x = (sizePlusPadding.x - Display.main.renderingWidth) * 0.5f;
                    }
                    else
                    {
                        // Vertical padding
                        sizePlusPadding.y = Display.main.renderingWidth / systemAspectRatio;
                        padding.y = (sizePlusPadding.y - Display.main.renderingHeight) * 0.5f;
                    }
                }

                var sizePlusPositivePadding = sizePlusPadding - padding;

                // If we are not inside of the main display then we must adjust the mouse position so it is scaled by
                // the main display and adjusted for any padding that may have been added due to different aspect ratios.
                if (position.y < -padding.y || position.y > sizePlusPositivePadding.y ||
                     position.x < -padding.x || position.x > sizePlusPositivePadding.x)
                {
                    var adjustedPosition = position;

                    if (!Screen.fullScreen)
                    {
                        // When in windowed mode, the window will be centered with the 0,0 coordinate at the top left, we need to adjust so it is relative to the screen instead.
                        adjustedPosition.x -= (Display.main.renderingWidth - Display.main.systemWidth) * 0.5f;
                        adjustedPosition.y -= (Display.main.renderingHeight - Display.main.systemHeight) * 0.5f;
                    }
                    else
                    {
                        // Scale the mouse position to account for the black bars when in a non-native aspect ratio.
                        adjustedPosition += padding;
                        adjustedPosition.x *= Display.main.systemWidth / sizePlusPadding.x;
                        adjustedPosition.y *= Display.main.systemHeight / sizePlusPadding.y;
                    }

                    var relativePos = Display.RelativeMouseAt(adjustedPosition);

                    // If we are not on the main display then return the adjusted position.
                    if (relativePos.z != 0)
                        return relativePos;
                }

                // We are using the main display.
                return new Vector3(position.x, position.y, 0);
            }
            #endif
        return Display.RelativeMouseAt(position);
    }

    /// <summary>
    /// Perform the raycast against the list of graphics associated with the Canvas.
    /// </summary>
    /// <param name="eventData">Current event data</param>
    /// <param name="resultAppendList">List of hit objects to append new results to.</param>
    public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
    {
        if (canvas == null)
            return;

        var canvasGraphics = GraphicRegistry.GetRaycastableGraphicsForCanvas(canvas);
        if (canvasGraphics == null || canvasGraphics.Count == 0)
            return;

        int displayIndex;
        var currentEventCamera = eventCamera; // Property can call Camera.main, so cache the reference

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || currentEventCamera == null)
            displayIndex = canvas.targetDisplay;
        else
            displayIndex = currentEventCamera.targetDisplay;

        var eventPosition = RelativeMouseAtScaled(eventData.position);
        if (eventPosition != Vector3.zero)
        {
            // We support multiple display and display identification based on event position.

            int eventDisplayIndex = (int)eventPosition.z;

            // Discard events that are not part of this display so the user does not interact with multiple displays at once.
            if (eventDisplayIndex != displayIndex)
                return;
        }
        else
        {
            // The multiple display system is not supported on all platforms, when it is not supported the returned position
            // will be all zeros so when the returned index is 0 we will default to the event data to be safe.
            eventPosition = eventData.position;

#if UNITY_EDITOR
            if (Display.activeEditorGameViewTarget != displayIndex)
                return;
            eventPosition.z = Display.activeEditorGameViewTarget;
#endif

            // We dont really know in which display the event occured. We will process the event assuming it occured in our display.
        }

        // Convert coordinate to pixel scaler render texture
        eventPosition = (Vector3)PixelScaler.ScreenToRenderTexture(eventPosition) + new Vector3(0, 0, eventPosition.z);

        // Convert to view space
        Vector2 pos;
        if (currentEventCamera == null)
        {
            // Multiple display support only when not the main display. For display 0 the reported
            // resolution is always the desktops resolution since its part of the display API,
            // so we use the standard none multiple display method. (case 741751)
            float w = Screen.width;
            float h = Screen.height;
            if (displayIndex > 0 && displayIndex < Display.displays.Length)
            {
                w = Display.displays[displayIndex].systemWidth;
                h = Display.displays[displayIndex].systemHeight;
            }
            pos = new Vector2(eventPosition.x / w, eventPosition.y / h);
        }
        else
            pos = currentEventCamera.ScreenToViewportPoint(eventPosition);

        // If it's outside the camera's viewport, do nothing
        if (pos.x < 0f || pos.x > 1f || pos.y < 0f || pos.y > 1f)
            return;

        float hitDistance = float.MaxValue;

        Ray ray = new Ray();

        if (currentEventCamera != null)
            ray = currentEventCamera.ScreenPointToRay(eventPosition);

        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && blockingObjects != BlockingObjects.None)
        {
            float distanceToClipPlane = 100.0f;

            if (currentEventCamera != null)
            {
                float projectionDirection = ray.direction.z;
                distanceToClipPlane = Mathf.Approximately(0.0f, projectionDirection)
                    ? Mathf.Infinity
                    : Mathf.Abs((currentEventCamera.farClipPlane - currentEventCamera.nearClipPlane) / projectionDirection);
            }
#if PACKAGE_PHYSICS
                if (blockingObjects == BlockingObjects.ThreeD || blockingObjects == BlockingObjects.All)
                {
                    if (ReflectionMethodsCache.Singleton.raycast3D != null)
                    {
                        var hits = ReflectionMethodsCache.Singleton.raycast3DAll(ray, distanceToClipPlane, (int)m_BlockingMask);
                        if (hits.Length > 0)
                            hitDistance = hits[0].distance;
                    }
                }
#endif
#if PACKAGE_PHYSICS2D
                if (blockingObjects == BlockingObjects.TwoD || blockingObjects == BlockingObjects.All)
                {
                    if (ReflectionMethodsCache.Singleton.raycast2D != null)
                    {
                        var hits = ReflectionMethodsCache.Singleton.getRayIntersectionAll(ray, distanceToClipPlane, (int)m_BlockingMask);
                        if (hits.Length > 0)
                            hitDistance = hits[0].distance;
                    }
                }
#endif
        }

        m_RaycastResults.Clear();

        Raycast(canvas, currentEventCamera, eventPosition, canvasGraphics, m_RaycastResults);

        int totalCount = m_RaycastResults.Count;
        for (var index = 0; index < totalCount; index++)
        {
            var go = m_RaycastResults[index].gameObject;
            bool appendGraphic = true;

            if (ignoreReversedGraphics)
            {
                if (currentEventCamera == null)
                {
                    // If we dont have a camera we know that we should always be facing forward
                    var dir = go.transform.rotation * Vector3.forward;
                    appendGraphic = Vector3.Dot(Vector3.forward, dir) > 0;
                }
                else
                {
                    // If we have a camera compare the direction against the cameras forward.
                    var cameraForward = currentEventCamera.transform.rotation * Vector3.forward * currentEventCamera.nearClipPlane;
                    appendGraphic = Vector3.Dot(go.transform.position - currentEventCamera.transform.position - cameraForward, go.transform.forward) >= 0;
                }
            }

            if (appendGraphic)
            {
                float distance = 0;
                Transform trans = go.transform;
                Vector3 transForward = trans.forward;

                if (currentEventCamera == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    distance = 0;
                else
                {
                    // http://geomalgorithms.com/a06-_intersect-2.html
                    distance = (Vector3.Dot(transForward, trans.position - ray.origin) / Vector3.Dot(transForward, ray.direction));

                    // Check to see if the go is behind the camera.
                    if (distance < 0)
                        continue;
                }

                if (distance >= hitDistance)
                    continue;

                var castResult = new RaycastResult
                {
                    gameObject = go,
                    module = this,
                    distance = distance,
                    screenPosition = eventPosition,
                    displayIndex = displayIndex,
                    index = resultAppendList.Count,
                    depth = m_RaycastResults[index].depth,
                    sortingLayer = canvas.sortingLayerID,
                    sortingOrder = canvas.sortingOrder,
                    worldPosition = ray.origin + ray.direction * distance,
                    worldNormal = -transForward
                };
                resultAppendList.Add(castResult);
            }
        }
    }

    /// <summary>
    /// The camera that will generate rays for this raycaster.
    /// </summary>
    /// <returns>
    /// - Null if Camera mode is ScreenSpaceOverlay or ScreenSpaceCamera and has no camera.
    /// - canvas.worldCanvas if not null
    /// - Camera.main.
    /// </returns>
    public override Camera eventCamera
    {
        get
        {
            var canvas = this.canvas;
            var renderMode = canvas.renderMode;
            if (renderMode == RenderMode.ScreenSpaceOverlay
                || (renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null))
                return null;

            return canvas.worldCamera ?? Camera.main;
        }
    }

    /// <summary>
    /// Perform a raycast into the screen and collect all graphics underneath it.
    /// </summary>
    [NonSerialized] static readonly List<Graphic> s_SortedGraphics = new List<Graphic>();
    private static void Raycast(Canvas canvas, Camera eventCamera, Vector2 pointerPosition, IList<Graphic> foundGraphics, List<Graphic> results)
    {
        // Necessary for the event system
        int totalCount = foundGraphics.Count;
        for (int i = 0; i < totalCount; ++i)
        {
            Graphic graphic = foundGraphics[i];

            // -1 means it hasn't been processed by the canvas, which means it isn't actually drawn
            if (!graphic.raycastTarget || graphic.canvasRenderer.cull || graphic.depth == -1)
                continue;

            if (!RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, pointerPosition, eventCamera, graphic.raycastPadding))
                continue;

            if (eventCamera != null && eventCamera.WorldToScreenPoint(graphic.rectTransform.position).z > eventCamera.farClipPlane)
                continue;

            if (graphic.Raycast(pointerPosition, eventCamera))
            {
                s_SortedGraphics.Add(graphic);
            }
        }

        s_SortedGraphics.Sort((g1, g2) => g2.depth.CompareTo(g1.depth));
        totalCount = s_SortedGraphics.Count;
        for (int i = 0; i < totalCount; ++i)
            results.Add(s_SortedGraphics[i]);

        s_SortedGraphics.Clear();
    }
}
