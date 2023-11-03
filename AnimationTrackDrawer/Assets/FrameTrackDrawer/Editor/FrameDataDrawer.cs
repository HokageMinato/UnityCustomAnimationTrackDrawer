using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using FrameTrackDrawer.Runtime;
using UnityEngine.UIElements;
using System;
using NUnit.Framework;
using System.Runtime.ConstrainedExecution;
using System.Linq;

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
        private const float HOVERING_BOX_HEIGHT = 17;
        private const float SCROLLBAR_HEIGHT = 2f;
        private const string EMPTY_STRING = "";
        private static readonly string[] EMPTY_EVENT_MESSAGE = new string[0];
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
        private List<Rect> frameRects = new List<Rect>();

        GUIStyle tooltipBoxStyle;

        private Color originalContentColor;
        private Color originalBackgroundColor;
        #endregion


        #region PRIVATE_PROPERTIES
        private float CalculatedPropertyHeight = 0 ;
        #endregion


        #region UNITY_METHODS
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            CachePropertiesAndValues(property);

            float frameControlWidth = CalculateTotalWidth(totalFramesPropertyCached);
            float guiWidth = EditorGUIUtility.currentViewWidth / 2f;


            float calculatedHeight = BOX_HEIGHT + SCROLLBAR_HEIGHT;
            
            if (frameControlWidth < guiWidth)
                calculatedHeight = BOX_HEIGHT;


            for (int i = 0; i < totalFramesPropertyCached; i++)
            {
                var tooltipTexts = GetFrameTooltipTexts(i);
                foreach (var tooltipText in tooltipTexts)
                    calculatedHeight += HOVERING_BOX_HEIGHT;

            }

            CalculatedPropertyHeight = calculatedHeight;
            return CalculatedPropertyHeight;
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
            CacheHoveringTextBoxStyle();
        }

        private void CacheHoveringTextBoxStyle()
        {
            tooltipBoxStyle = new GUIStyle((GUIStyle)"ProfilerBadge")
            {
                font = EditorStyles.miniButton.font,
                fontStyle = EditorStyles.miniButton.fontStyle,
                fontSize = EditorStyles.miniButton.fontSize,
                alignment = TextAnchor.MiddleCenter
            };
        }

        private void CacheFrameEvents()
        {
            SerializedProperty frameEventsProperty = targetFrameEventPropertyCached;
            int arraySize = frameEventsProperty.arraySize;


            if (cachedFrameEvents.Count != arraySize)
            {
                cachedFrameEvents = new List<FrameEvent>(arraySize);
                for (int i = 0; i < arraySize; i++)
                    cachedFrameEvents.Add(new FrameEvent()
                    {
                        eventsName = new string[0]
                    }); ;
            }


            for (int i = 0; i < cachedFrameEvents.Count; i++)
            {
                SerializedProperty frameEventProperty = frameEventsProperty.GetArrayElementAtIndex(i);
                cachedFrameEvents[i].frameIndex = frameEventProperty.FindPropertyRelative(FrameEvent.FrameIndexFieldName).intValue;
                GetEventValuesNonAlloc(frameEventProperty, ref cachedFrameEvents[i].eventsName);
            }
        }

        private void GetEventValuesNonAlloc(SerializedProperty frameEventProperty, ref string[] contentsArray)
        {
            var eventProps = frameEventProperty.FindPropertyRelative(FrameEvent.FrameEventsNameFieldName);
            int arrayLen = eventProps.arraySize;
            if (contentsArray.Length != arrayLen)
                Array.Resize(ref contentsArray, arrayLen);

            for (int i = 0; i < arrayLen; i++)
                contentsArray[i] = eventProps.GetArrayElementAtIndex(i).stringValue;

        }


        private Rect DrawLabel(Rect position, GUIContent label)
        {
            return EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        }

        private void DrawFrameContainerBox(Rect position)
        {
            GUI.Box(position, EMPTY_STRING, GUI.skin.box);
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
            _scrollPosition = GUI.BeginScrollView(new Rect(0, 0, position.width, CalculatedPropertyHeight), _scrollPosition, contentRect, false, false, GUI.skin.horizontalScrollbar, GUIStyle.none);
            DrawFrameBoxes(contentRect);
            DrawHoveringBlocks(contentRect);
            GUI.EndScrollView();
            GUI.EndGroup();

            
        }

        private void DrawFrameBoxes(Rect position)
        {
            if (frameRects.Count != totalFramesPropertyCached)
            {
                if (frameRects == null)
                    frameRects = new List<Rect>(totalFramesPropertyCached);
                else
                    frameRects.Clear();

                for (int i = 0; i < totalFramesPropertyCached; i++)
                    frameRects.Add(new Rect(0, 0, 100, 100));
            }

            float xOffset = 0;
            for (int i = 0; i < totalFramesPropertyCached; i++)
            {
                string iStr = i.ToString();
                float currentFrameWidth = _singleFrameBoxWidth + iStr.Length * WIDTH_INCREMENT_PER_DIGIT;
                Color currentBarColor = GetBarColor(i);
                DrawEventBar(position,currentBarColor, xOffset);
                frameRects[i] = DrawFrameBox(position,currentBarColor,xOffset,currentFrameWidth ,iStr);
                xOffset += currentFrameWidth;
            }
        }

        private void DrawEventBar(Rect position,Color currentBarColor , float xOffset)
        {
            Rect barRect = new Rect(xOffset, position.y, BAR_WIDTH, BOX_HEIGHT);
            EditorGUI.DrawRect(barRect, currentBarColor);
        }

        private Rect DrawFrameBox(Rect position, Color currentBarColor, float x, float frameWidth,string frameLabel)
        {
            Rect frameRect = new Rect(x, position.y, frameWidth, BOX_HEIGHT);
            ApplyGUIColor(Color.white, currentBarColor);
            EditorGUI.LabelField(frameRect, new GUIContent(frameLabel), GetFrameBoxStyle());
            RevertGUIColor();
            return frameRect;
        }

        private void DrawHoveringBlocks(Rect position)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            for (int i = 0; i < frameRects.Count; i++)
            {
                var tooltipTexts = GetFrameTooltipTexts(i);
                var frameRect = frameRects[i];

                foreach (var tooltipText in tooltipTexts)
                    DrawHoveringTextBlock(ref frameRect, tooltipText);

            }
        }

        private void DrawHoveringTextBlock(ref Rect frameRect, string content)
        {
            float width =  frameRect.width;
            float tooltipX = frameRect.x + frameRect.width / 2 - width / 2;
            float tooltipY = frameRect.y + frameRect.height;
            Rect tooltipRect = new Rect(tooltipX, tooltipY, width, HOVERING_BOX_HEIGHT);
            tooltipBoxStyle.Draw(tooltipRect, content, false, false, false, false);
            frameRect.y += HOVERING_BOX_HEIGHT;
        }

        private void ApplyGUIColor(Color newContentColor, Color newBackgroundColor)
        {
            originalBackgroundColor = GUI.backgroundColor;
            originalContentColor = GUI.contentColor;

            GUI.backgroundColor = newBackgroundColor;
            GUI.contentColor = newContentColor;
        }

        private float HoveringBoxYOffetFromHeight()
        {
            return (HOVERING_BOX_HEIGHT + 8) / 2f;
        }

        private void RevertGUIColor()
        {
            GUI.backgroundColor = originalBackgroundColor;
            GUI.contentColor = originalContentColor;
        }

        private GUIStyle GetFrameBoxStyle()
        {
            return new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter
            };
        }

        

        private string[] GetFrameTooltipTexts(int i)
        {
            foreach (var frameEvent in cachedFrameEvents)
            {
                if (frameEvent.frameIndex == i)
                    return frameEvent.eventsName;
            }
            return EMPTY_EVENT_MESSAGE;
        }


        private Color GetBarColor(int i)
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
