using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime
{
    public class AVIMAudioMessage : AVIMMessage
    {
        public override Task<AVIMMessage> MakeAsync()
        {
            throw new NotImplementedException();
        }

        public override Task<AVIMMessage> RestoreAsync(IDictionary<string, object> estimatedData)
        {
            return Task.FromResult<AVIMMessage>(new AVIMAudioMessage());
        }
    }
}
