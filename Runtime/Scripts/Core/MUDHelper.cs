using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nethereum.Util;
using Nethereum.ABI;
using Nethereum.Hex.HexConvertors.Extensions;

using System.Linq;
using System.Numerics;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.Model;
using Nethereum.ABI.Util;
using Nethereum.Util;
using Vector3 = UnityEngine.Vector3;
// using Nethereum.JsonRpc.WebSocketStreamingClient;
// using Nethereum.RPC.Eth.Blocks;
// using Nethereum.Web3;
// using Nethereum.Web3.Accounts;

namespace mud.Client
{


    public class MUDHelper : MonoBehaviour
    {

    public static MUDEntity GetMUDEntityFromRadius(Vector3 position, float radius) {
        Collider [] hits = new Collider[10]; 
        int amount = Physics.OverlapSphereNonAlloc(position, radius, hits, LayerMask.NameToLayer("Nothing"), QueryTriggerInteraction.Ignore);
        int selectedItem = -1;
        float minDistance = 999f;
        MUDEntity bestItem = null;
        List<MUDEntity> entities = new List<MUDEntity>();

        for (int i = 0; i < amount; i++)
        {
            MUDEntity checkItem = hits[i].GetComponentInParent<MUDEntity>();

            if (!checkItem)
                continue;

            entities.Add(checkItem);

            float distance = Vector3.Distance(position, hits[i].ClosestPoint(position));
            if (distance < minDistance)
            {
                minDistance = distance;
                selectedItem = i;
                bestItem = checkItem;
            }
        }

        return bestItem;
    }

    public static MUDEntity [] GetEntitiesFromRadius(Vector3 position, float radius)
    {
        Collider [] hits = new Collider[10]; 
        int amount = Physics.OverlapSphereNonAlloc(position, radius, hits, LayerMask.NameToLayer("Nothing"), QueryTriggerInteraction.Ignore);
        int selectedItem = -1;
        float minDistance = 999f;
        MUDEntity bestItem = null;
        List<MUDEntity> entities = new List<MUDEntity>();

        for (int i = 0; i < amount; i++)
        {
            MUDEntity checkItem = hits[i].GetComponentInParent<MUDEntity>();

            if (!checkItem)
                continue;

            entities.Add(checkItem);

            float distance = Vector3.Distance(position, checkItem.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                selectedItem = i;
                bestItem = checkItem;
            }
        }

        // return bestItem;

        return entities.ToArray();

    }

        public static string TruncateHash(string hash)
        {

            if (string.IsNullOrEmpty(hash))
            {
                //Debug.Log("Empty hash");
                return null;
            }

            if (hash.Length > 9)
            {
                if (hash[hash.Length - 1] == ']')
                {
                    return hash.Substring(0, 5) + "..." + hash.Substring(hash.IndexOf('[') - 4);
                }
                else
                {
                    return hash.Substring(0, 5) + "..." + hash.Substring(hash.Length - 4);
                }
            }
            else
            {
                return hash;
            }

        }


        //random.sol implementation
        //function randomCoord(uint minNumber, uint maxNumber, int32 x, int32 y) view returns (uint amount) {
        //      amount = uint(keccak256(abi.encodePacked(x, y, block.timestamp, msg.sender, block.number))) % (maxNumber-minNumber);
        //      amount = amount + minNumber;
        //      return amount;
        // } 

        public enum RandomSource {FromEntity, FromPosition}

        public static float RandomNumber(int min, int max, MUDEntity entity, RandomSource randomType, int seed = 0) {
            if(randomType == RandomSource.FromEntity) {
                return RandomFromKey(min, max, entity.Key, seed);
            } else if(randomType == RandomSource.FromPosition) {
                return RandomFromPosition(min, max, entity.transform.position.x,entity.transform.position.y, seed);
            } else {
                Debug.LogError("Bad");
                return -1;
            }
        }

       
        public static float RandomFrom(int min, int max, int seed = 0, params object[] inputs) {
            float number = GetSha3ABIEncodedNumber(seed,inputs);
            number = number % (max-min);
            number = number + min;
            return (float)number;
        }
        //warning, this DOESNT give same values as randomCoord
        public static float RandomFromPosition(int min, int max, float x, float y, int seed = 0)
        {
            return RandomFrom(min, max, seed, new object[]{x,y});
        }

