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
    protected MUDEntity entity;
    protected MUDTableManager table;
    int componentsLoaded = 0;
    bool hasInit;
    bool loaded = false;

    public virtual void Init(MUDEntity ourEntity, MUDTableManager ourTable)
    {
        entity = ourEntity;
        table = ourTable;

        table.RegisterComponent(true, this);

        LoadComponents();

        hasInit = true;
    }


    async UniTaskVoid LoadComponents() {

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
        table.RegisterComponent(false,this);
    }

    public void DoUpdate(mud.Client.IMudTable table, UpdateEvent eventType) {
        UpdateComponent(table,eventType);
        OnUpdated?.Invoke();
    }

    protected virtual void UpdateComponent(mud.Client.IMudTable table, UpdateEvent eventType)
    {

        if (eventType == UpdateEvent.Insert)
        {

        }
        else if (eventType == UpdateEvent.Delete)
        {

        }
        else if (eventType == UpdateEvent.Update)
        {

        }

    }



}
