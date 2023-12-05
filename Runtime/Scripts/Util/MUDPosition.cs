using UnityEngine;
using mud;

//tries to find an x,y,z in a table
//falls back to a 2D position if no Z is found
public class MUDPosition : MUDComponent
{

    public Vector3 position = Vector3.zero;
    protected override void UpdateComponent(MUDTable table, UpdateInfo newInfo) {

        table.RawValue.TryGetValue("x", out object x);
        table.RawValue.TryGetValue("y", out object y);
        table.RawValue.TryGetValue("z", out object z);

        //2D pos, shift Y position to Z value
        if(z == null) {
            position = new Vector3(x == null ? position.x : (float)x, 0f, y == null ? position.y : (float)y);
        } else {
            position = new Vector3(x == null ? position.x : (float)x, y == null ? position.y : (float)y, z == null ? position.z : (float)z);
        }

        transform.position = position;

    }

    void OnDrawGizmosSelected() {

    }
}
