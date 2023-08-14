using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using mud.Client;
using Cysharp.Threading.Tasks;

public class TableSpawner : MonoBehaviour {

    public static bool Loaded = false; 
    public static System.Action OnComplete;
    [SerializeField] private MUDComponent[] spawnTables;

    [SerializeField] bool debugAllTables;

    async void Start() {
        await LoadTables();
    }

    void OnDestroy() {
        Loaded = false; 
    }

    async UniTask LoadTables() {

        for (int i = 0; i < spawnTables.Length; i++) {

            TableManager newTable = (new GameObject()).AddComponent<TableManager>();
            newTable.transform.position = Vector3.zero;
            newTable.transform.parent = transform;

            newTable.componentPrefab = spawnTables[i];
            newTable.gameObject.name = spawnTables[i].TableName;

            newTable.logTable = debugAllTables;

            await UniTask.Delay(50);
        }

        Loaded = true; 
        OnComplete?.Invoke();


    }
}
