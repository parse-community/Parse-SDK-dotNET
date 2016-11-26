using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime.Internal
{
    internal class RouterState
    {
        public string groupId { get; internal set; }
        public string server { get; internal set; }
        public long ttl { get; internal set; }
        public string secondary { get; internal set; }
    }
}
