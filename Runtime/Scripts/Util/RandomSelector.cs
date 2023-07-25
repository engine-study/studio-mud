using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace mud.Client {

    public class RandomSelector : MonoBehaviour {

        [Header("Random GameObject")]
        public MUDHelper.RandomSource randomType;
        public int seed = 0;

        [Header("Random GameObject")]
        public bool randomizeChildren = false;
        public GameObject[] objects;

        [Header("Random Rotation")]
        public bool useRotation = false;
        public bool rotateY = true;
        public float rotationRound = 0f;

        [Header("Random Scale")]
        public bool useScale = false;
        public Vector2 range = Vector2.one;

        [Header("Random Position")]
        public bool usePos = false;
        public Vector3 minPos, maxPos;


        [Header("Debug")]
        public int child = -1;
        public int rotate = -1;
        public float scale = -1f;
        public float position = -1f;

        MUDEntity entity;
        void Start() {
            entity = GetComponentInParent<MUDEntity>();

            if (!entity) {
                Debug.LogError("Can't find entity", this);
                return;
            }

            if (randomizeChildren) {
                List<GameObject> tempList = new List<GameObject>();

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


            if (useRotation) {

                rotate = (int)MUDHelper.RandomNumber(0, 360, entity, randomType, seed + 1);

                //round to a number (ie. rotationRound of 90 would give it one of four directions)
                if (rotationRound != 0f) {
                    rotate = (int)(Mathf.Round(rotate / rotationRound) * rotationRound);
                }

                if (rotateY) {
                    transform.Rotate(Vector3.up * rotate);
                } else {
                    transform.Rotate(new Vector3(rotate * 1f, rotate * 2f, rotate * 3f));
                }

            }

            if (useScale) {
                scale = (int)MUDHelper.RandomNumber((int)(range.x * 100f), (int)(range.y * 100f), entity, randomType, seed + 2);

                scale = scale * .01f;
                transform.localScale *= scale;
            }

            if (usePos) {
                position = (int)MUDHelper.RandomNumber(0, 100, entity, randomType, seed + 3);
                position = position * .01f;
                transform.localPosition = Vector3.Lerp(minPos, maxPos, position);
            }
        }
    }

}