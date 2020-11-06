using System;

namespace TouhouCardEngine
{
    public class AGoto : SyncAction
    {
        SyncAction _targetAction;
        public AGoto(SyncAction targetAction)
        {
            if (targetAction == null)
                throw new ArgumentNullException(nameof(targetAction));
            _targetAction = targetAction;
        }
        public override void execute(CardEngine game)
        {
            game.trigger.currentTask.curActionIndex = game.trigger.currentTask.actions.indexOf(_targetAction) - 1;
        }
    }
}