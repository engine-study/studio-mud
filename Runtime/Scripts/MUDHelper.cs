using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MUDHelper : MonoBehaviour
{
    public static string TruncateHash(string hash) {
        
        if(string.IsNullOrEmpty(hash)) {
            //Debug.Log("Empty hash");
            return null;
        }

        if(hash.Length > 9) {
            if(hash[hash.Length -1] == ']') {
                return hash.Substring(0,5) + "..." + hash.Substring(hash.IndexOf('[') - 4) ;
            } else {
                return hash.Substring(0,5) + "..." + hash.Substring(hash.Length - 4);
            }
        } else {
            return hash;
        }
        
    }

}
