using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using mud.Client;
public class EntityDictionary : MonoBehaviour
{
    public static Dictionary<string, MUDEntity> Entities;
    public static MUDEntity GetEntity(string Key) { return Entities[Key]; }
    public static MUDEntity GetEntitySafe(string Key) { MUDEntity e; Entities.TryGetValue(Key, out e); return e; }
    public static void ToggleEntity(bool toggle, MUDEntity entity)
    {
        if (toggle) { Entities.Add(entity.Key, entity); }
        else { Entities.Remove(entity.Key); }
    }

    void Awake()
    {
        if (Entities == null)
        {
            Entities = new Dictionary<string, MUDEntity>();
        }

    }
}
