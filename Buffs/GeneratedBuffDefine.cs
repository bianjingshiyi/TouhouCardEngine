using MessagePack;
using System;
using System.Collections.Generic;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    [Serializable]
    public class GeneratedBuffDefine : BuffDefine
    {
        public GeneratedBuffDefine(int id, IEnumerable<PropModifier> modifiers = null, IEnumerable<GeneratedEffect> effects = null) : 
            base(id, modifiers)
        {
            if (effects != null)
                effectList.AddRange(effects);
        }
        public GeneratedBuffDefine(long cardPoolId, int id, IEnumerable<PropModifier> modifiers = null, IEnumerable<GeneratedEffect> effects = null) :
            base(id, modifiers)
        {
            this.cardPoolId = cardPoolId;
            if (effects != null)
                effectList.AddRange(effects);
        }

        public override Effect[] getEffects()
        {
            return effectList.ToArray();
        }

        public List<GeneratedEffect> effectList = new List<GeneratedEffect>();
    }
}