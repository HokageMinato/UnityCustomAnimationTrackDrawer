using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FrameTrackDrawer.Runtime
{
    public class Sample : MonoBehaviour
    {
        //Declare normally.                                       //supports Constructors.
        [SerializeField] private FrameTrackData frameTrackData = new FrameTrackData(20,
                                                                 new FrameEvent[2] 
                                                                 {   
                                                                    new FrameEvent(){ eventName = "Mark1" , frameIndex = 0 },
                                                                    new FrameEvent(){ eventName = "Mark3" , frameIndex = 2 },
                                                                 });


            void Start()
            {

                
            }

    }
}
