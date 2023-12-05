using UnityEngine;
using mud;
using UnityEngine.PlayerLoop;

//expects a table with a single boolean value
//toggles the entity on/off based on the value
//this will hide all components on the entity
public class MUDVisibility : MUDComponent {
    protected override void UpdateComponent(MUDTable table, UpdateInfo newInfo) {
        Entity.Toggle((bool)(table.RawValue?["value"]));
    }

}
