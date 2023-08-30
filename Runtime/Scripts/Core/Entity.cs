using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{
    public bool HasInit{get{return hasInit;}}

    [Header("Entity")]
    [SerializeField] bool hasInit = false; 
    protected virtual void Awake() {
        
    }

    protected virtual void Start()
    {
       
    }

    public virtual void Init()
    {   
        hasInit = true; 
    }

    protected virtual void OnDestroy()
    {
        if(hasInit) {
            Destroy();
        }
    }

    protected virtual void Destroy() {

    }
 
}
