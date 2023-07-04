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

        //Using the GitHub master branch of Nethereum (this is not available yet in nuget), you are able to do the following:

        // Automatically guessing the types, as per web3js.

        // var abiEncode = new ABIEncode();
        // var result = abiEncode.GetSha3ABIEncodedPacked(234564535, "0xfff23243".HexToByteArray(), true, -10);
        // Or using specific types:

        // var result = abiEncode.GetSha3ABIEncodedPacked(
        //                     new ABIValue("string", "Hello!%"), new ABIValue("int8", -23),
        //                     new ABIValue("address", "0x85F43D8a49eeB85d32Cf465507DD71d507100C1d"));


        // var result = abiEncode.GetSha3ABIEncodedPacked(
        //                 new ABIValue("string", "Hello!%"), new ABIValue("int8", -23),
        //                 new ABIValue("address", "0x85F43D8a49eeB85d32Cf465507DD71d507100C1d"));


        public static string GetSha3ABIEncodedAddress(string address)
        {

            var abiEncode = new ABIEncode();
            // byte[] bytes = abi
            var result = abiEncode.GetSha3ABIEncoded(new ABIValue("address", address));
            return result.ToHex(true);
            // return System.Text.Encoding.UTF8.GetString(result, 0, result.Length);
        }


        public static string GetSha3ABIEncoded(params object[] inputs)
        {
            var abiEncode = new ABIEncode();
            var result = abiEncode.GetSha3ABIEncoded(ConvertValuesToABI(inputs).ToArray());
            return result.ToHex(true);

            // return System.Text.Encoding.UTF8.GetString(result, 0, result.Length);
        }


        //we do this because nethereum does not recognize addresses as distinct from strings
        public static List<ABIValue> ConvertValuesToABI(params object[] inputs)
        {
            var abiValues = new List<ABIValue>();

            for (int i = 0; i < inputs.Length; i++)
            {
                var value = inputs[i];
                string stringInput = inputs[i] is string ? inputs[i] as string : null;

                if (stringInput != null && stringInput[0] == '0' && stringInput[1] == 'x')
                {
                    abiValues.Add(new ABIValue(new AddressType(), stringInput));
                }
                else
                {
                    // from https://github.com/Nethereum/Nethereum/blob/master/src/Nethereum.ABI/ABIEncode.cs

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