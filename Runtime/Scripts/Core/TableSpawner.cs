using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using mud;
using Cysharp.Threading.Tasks;
using mud;

public class TableSpawner : MonoBehaviour {

    public bool Loaded {get{return syncing;}} 
    public static System.Action OnComplete;

    [Header("Prefabs")]
    [SerializeField] private MUDComponent[] spawnTables;

    [Header("Settings")]
    [SerializeField] bool AutoSpawn = true;
    [SerializeField] bool logAllTables = false;

    [Header("Debug")]
    [SerializeField] bool syncing = false;
    [SerializeField] private List<TableManager> tables;


    void Awake() {
        if(AutoSpawn) {
            if(NetworkManager.Initialized) { SpawnTables(); } 
            else { NetworkManager.OnInitialized += SpawnTables; }
        }
    }


    void OnDestroy() {
        syncing = false; 
        NetworkManager.OnInitialized -= SpawnTables; 
    }

    public async void SpawnTables() {
        await LoadTables();
    }

    async UniTask LoadTables() {

        if(syncing) {Debug.LogError(gameObject.name + ": Already loaded.", this); return;}
        syncing = true; 
        
        tables = new List<TableManager>();

        for (int i = 0; i < spawnTables.Length; i++) {

            TableManager newTable = (new GameObject()).AddComponent<TableManager>();

            newTable.transform.position = Vector3.zero;
            newTable.transform.parent = transform;
            newTable.LogTable = logAllTables;
            newTable.AutoSpawn = false;

            newTable.RegisterTable(spawnTables[i]);
            tables.Add(newTable);
        }

        for (int i = 0; i < spawnTables.Length; i++) {
            tables[i].Spawn(spawnTables[i]);
            await UniTask.Delay(100);
        }

        OnComplete?.Invoke();
    }

}
