using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using mud.Client;
public class EntityDictionary : MonoBehaviour
{
    public static System.Action OnEntitySpawned;
    public static System.Action OnEntityDestroyed;
    public static EntityDictionary Instance;

    private static GameObject entityPrefab;
    private static Dictionary<string, MUDEntity> m_Entities;
    public static MUDEntity GetEntity(string Key) { return m_Entities[Key]; }
    public static MUDEntity GetEntitySafe(string Key) { MUDEntity e; m_Entities.TryGetValue(Key, out e); return e; }

    [Header("Settings")]
    [SerializeField] Transform entityParent;

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Mutiple instances.", Instance);
        }

        Instance = this;
        m_Entities = new Dictionary<string, MUDEntity>();

    }

    void OnDestroy()
    {
        Instance = null;
    }

    public static MUDEntity FindOrSpawnEntity(string newKey)
    {
        if(Instance == null) {
            Debug.LogError("No EntityDictionary Instance found");
            return null;
        }
        
        //get the entity if it exists or spawn it
        MUDEntity newEntity = GetEntitySafe(newKey);

        if (newEntity != null)
        {
            // Debug.Log("Found " + newEntity.name, Instance);
        }
        else
        {
            if (entityPrefab == null)
            {
                entityPrefab = (Resources.Load("Entity") as GameObject);
            }

            //spawn the entity if it doesnt exist
            newEntity = Instantiate(entityPrefab, Vector3.up * -1000f, Quaternion.identity, Instance.entityParent).GetComponent<MUDEntity>();
            newEntity.gameObject.name = "Entity [" + MUDHelper.TruncateHash(newKey) + "]";

            newEntity.SetMudKey(newKey);
            ToggleEntity(true, newEntity);

            // Debug.Log("Spawned " + newEntity.name, Instance);

            OnEntitySpawned?.Invoke();

        }


        return newEntity;
    }

    public static void DestroyEntity(string newKey)
    {

        OnEntityDestroyed?.Invoke();

        MUDEntity newEntity = m_Entities[newKey];

        ToggleEntity(false, null);

        m_Entities.Remove(newKey);
        Destroy(newEntity);


    }

    public static void ToggleEntity(bool toggle, MUDEntity entity)
    {
        if (toggle) { m_Entities.Add(entity.Key, entity); }
        else { m_Entities.Remove(entity.Key); }
    }

}
