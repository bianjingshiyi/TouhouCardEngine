﻿using System.Collections.Generic;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class GeneratedEffectData : ActionDefine
    {
        public GeneratedEffectData() : base(defName)
        {
            _inputs = new PortDefine[0];
            _consts = new PortDefine[0];
            _outputs = new PortDefine[2]
            {
                PortDefine.Control(enableActionName, "EnableAction"),
                PortDefine.Control(disableActionName, "DisableAction")
            };
            hideDocument = true;
            disableMenu = true;
            disableCreate = true;
        }
        public override Task<ControlOutput> run(Flow flow, Node node)
        {
            return Task.FromResult<ControlOutput>(null);
        }

        private PortDefine[] _inputs;
        private PortDefine[] _consts;
        private PortDefine[] _outputs;
        public const string enableActionName = "enableAction";
        public const string disableActionName = "disableAction";
        public const string defName = "GeneratedEffectData";
        public override IEnumerable<PortDefine> inputDefines => _inputs;
        public override IEnumerable<PortDefine> constDefines => _consts;
        public override IEnumerable<PortDefine> outputDefines => _outputs;
    }
}