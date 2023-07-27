using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace mud.Client {

    public class ExampleComponent : MUDComponent
    {
        protected override IMudTable GetTable() {throw new System.NotImplementedException();}

        protected override void UpdateComponent(Client.IMudTable table, UpdateInfo newInfo) {
            throw new System.NotImplementedException();
        }
    }
}
