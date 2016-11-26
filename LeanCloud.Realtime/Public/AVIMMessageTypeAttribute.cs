using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class AVIMMessageTypeAttribute: Attribute
    {
        public AVIMMessageTypeAttribute(int typeEnum,string name)
        {
            this.Name = name;
            this.TypeEnum = typeEnum;
        }

        public int TypeEnum { get; set; }

        public string Name { get; set; }
    }
}
