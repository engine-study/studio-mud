using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace mud{

    //some components help the game keep track of whether and Entity is "active" or not
    //helps keep track of updates with new values
    //and instantiate the component into the right state

    [RequireComponent(typeof(MUDComponent))]
    public class RichState : MonoBehaviour
    {
        MUDComponent component;

        void Awake() {
            component = GetComponent<MUDComponent>();
        }


    }

}
