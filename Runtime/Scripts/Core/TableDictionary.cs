using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using mud.Unity;
using System;

namespace mud.Client
{

    public class TableDictionary : MonoBehaviour
    {


        public static IMudTable GetTableValue<T>(MUDComponent component) where T : IMudTable{
            // return MUDTableManager.Tables[component.ComponentToTable].GetTableValue(component.Entity.Key);
            return null;
        }
    }
}

