using LeanCloud;
using LeanCloud.Core.Internal;
using LeanCloud.Storage.Internal;
using LeanCloud.Realtime.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime
{
    /// <summary>
    /// 图像消息
    /// </summary>
    public class AVIMImageMessage : AVIMMessage
    {
        ///// <summary>
        ///// 从本地文件路径构建图像消息
        ///// </summary>
        ///// <returns></returns>
        //public static AVIMImageMessage FromPath()
        //{

        //}

        /// <summary>
        /// 
        /// </summary>
        public AVIMImageMessage()
        {

        }

        internal AVFile fileState;
        /// <summary>
        /// 从外链 Url 构建图像消息
        /// </summary>
        /// <returns></returns>
        public static AVIMImageMessage FromUrl(string url)
        {
            AVIMImageMessage imageMessage = new AVIMImageMessage();
            imageMessage.fileState = new AVFile(string.Empty.Random(8), url);
            return imageMessage;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override Task<AVIMMessage> MakeAsync()
        {
            return fileState.SaveAsync().OnSuccess(_ =>
            {
                this.Attribute(AVIMProtocol.LCTYPE, -2);
                this.Attribute(AVIMProtocol.LCTEXT, "Image");
                var fileData = new Dictionary<string, object>();
                fileData["url"] = fileState.Url.ToString();
                fileData["objId"] = fileState.ObjectId;
                this.Attribute(AVIMProtocol.LCFILE, fileData);
                return this as AVIMMessage;
            });
        }

        /// <summary>
        /// 从服务端的数据生成 AVIMMessage
        /// </summary>
        /// <param name="estimatedData"></param>
        /// <returns></returns>
        public override Task<AVIMMessage> RestoreAsync(IDictionary<string, object> estimatedData)
        {
            throw new NotImplementedException();
        }
    }
}
