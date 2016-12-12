using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime
{
    /// <summary>
    /// 消息接口
    /// <para>所有消息必须实现这个接口</para>
    /// </summary>
    public interface IAVIMMessage
    {
        /// <summary>
        /// 异步生成消息体
        /// <para>发送的时候先调用此方法获取最终的消息体，然后再从 WebSocket 发送编码之后消息体</para>
        /// </summary>
        /// <returns></returns>
        Task<AVIMMessage> MakeAsync();

        Task<AVIMMessage> RestoreAsync(IDictionary<string, object> estimatedData);
    }
}
