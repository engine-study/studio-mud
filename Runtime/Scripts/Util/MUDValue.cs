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

        table.RawValue.TryGetValue("value", out object value);
        table.RawValue.TryGetValue("y", out object y);
        table.RawValue.TryGetValue("z", out object z);

        if(value == null) {
            return;
        }

        IntValue = (int)value;
        UIntValue = (uint)value;
        BoolValue = (bool)value;
        BigIntValue = (BigInteger)value;

    }


}
