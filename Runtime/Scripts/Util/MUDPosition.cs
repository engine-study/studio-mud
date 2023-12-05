using UnityEngine;
using mud;

//tries to find an x,y,z in a table
//falls back to a 2D position if no Z is found
public class MUDPosition : MUDComponent
{

    [Header("Position")]
    public bool is2D;
    public Vector3 position = Vector3.zero;
    protected override void UpdateComponent(MUDTable table, UpdateInfo newInfo) {

        bool hasX = TryValue("x", out int x);
        bool hasY = TryValue("y", out int y);
        bool hasZ = TryValue("z", out int z);

        //2D pos, shift Y position to Z value
        if(is2D) {
            position = new Vector3(hasX ? x : position.x, 0f, hasY ? y : position.y);
        } else {
            position = new Vector3(hasX ? x : position.x, hasY ? y : position.y, hasZ ? z : position.z);
        }

        transform.position = position;

    }

}
