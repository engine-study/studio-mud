using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace mud.Client
{
    public class ExampleComponentManager : MUDTableManager
    {

        protected override void Subscribe(mud.Unity.NetworkManager nm)
        {

            // var InsertSub = ObservableExtensions.Subscribe(PositionTable.OnRecordInsert().ObserveOnMainThread(), OnInsertRecord);
            // _disposers.Add(InsertSub);

            // var UpdateSub = ObservableExtensions.Subscribe(PositionTable.OnRecordUpdate().ObserveOnMainThread(), OnUpdateRecord);
            // _disposers.Add(UpdateSub);
        }

        protected override IMudTable RecordUpdateToTable(RecordUpdate tableUpdate)
        {

            // PositionTableUpdate update = tableUpdate as PositionTableUpdate;

            // var newValue = update.TypedValue.Item1;
            // var oldValue = update.TypedValue.Item2;

            // if (newValue == null)
            // {
            //     Debug.LogError("No currentValue");
            //     return null;
            // }

            // return newValue;
            return null;
            
        }

    }
}
