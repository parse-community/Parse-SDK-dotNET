using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime.Internal
{
    internal class MessageCommand: AVIMCommand
    {
        public MessageCommand() 
            : base(cmd: "direct")
        {

        }

        public MessageCommand(AVIMCommand source)
            :base(source: source)
        {

        }

        public MessageCommand ConvId(string convId)
        {
            return new MessageCommand(this.Argument("cid", convId));
        }

        public MessageCommand Receipt(bool receipt)
        {
            return new MessageCommand(this.Argument("r", receipt));
        }

        public MessageCommand Transient(bool transient)
        {
            return new MessageCommand(this.Argument("transient", transient));
        }
        public MessageCommand Distinct(string token)
        {
            return new MessageCommand(this.Argument("dt", token));
        }
        public MessageCommand Message(string msg)
        {
            return new MessageCommand(this.Argument("msg", msg));
        }

    }
}
