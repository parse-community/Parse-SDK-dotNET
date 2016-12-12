using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime
{
    /// <summary>
    /// 签名
    /// </summary>
    public class AVIMSignature
    {
        /// <summary>
        /// 经过 SHA1 以及相关操作参数计算出来的加密字符串
        /// </summary>
        public string SignatureContent { get; set; }

        /// <summary>
        /// 服务端时间戳
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// 随机字符串
        /// </summary>
        public string Nonce { get; set; }

        /// <summary>
        /// 构造一个签名
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        /// <param name="n"></param>
        public AVIMSignature(string s,long t,string n)
        {
            this.Nonce = n;
            this.SignatureContent = s;
            this.Timestamp = t;
        }
    }
}
