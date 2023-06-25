using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using mud.Client;

public abstract class MUDComponent : MonoBehaviour
{
    public System.Action OnUpdated;
    public MUDEntity Entity {get{return entity;}}
    bool hasInit;

    [Header("Debug")]
    [SerializeField] protected MUDEntity entity;
    [SerializeField] protected MUDTableManager table;

    public virtual void Init(MUDEntity ourEntity, MUDTableManager ourTable) {
        entity = ourEntity;
        table = ourTable;
        table.Components.Add(entity.Key, this);
        hasInit = true;
    }

    public virtual void OnDestroy() {
        if(hasInit) {

        }
    }

    public virtual void Cleanup() {
        table.Components.Remove(entity.Key);
    }

    public virtual void UpdateComponent(mud.Client.IMudTable table, TableEvent eventType) {

        if(eventType == TableEvent.Insert) {

        } else if(eventType == TableEvent.Delete) {

        } else if(eventType == TableEvent.Update) {
            
        }
    }

}
