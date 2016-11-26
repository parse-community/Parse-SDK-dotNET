using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime.Internal
{
    internal class SessionCommand : AVIMCommand
    {
        public SessionCommand() 
            : base(cmd: "session")
        {

        }

        public SessionCommand(AVIMCommand source)
            :base(source: source)
        {
           
        }

        public SessionCommand UA(string ua)
        {
          return new SessionCommand(this.Argument("ua", ua));
        }

        public SessionCommand Tag(string tag)
        {
            return new SessionCommand(this.Argument("tag", tag));
        }

        public SessionCommand R(int r)
        {
            return new SessionCommand(this.Argument("r", r));
        }
    }
}
