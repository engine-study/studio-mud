using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace mud.Client
{

    public class RandomSelector : MonoBehaviour
    {

        [Header("Random")]
        public MUDHelper.RandomSource randomType;
        public GameObject[] objects;
        public int seed = 0;

        [Header("Debug")]
        public int number = -1;
        MUDEntity entity;
        void Start()
        {
            entity = GetComponentInParent<MUDEntity>();

            if(objects.Length == 0) {
                objects = new GameObject[transform.childCount];
                for(int i = 0; i < transform.childCount; i++) {
                    objects[i] = transform.GetChild(i).gameObject;
                }
            }

            if (!entity)
            {
                Debug.LogError("Can't find entity", this);
                return;
            }

            if(entity.HasInit) {
                Init();
            } else {
                entity.OnInit += Init;
            }

        }

        void OnDestroy()
        {
            if (entity)
                entity.OnInit -= Init;
        }
        void Init()
        {

            //a position component on our entity is expected to have updated our position at this point, bad assumption? 
            number = (int)MUDHelper.RandomNumber(0, objects.Length, entity, randomType);

            for (int i = 0; i < objects.Length; i++)
            {
                objects[i].SetActive(i == number);
            }
        }
    }

}