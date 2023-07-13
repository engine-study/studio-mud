using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace mud.Client {

    public class RandomSelector : MonoBehaviour {

        [Header("Random GameObject")]
        public MUDHelper.RandomSource randomType;
        public int seed = 0;

        [Header("Random GameObject")]
        public bool randomizeChildren = true;
        public GameObject[] objects;

        [Header("Random Rotation")]
        public bool rotateY = true;
        public float rotationRound = 0f;

        [Header("Debug")]
        public int child = -1;
        public int rotate = -1;

        MUDEntity entity;
        void Start() {
            entity = GetComponentInParent<MUDEntity>();

            if (!entity) {
                Debug.LogError("Can't find entity", this);
                return;
            }

            List<GameObject> tempList = new List<GameObject>();
            if (randomizeChildren) {

                //autopopulate if the object list is empty
                if (objects.Length == 0) {

                    for (int i = 0; i < transform.childCount; i++) {

                        //ignore children that also have randomselector
                        if (transform.GetChild(i).gameObject.GetComponent<RandomSelector>())
                            continue;

                        tempList.Add(transform.GetChild(i).gameObject);
                    }

                    objects = tempList.ToArray();

                }

                if (objects.Length < 1f) {
                    Debug.LogError("No children", this);
                    return;
                }

            }

            if (entity.HasInit) {
                Init();
            } else {
                entity.OnInit += Init;
            }

        }

        void OnDestroy() {
            if (entity)
                entity.OnInit -= Init;
        }
        void Init() {

            if (randomizeChildren) {

                //a position component on our entity is expected to have updated our position at this point, bad assumption? 
                child = (int)MUDHelper.RandomNumber(0, objects.Length, entity, randomType, seed);
                for (int i = 0; i < objects.Length; i++) {
                    objects[i].SetActive(i == child);
                }
            }


            if (rotateY) {
                rotate = (int)MUDHelper.RandomNumber(0, 360, entity, randomType, seed + 1);

                //round to a number (ie. rotationRound of 90 would give it one of four directions)
                if (rotationRound != 0f) {
                    rotate = (int)(Mathf.Round(rotate / rotationRound) * rotationRound);
                }

                transform.Rotate(Vector3.up * rotate);
            }
        }
    }

}