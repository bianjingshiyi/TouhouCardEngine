using System;

namespace TouhouCardEngine.Histories
{
    public class GamePropChange : GameChange
    {
        private string name;
        private object beforeValue;
        private object afterValue;
        public GamePropChange(IChangeableGame target, string name, object beforeValue, object afterValue) : base(target)
        {
            this.name = name;
            this.beforeValue = beforeValue is ICloneable cloneableBefore ? cloneableBefore.Clone() : beforeValue;
            this.afterValue = afterValue is ICloneable cloneableAfter ? cloneableAfter.Clone() : afterValue;
        }

        public override void applyFor(IChangeableGame changeable)
        {
            changeable.setProp(name, afterValue is ICloneable cloneable ? cloneable.Clone() : afterValue);
        }

        public override void revertFor(IChangeableGame changeable)
        {
            changeable.setProp(name, beforeValue is ICloneable cloneable ? cloneable.Clone() : beforeValue);
        }
    }
}
