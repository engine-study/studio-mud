using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using mud.Client;
public class EntityDictionary : MonoBehaviour
{
    public static System.Action OnEntitySpawned;
    public static System.Action OnEntityDestroyed;
    public static GameObject entityPrefab;

    public static EntityDictionary Instance;
    public static Dictionary<string, MUDEntity> Entities;
    public static MUDEntity GetEntity(string Key) { return Entities[Key]; }
    public static MUDEntity GetEntitySafe(string Key) { MUDEntity e; Entities.TryGetValue(Key, out e); return e; }


    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Mutiple instances.", Instance);
        }

        Instance = this;
        Entities = new Dictionary<string, MUDEntity>();

    }

    void OnDestroy()
    {
        Instance = null;
    }

    public static MUDEntity SpawnEntity(string newKey)
    {

        //get the entity if it exists or spawn it
        MUDEntity newEntity = null;
        EntityDictionary.Entities.TryGetValue(newKey, out newEntity);

        if (newEntity)
        {
            Debug.Log("Found " + newEntity.name, Instance);
        }
        else
        {
            if (entityPrefab == null)
            {
                entityPrefab = (Resources.Load("Entity") as GameObject);
            }

            //spawn the entity if it doesnt exist
            newEntity = Instantiate(entityPrefab, Vector3.up * -1000f, Quaternion.identity).GetComponent<MUDEntity>();
            newEntity.gameObject.name = "Entity [" + MUDHelper.TruncateHash(newKey) + "]";
            Entities.Add(newKey, newEntity);

            newEntity.SetMudKey(newKey);
            ToggleEntity(true, newEntity);

            Debug.Log("Spawned " + newEntity.name, Instance);

        }

        OnEntitySpawned?.Invoke();

        return newEntity;
    }

    public static void DestroyEntity(string newKey)
    {

        OnEntityDestroyed?.Invoke();

        MUDEntity newEntity = Entities[newKey];

        ToggleEntity(false, null);

        Entities.Remove(newKey);
        Destroy(newEntity);


    }

    public static void ToggleEntity(bool toggle, MUDEntity entity)
    {
        if (toggle) { Entities.Add(entity.Key, entity); }
        else { Entities.Remove(entity.Key); }
    }

}
