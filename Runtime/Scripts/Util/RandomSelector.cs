using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace mud.Client {

    public class RandomSelector : MonoBehaviour {

        public GameObject ActiveChild {get{return activeChild;}}
        [Header("Random GameObject")]
        public MUDHelper.RandomSource randomType;
        public int seed = 0;

        [Header("Random GameObject")]
        [SerializeField] private bool randomizeChildren = false;
        [SerializeField] private GameObject[] objects;
        [SerializeField] private GameObject activeChild;

        [Header("Random Rotation")]
        [SerializeField] private bool useRotation = false;
        [SerializeField] private bool rotateY = true;
        [SerializeField] private float rotationRound = 0f;

        [Header("Random Scale")]
        [SerializeField] private bool useScale = false;
        [SerializeField] private Vector2 range = Vector2.one;

        [Header("Random Position")]
        [SerializeField] private bool usePos = false;
        [SerializeField] private Vector3 minPos, maxPos;


        [Header("Debug")]
        [SerializeField] private int child = -1;
        [SerializeField] private int rotate = -1;
        [SerializeField] private float scale = -1f;
        [SerializeField] private float position = -1f;

        MUDComponent component;
        bool init = false;
        void Awake() {

            if (init) { return; }

            component = GetComponentInParent<MUDComponent>();

            if (!component) {
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

            if (component.HasInit) {
                Init();
            } else {
                component.OnLoaded += Init;
            }

        }

        void OnDestroy() {
            if (component)
                component.OnAwake -= Init;
        }
        void Init() {

            if (randomizeChildren) {

                //a position component on our entity is expected to have updated our position at this point, bad assumption? 
                child = (int)MUDHelper.RandomNumber(0, objects.Length, component.Entity, randomType, seed);
                for (int i = 0; i < objects.Length; i++) {
                    objects[i].SetActive(i == child);
                }

                activeChild = objects[child];
            }


            if (useRotation) {

                rotate = (int)MUDHelper.RandomNumber(0, 360, component.Entity, randomType, seed + 1);

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
                scale = (int)MUDHelper.RandomNumber((int)(range.x * 100f), (int)(range.y * 100f), component.Entity, randomType, seed + 2);

                scale = scale * .01f;
                transform.localScale *= scale;
            }

            if (usePos) {
                position = (int)MUDHelper.RandomNumber(0, 100, component.Entity, randomType, seed + 3);
                position = position * .01f;
                transform.localPosition = Vector3.Lerp(minPos, maxPos, position);
            }

            init = true;
        }
    }

}