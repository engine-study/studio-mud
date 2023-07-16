using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using mud.Client;

public class TableSpawner : MonoBehaviour {

    public MUDComponent[] spawnTables;

    void Start() {
        for (int i = 0; i < spawnTables.Length; i++) {

            TableManager newTable = (new GameObject()).AddComponent<TableManager>();
            newTable.transform.position = Vector3.zero;
            newTable.transform.parent = transform;

            newTable.componentPrefab = spawnTables[i];
            newTable.gameObject.name = spawnTables[i].TableType.ToString();
        }
    }
}
