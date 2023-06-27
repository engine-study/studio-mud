using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using mud.Client;

public abstract class MUDComponent : MonoBehaviour
{
    public MUDEntity Entity { get { return entity; } }
    public bool Loaded { get { return loaded; } }
    public List<MUDComponent> RequiredComponents {get{return requiredComponents;}}
    public System.Action OnLoaded, OnUpdated;

    [Header("Settings")]
    [SerializeField] List<MUDComponent> requiredComponents;


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

        //always delay a frame so that RequiredComponents has been fully added to by any other scripts on Start and Awake
        await UniTask.Delay(100);

        for(int i = 0; i < requiredComponents.Count; i++) {
            await entity.GetMUDComponentAsync(requiredComponents[i]);
            componentsLoaded++;
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

        OnUpdated?.Invoke();
    }



}
