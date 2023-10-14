using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using mud.Client;
using Cysharp.Threading.Tasks;
using mud.Unity;

public class TableSpawner : MonoBehaviour {

    public bool Loaded {get{return loaded;}} 
    public static System.Action OnComplete;
    
    [Header("Spawner")]
    [SerializeField] bool autoSpawn = true;
    [SerializeField] private MUDComponent[] spawnTables;

    [Header("Debug")]
    [SerializeField] bool debugAllTables = false;
    [SerializeField] bool loaded = false;


    void Start() {
        if(autoSpawn) {
            SpawnTables();
        }
    }

    void OnDestroy() {
        loaded = false; 
    }

    public async void SpawnTables() {
        await LoadTables();
    }

    async UniTask LoadTables() {

        if(loaded) {Debug.LogError("Already loaded.", this); return;}

        while(NetworkManager.Initialized == false) {await UniTask.Delay(10);}
        
        for (int i = 0; i < spawnTables.Length; i++) {

            TableManager newTable = (new GameObject()).AddComponent<TableManager>();
            newTable.transform.position = Vector3.zero;
            newTable.transform.parent = transform;

            newTable.SetPrefab(spawnTables[i]);
            newTable.LogTable = debugAllTables;
            newTable.AutoSpawn = true;

            await UniTask.Delay(100);
        }

        loaded = true; 
        OnComplete?.Invoke();
    }

}
