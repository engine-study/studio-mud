using UnityEngine;
using mud;
using UnityEngine.PlayerLoop;

//expects a table with a single boolean value
//toggles the entity on/off based on the value
//this will hide all components on the entity
public class MUDVisibility : MUDComponent {

    [Header("Visible")]
    public bool visible;

    protected override void UpdateComponent(MUDTable table, UpdateInfo newInfo) {

        table.RawValue.TryGetValue("value", out object value);
        visible = value == null ? false : (bool)value;
        
        Entity.Toggle(visible);
    }

}