        public static float RandomFromKey(int min, int max, string entity, int seed = 0)
        {
            return RandomFrom(min, max, seed, entity);
        }

        public static int GetSha3ABIEncodedNumber(int seed = 0, params object[] inputs)
        {
            var abiEncode = new ABIEncode();
            var abiValues = ConvertValuesToABI(inputs);
            abiValues.Add(new ABIValue(new IntType("uint256"), seed));
            var result = abiEncode.GetSha3ABIEncoded(abiValues.ToArray());
            return GetNumber(result);
        }

        static int GetNumber(byte [] bytes) {
            // If the system architecture is little-endian (that is, little end first),
            // reverse the byte array.
            if (System.BitConverter.IsLittleEndian)
                System.Array.Reverse(bytes);

            int i = System.Convert.ToInt32(System.BitConverter.ToUInt16(bytes, 0));
            return i;
        }


        public static string GetSha3ABIEncodedEntity(string entity)
        {
            var abiEncode = new ABIEncode();
            var result = abiEncode.GetSha3ABIEncoded(new ABIValue(new StringType(), entity));
            return result.ToHex(true);
        }


        public static string GetSha3ABIEncodedAddress(string address)
        {
            var abiEncode = new ABIEncode();
            var result = abiEncode.GetSha3ABIEncoded(new ABIValue("address", address));
            return result.ToHex(true);
        }
   
        public static string GetSha3ABIEncoded(params object[] inputs)
        {
            var abiEncode = new ABIEncode();
            var result = abiEncode.GetSha3ABIEncoded(ConvertValuesToABI(inputs).ToArray());
            return result.ToHex(true);
        }


        //we do this because nethereum does not recognize addresses as distinct from strings
        // from https://github.com/Nethereum/Nethereum/blob/master/src/Nethereum.ABI/ABIEncode.cs
        public static List<ABIValue> ConvertValuesToABI(params object[] inputs)
        {
            var abiValues = new List<ABIValue>();

            for (int i = 0; i < inputs.Length; i++)
            {
                var value = inputs[i];
                string stringInput = inputs[i] is string ? inputs[i] as string : null;

                //special cases for address
                if (stringInput != null && stringInput.Length == 20 && stringInput[0] == '0' && stringInput[1] == 'x')
                {
                    abiValues.Add(new ABIValue(new AddressType(), stringInput));
                }
                else
                {
                    //the rest is from Nethereum's ABIEncode implementation
                    if (value is System.Numerics.BigInteger bigIntValue)
                    {
                        if (bigIntValue >= 0)
                        {
                            abiValues.Add(new ABIValue(new IntType("uint256"), bigIntValue));
                        }
                        else
                        {
                            abiValues.Add(new ABIValue(new IntType("int256"), bigIntValue));
                        }
                    }

                    if (value.IsNumber())
                    {
                        var bigInt = System.Numerics.BigInteger.Parse(value.ToString());
                        if (bigInt >= 0)
                        {
                            abiValues.Add(new ABIValue(new IntType("uint256"), value));
                        }
                        else
                        {
                            abiValues.Add(new ABIValue(new IntType("int256"), value));
                        }
                    }

                    if (value is string)
                    {
                        abiValues.Add(new ABIValue(new StringType(), value));
                    }

                    if (value is bool)
                    {
                        abiValues.Add(new ABIValue(new BoolType(), value));
                    }

                    if (value is byte[])
                    {
                        abiValues.Add(new ABIValue(new BytesType(), value));
                    }
                }
            }
            return abiValues;
        }
    }
}