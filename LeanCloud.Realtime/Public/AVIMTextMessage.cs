using LeanCloud.Realtime.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime
{
    /// <summary>
    /// 纯文本信息
    /// </summary>
    public class AVIMTextMessage : AVIMMessage
    {
        /// <summary>
        /// 构建一个文本信息 <see cref="AVIMTextMessage"/> class.
        /// </summary>
        public AVIMTextMessage()
        {

        }
        /// <summary>
        /// 文本内容
        /// </summary>
        public string TextContent { get; set; }

        /// <summary>
        /// 构造一个纯文本信息
        /// </summary>
        /// <param name="textContent"></param>
        public AVIMTextMessage(string textContent)
            : this()
        {
            TextContent = textContent;
        }

        /// <summary>
        /// 构建文本消息
        /// </summary>
        /// <returns></returns>
        public override Task<AVIMMessage> MakeAsync()
        {
            this.Attribute(AVIMProtocol.LCTYPE, -1);
            this.Attribute(AVIMProtocol.LCTEXT, TextContent);
            return Task.FromResult<AVIMMessage>(this);
        }

        public override Task<AVIMMessage> RestoreAsync(IDictionary<string, object> estimatedData)
        {
            throw new NotImplementedException();
        }
    }
}
