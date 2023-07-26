using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using mud.Client;
public class EntityDictionary : MonoBehaviour {
    public static System.Action OnEntitySpawned;
    public static System.Action OnEntityDestroyed;
    public static EntityDictionary Instance;

    private static Dictionary<string, MUDEntity> m_Entities;
    private static Dictionary<string, MUDEntity> m_chunkedEntities;
    public static MUDEntity GetEntity(string Key) { return m_Entities[Key]; }
    public static MUDEntity GetEntitySafe(string Key) { MUDEntity e; m_Entities.TryGetValue(Key, out e); return e; }

    [Header("Options")]
    [SerializeField] private GameObject entityPrefab;
    [SerializeField] private Transform entityParent;

    [Header("Debug")]
    [SerializeField] private List<MUDEntity> spawnedEntities;

    void Awake() {
        if (Instance != null) {
            Debug.LogError("Mutiple instances.", Instance);
        }

        Instance = this;
        m_Entities = new Dictionary<string, MUDEntity>();
        m_chunkedEntities = new Dictionary<string, MUDEntity>();

    }

    void OnDestroy() {
        Instance = null;
    }

    public static MUDEntity FindOrSpawnEntity(string newKey) {

        if (Instance == null) {
            Debug.LogError("No EntityDictionary Instance found");
            return null;
        }

        //get the entity if it exists or spawn it
        MUDEntity newEntity = GetEntitySafe(newKey);

        if (newEntity != null) {
            // Debug.Log("Found " + newEntity.name, Instance);
        } else {
            if (Instance.entityPrefab == null) {
                Instance.entityPrefab = (Resources.Load("Entity") as GameObject);
            }

            if (Instance.entityParent == null) {
                Instance.entityParent = new GameObject().transform;
                Instance.entityParent.name = "Entities";
            }

            //spawn the entity if it doesnt exist
            newEntity = Instantiate(Instance.entityPrefab, Vector3.zero, Quaternion.identity, Instance.entityParent).GetComponent<MUDEntity>();
            newEntity.gameObject.name = "Entity [" + MUDHelper.TruncateHash(newKey) + "]";

            newEntity.InitEntity(newKey);
            ToggleEntity(true, newEntity);

            // Debug.Log("Spawned " + newEntity.name, Instance);

            OnEntitySpawned?.Invoke();

        }


        return newEntity;
    }

    public static void SpawnAllComponentsOntoEntity(MUDEntity entity) {
        //search all tables to see if they contain this entity
        foreach (TableManager value in TableManager.Tables.Values) {

        }
    }

    public static void DestroyEntity(string newKey) {

        OnEntityDestroyed?.Invoke();

        MUDEntity newEntity = m_Entities[newKey];

        ToggleEntity(false, null);

        m_Entities.Remove(newKey);
        Destroy(newEntity);


    }

    public static void ToggleEntity(bool toggle, MUDEntity entity) {
        if (toggle) {
            Instance.spawnedEntities.Add(entity);
            m_Entities.Add(entity.Key, entity);
        } else {
            Instance.spawnedEntities.Remove(entity);
            m_Entities.Remove(entity.Key);
        }
    }

}
