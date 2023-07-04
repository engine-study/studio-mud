using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace mud.Client {

    public class MUDChunkManager : MonoBehaviour
    {
        [Header("Chunk Loader")]
        public Vector2Int width;
        public Vector2Int height;
        public List<MUDEntity> Entities;
        public MUDNetworkSync sync;
        public static System.Action<string> OnEntityRequest;

        void Awake() {
            sync.OnNetworkInit += LoadChunk;
        }

        void OnDestroy() {
            sync.OnNetworkInit -= LoadChunk;
        }

        void Start() {
            
        }

        void LoadChunk() {

        }

        void ToggleChunk(bool toggle, Vector2Int newWidth, Vector2Int newHeight) {

            for(int x = newWidth.x; x < newWidth.y; x++) {
                for(int y = newHeight.x; y < newHeight.y; y++) {
                    LoadEntity(toggle, x, y);
                }
            }

        }

        void LoadEntity(bool toggle, int x, int y) {

            string key = MUDHelper.GetSha3ABIEncoded( new object[2]{x,y});
            MUDEntity entity = EntityDictionary.FindOrSpawnEntity(key);
            EntityDictionary.SpawnAllComponentsOntoEntity(entity);

            OnEntityRequest?.Invoke(key);
            
        }
    }


}
