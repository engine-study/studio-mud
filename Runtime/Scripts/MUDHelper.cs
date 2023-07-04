using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nethereum.Util;
using Nethereum.ABI;

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

    
        public static string GetSha3ABIEncodedAddress(string address) {
            
            var abiEncode = new ABIEncode();            
            var result = abiEncode.GetSha3ABIEncodedPacked( new ABIValue("address", address));

            return System.Text.Encoding.UTF8.GetString(result, 0, result.Length);
        }
        public static string GetSha3ABIEncodedPacked(params object[] inputs) 
        {
            var abiEncode = new ABIEncode();
            
            var result = abiEncode.GetSha3ABIEncodedPacked(inputs);
                            
            // var result = abiEncode.GetSha3ABIEncodedPacked(
            //                 new ABIValue("string", "Hello!%"), new ABIValue("int8", -23),
            //                 new ABIValue("address", "0x85F43D8a49eeB85d32Cf465507DD71d507100C1d"));

            return System.Text.Encoding.UTF8.GetString(result, 0, result.Length);
        }


        //old method
        // public static string Hash(string input)
        // {
        //     using (var shaAlg = SHA3.Net.Sha3.Sha3256())
        //     {
        //         var hash = shaAlg.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        //         return System.Text.Encoding.UTF8.GetString(hash, 0, hash.Length);
        //     }
        // }

    }
}