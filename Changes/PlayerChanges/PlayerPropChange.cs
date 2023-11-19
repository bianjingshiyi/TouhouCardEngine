using System;

namespace TouhouCardEngine.Histories
{
    public class PlayerPropChange : PlayerChange
    {
        private string name;
        private object beforeValue;
        private object afterValue;
        public PlayerPropChange(IChangeablePlayer target, string name, object beforeValue, object afterValue) : base(target)
        {
            this.name = name;
            this.beforeValue = beforeValue is ICloneable cloneableBefore ? cloneableBefore.Clone() : beforeValue;
            this.afterValue = afterValue is ICloneable cloneableAfter ? cloneableAfter.Clone() : afterValue;
        }

        public override void applyFor(IChangeablePlayer changeable)
        {
            changeable.setProp(name, afterValue is ICloneable cloneable ? cloneable.Clone() : afterValue);
        }

        public override void revertFor(IChangeablePlayer changeable)
        {
            changeable.setProp(name, beforeValue is ICloneable cloneable ? cloneable.Clone() : beforeValue);
        }
    }
}
