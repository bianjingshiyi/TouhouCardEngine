using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NitoriNetwork.Common
{
    public interface INetworkingLogger
    {
        void Log(string log);
        void Warning(string log);
        void Error(string log);
    }
}
