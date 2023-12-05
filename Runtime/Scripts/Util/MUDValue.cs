using UnityEngine;
using mud;
using System.Numerics;

//tries to find the value of a table with a single value
public class MUDValue : MUDComponent
{
    [Header("Value")]
    public int IntValue;
    public uint UIntValue; 
    public bool BoolValue;
    public BigInteger BigIntValue;

     protected override void UpdateComponent(MUDTable table, UpdateInfo newInfo) {

        TryValue("value", out IntValue);
        TryValue("value", out UIntValue);
        TryValue("value", out BoolValue);
        TryValue("value", out BigIntValue);

    }


}
