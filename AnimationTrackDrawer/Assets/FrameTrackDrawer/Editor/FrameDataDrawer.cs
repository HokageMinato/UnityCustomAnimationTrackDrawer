using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using FrameTrackDrawer.Runtime;


namespace FrameTrackDrawer.Editor
{
    [CustomPropertyDrawer(typeof(FrameTrackData))]
    public class FrameTrackPropertyDrawer : PropertyDrawer
    {
        #region PRIVATE_CONSTANTS
        private const float BOX_HEIGHT = 20f;
        private const float BAR_WIDTH = 2;
        private const float MAX_FRAME_WIDTH = 50f;
        private const float MIN_FRAME_WIDTH = 14f;
        private const float WIDTH_INCREMENT_PER_DIGIT = 5f;
        private const float SCROLLBAR_HEIGHT = 12f;
        private const string EMPTY = "";
        #endregion


        #region PRIVATE_VARS
        private SerializedProperty targetSerializedPropertyCached;
        private SerializedProperty targetFrameEventPropertyCached;
        private int totalFramesPropertyCached;

        private Vector2 _scrollPosition = Vector2.zero;
        private float _singleFrameBoxWidth = 50f;
        private float _totalWidth;
        private Color disabledBarColor = Color.gray;  // Dark grey
        private Color enabledBarColor = Color.cyan;
        private List<FrameEvent> cachedFrameEvents = new List<FrameEvent>();
        private const string EMPTY_EVENT_MESSAGE = "Empty";
        
        #endregion


        #region PRIVATE_PROPERTIES
        private float HeightWithScrollbar => BOX_HEIGHT + SCROLLBAR_HEIGHT;
        #endregion


        #region UNITY_METHODS
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            CachePropertiesAndValues(property);

            float frameControlWidth = CalculateTotalWidth(totalFramesPropertyCached);
            float guiWidth = EditorGUIUtility.currentViewWidth / 2f;

            if (frameControlWidth < guiWidth)
                return BOX_HEIGHT;

            return HeightWithScrollbar;
        }

       
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, targetSerializedPropertyCached);
            position = DrawLabel(position, label);
            DrawFrameContainerBox(position);
            HandleZooming(Event.current);
            HandleScrolling(position);
            EditorGUI.EndProperty();
        }
        #endregion


        #region PRIVATE_METHODS

        private void CachePropertiesAndValues(SerializedProperty property)
        {
            targetSerializedPropertyCached = property;
            targetFrameEventPropertyCached = targetSerializedPropertyCached.FindPropertyRelative(FrameTrackData.FrameEventsFieldName);
            totalFramesPropertyCached = targetSerializedPropertyCached.FindPropertyRelative(FrameTrackData.TotalFramesFieldName).intValue;
            CacheFrameEvents();
        }

        private void CacheFrameEvents()
        {
            SerializedProperty frameEventsProperty = targetFrameEventPropertyCached;
            int arraySize = frameEventsProperty.arraySize;
            
            cachedFrameEvents.Clear();

            for (int i = 0; i < arraySize; i++)
            {
                SerializedProperty frameEventProperty = frameEventsProperty.GetArrayElementAtIndex(i);
                cachedFrameEvents.Add(new FrameEvent 
                { 
                    frameIndex = frameEventProperty.FindPropertyRelative(FrameEvent.FrameIndexFieldName).intValue, 
                    eventName = frameEventProperty.FindPropertyRelative(FrameEvent.EventNameFieldName).stringValue
                });
            }
        }

        private Rect DrawLabel(Rect position, GUIContent label)
        {
            return EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        }

        private void DrawFrameContainerBox(Rect position)
        {
            GUI.Box(position, EMPTY, GUI.skin.box);
        }

        private void HandleZooming(Event currentEvent)
        {
            if (currentEvent.type == EventType.ScrollWheel)
            {
                _singleFrameBoxWidth -= currentEvent.delta.y;
                _singleFrameBoxWidth = Mathf.Clamp(_singleFrameBoxWidth, MIN_FRAME_WIDTH, MAX_FRAME_WIDTH);
                currentEvent.Use();
            }
        }

        private void HandleScrolling(Rect position)
        {
            GUI.BeginGroup(position);
            _totalWidth = CalculateTotalWidth(totalFramesPropertyCached);
            Rect contentRect = new Rect(0, 0, _totalWidth, BOX_HEIGHT);
            _scrollPosition = GUI.BeginScrollView(new Rect(0, 0, position.width, HeightWithScrollbar), _scrollPosition, contentRect, false, false, GUI.skin.horizontalScrollbar, GUIStyle.none);
            DrawFrameBoxes(contentRect, totalFramesPropertyCached);
            GUI.EndScrollView();
            GUI.EndGroup();
        }

        private void DrawFrameBoxes(Rect position, int totalFrames)
        {
            float xOffset = 0;
            for (int i = 0; i < totalFrames; i++)
            {
                float barWidth = DrawEventBar(position,  xOffset, i);
                DrawFrameBox(position,ref xOffset, i,barWidth);
            }
        }

        private void DrawFrameBox(Rect position,ref float xOffset, int frameIndexI,  float barRectWidth)
        {
            string label = frameIndexI.ToString();
            float currentFrameWidth = _singleFrameBoxWidth + label.Length * WIDTH_INCREMENT_PER_DIGIT;

            Rect frameRect = new Rect(xOffset + barRectWidth, position.y, currentFrameWidth - barRectWidth, BOX_HEIGHT);

            Color originalBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = GetBarColor(frameIndexI);
            EditorGUI.LabelField(frameRect, new GUIContent(label, GetFrameTooltip(frameIndexI)), GetFrameBoxStyle());
            GUI.backgroundColor = originalBackgroundColor;  // Reset background color to original
            xOffset += currentFrameWidth;
        }

        private GUIStyle GetFrameBoxStyle() 
        {
            return  new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter
            };
        }

        private float DrawEventBar(Rect position,  float xOffset, int frameIndexI)
        {
            Color currentBarColor = GetBarColor(frameIndexI);
            Rect barRect = new Rect(xOffset, position.y, BAR_WIDTH, BOX_HEIGHT);
            EditorGUI.DrawRect(barRect, currentBarColor);
            return barRect.width;
        }

        private string GetFrameTooltip(int i)
        {
            foreach (var frameEvent in cachedFrameEvents)
            {
                if (frameEvent.frameIndex == i)
                    return frameEvent.eventName;  // This can be replaced with actual tooltip text
            }
            return EMPTY_EVENT_MESSAGE;
        }


        private Color GetBarColor( int i)
        {
            foreach (var frameEvent in cachedFrameEvents)
            {
                if (frameEvent.frameIndex == i)
                    return enabledBarColor;
            }

            return disabledBarColor;
        }

        private float CalculateTotalWidth(int totalFrames)
        {
            float totalWidth = 0;
            for (int i = 0; i < totalFrames; i++)
            {
                string labelText = i.ToString();
                totalWidth += _singleFrameBoxWidth + labelText.Length * WIDTH_INCREMENT_PER_DIGIT;
            }

            return totalWidth;
        }
        #endregion
    }
}
