using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using mud.Client;

public abstract class MUDComponent : MonoBehaviour
{
    public System.Action OnUpdated;
    public MUDEntity Entity { get { return entity; } }
    public bool Loaded { get { return loaded; } }
    public System.Action OnLoaded;

    [Header("Settings")]
    [SerializeField] List<MUDComponent> expectedComponents;


    [Header("Debug")]
    [SerializeField] protected MUDEntity entity;
    [SerializeField] protected MUDTableManager table;
    [SerializeField] bool hasInit;
    [SerializeField] int componentsLoaded = 0;
    [SerializeField] bool loaded = false;

    public virtual void Init(MUDEntity ourEntity, MUDTableManager ourTable)
    {
        entity = ourEntity;
        table = ourTable;
        table.Components.Add(entity.Key, this);

        CheckIfLoaded();

        hasInit = true;
    }

    async UniTaskVoid CheckIfLoaded() {

        for(int i = 0; i < expectedComponents.Count; i++) {
            await entity.GetMUDComponentAsync(expectedComponents[i]);
        }        
       
        loaded = true;
        OnLoaded?.Invoke();
        
    }

    public virtual void OnDestroy()
    {
        if (hasInit)
        {

        }
    }

    public virtual void Cleanup()
    {
        table.Components.Remove(entity.Key);
    }

    public virtual void UpdateComponent(mud.Client.IMudTable table, TableEvent eventType)
    {

        if (eventType == TableEvent.Insert)
        {

        }
        else if (eventType == TableEvent.Delete)
        {

        }
        else if (eventType == TableEvent.Update)
        {

        }

    }



}
