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
// using Nethereum.JsonRpc.WebSocketStreamingClient;
// using Nethereum.RPC.Eth.Blocks;
// using Nethereum.Web3;
// using Nethereum.Web3.Accounts;

namespace mud.Client
{


    public class MUDHelper : MonoBehaviour
    {


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

        public static float RandomNumber(float min, float max, MUDEntity entity, RandomSource randomType) {
            if(randomType == RandomSource.FromEntity) {
                return RandomFromKey(min, max, entity.Key);
            } else if(randomType == RandomSource.FromPosition) {
                return RandomFromPosition(min, max, entity.transform.position.x,entity.transform.position.y);
            } else {
                Debug.LogError("Bad");
                return -1;
            }
        }

        //warning, this DOESNT give same values as randomCoord
        public static float RandomFromPosition(float min, float max, float x, float y)
        {
            float number = float.Parse(GetSha3ABIEncoded(x,y)) % (max-min);
            number = number + min;
            return number;
        }

        public static float RandomFromKey(float min, float max, string entity)
        {
            float number = float.Parse(entity, System.Globalization.NumberStyles.HexNumber) % (max-min);
            number = number + min;
            return number;
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
                if (stringInput != null && stringInput[0] == '0' && stringInput[1] == 'x')
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