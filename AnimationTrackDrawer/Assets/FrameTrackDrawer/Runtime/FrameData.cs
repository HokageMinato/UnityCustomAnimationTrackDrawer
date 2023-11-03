using System;
using System.Linq;
using UnityEngine;

namespace FrameTrackDrawer.Runtime
{
    [Serializable]
    public class FrameTrackData
    {
        #region PUBLIC_PROPERTIES
        [SerializeField] private int totalFrames;
        [SerializeField] private FrameEvent[] frameEvents;
        #endregion

        #region CONSTRUCTOR
        public FrameTrackData(int totalFrames, FrameEvent[] frameEvents)
        {
            this.totalFrames = totalFrames;


            ValidateFrameEvents(frameEvents, () => 
            {
                this.frameEvents = frameEvents;
            });
            
        }
        #endregion

        #region UNITY_CALLBACKS
        void OnValidate() 
        {
            if (totalFrames < 0)
            {
                totalFrames = 0;
                Debug.LogError($"Animation track cant be in negative length");
            }

            frameEvents = StripInvalidValues(frameEvents);
        }
        #endregion

        #region PUBLIC_METHODS
        public void AddFrameEventText(int frameIndex, string text) 
        {
            FrameEvent existingRecord = frameEvents.FirstOrDefault(xEvent => xEvent.frameIndex == frameIndex);
            if (existingRecord == null)
            {
                frameEvents = frameEvents.Append(new FrameEvent() { eventsName = new string[1] { text }, frameIndex = frameIndex}).ToArray();
                return;
            }
            else 
            {
                existingRecord.eventsName.Append($" \n {text}"); 
            }

        }

        public void RemoveFrameEventText(int frameIndex, string text)
        {
            FrameEvent existingRecord = frameEvents.FirstOrDefault(xEvent => xEvent.frameIndex == frameIndex);
            if (existingRecord == null)
            {
                Debug.Log("RC NF");
                return;
            }

            existingRecord.eventsName = existingRecord.eventsName.Where(xEventEntry => !xEventEntry.Equals($"\n {text}")).ToArray();

        }
        #endregion

        #region PRIVATE_METHODS
        private void ValidateFrameEvents(FrameEvent[] frameEvents,Action onValidationSuccess)
        {
            if (frameEvents == null)
            {
                Debug.LogError($"Frameevent array is null");
                return;
            }

            frameEvents = StripInvalidValues(frameEvents);

            if (frameEvents.Length == 0)
            {
                Debug.LogError("Invalid values found in array, aborting");
                return;
            }

            onValidationSuccess();

        }

        private FrameEvent[] StripInvalidValues(FrameEvent[] frameEvents)
        {
            frameEvents = frameEvents.Where(xFrameEvent => xFrameEvent.frameIndex >= 0
                                          && xFrameEvent.frameIndex < totalFrames).ToArray();
            return frameEvents;
        }

        #endregion

        #region EDITOR_ACCESSORS
#if UNITY_EDITOR
        public static string TotalFramesFieldName => nameof(totalFrames);
        public static string FrameEventsFieldName => nameof(frameEvents);
        #endif
        #endregion
    }

    [Serializable]
    public class FrameEvent 
    {
        public int frameIndex;
        public string[] eventsName;

        #region EDITOR_ACCESSORS
#if UNITY_EDITOR
        public static string FrameEventsNameFieldName => nameof(eventsName);
        public static string FrameIndexFieldName => nameof(frameIndex);
#endif
        #endregion
    }
}
