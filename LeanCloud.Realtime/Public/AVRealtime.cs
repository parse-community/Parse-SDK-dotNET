using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeanCloud;

namespace LeanCloud.Realtime
{
    public class AVRealtime
    {

        public AVRealtime(Configuration config)
        {
        }

        public Task<AVIMClient> CreateClient(string clientId)
        {
            
            return Task.FromResult(new AVIMClient(clientId));
        }

        public struct Configuration
        {
           
        }
    }
}
