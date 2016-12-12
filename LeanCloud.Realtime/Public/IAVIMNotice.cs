using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime
{

    public interface IAVIMNotice
    {
        AVIMNotice Restore(IDictionary<string, object> estimatedData);
    }
}
