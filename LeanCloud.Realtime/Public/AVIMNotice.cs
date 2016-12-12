using LeanCloud;
using LeanCloud.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime
{
    /// <summary>
    /// 从服务端接受到的通知
    /// <para>通知泛指消息，对话信息变更（例如加人和被踢等），服务器的 ACK，消息回执等</para>
    /// </summary>
    public class AVIMNotice : EventArgs
    {
        public AVIMNotice()
        {

        }
        public AVIMNotice(IDictionary<string, object> estimatedData)
        {
            this.cmd = estimatedData["cmd"].ToString();
        }
        protected readonly string cmd;

        public static IDictionary<string, IAVIMNotice> noticeFactories = new Dictionary<string, IAVIMNotice>();

        //public static Type GetSubClass(string cmd)
        //{
        //    Tuple<Func<IDictionary<string, object>, AVIMNotice>, Type> result;
        //    if (!noticeFactories.TryGetValue(cmd, out result))
        //    {
        //        return typeof(AVIMNotice);
        //    }
        //    return result.Item2;
        //}
        public static void RegisterInterface<T>(IAVIMNotice restore) 
            where T : IAVIMNotice
        {
            var typeName = GetNoticeTypeName<T>();
            if (typeName == null)
            {
                throw new ArgumentException("No AVIMNoticeName attribute specified on the given subclass.");
            }
            noticeFactories[typeName] = restore;
        }

        public static string GetNoticeTypeName<T>()
        {
            var dnAttribute = typeof(T).GetTypeInfo().GetCustomAttributes(
                typeof(AVIMNoticeNameAttribute), true
            ).FirstOrDefault() as AVIMNoticeNameAttribute;
            if (dnAttribute != null)
            {
                return dnAttribute.NoticeTypeName;
            }
            return null;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [AVIMNoticeName("direct")]
    public class AVIMMessageNotice : AVIMNotice, IAVIMNotice
    {
        public AVIMMessageNotice()
        {

        }
        public AVIMMessageNotice(IDictionary<string, object> estimatedData)
            :base(estimatedData)
        {
            this.cid = estimatedData["cid"].ToString();
            this.fromPeerId = estimatedData["fromPeerId"].ToString();
            this.id = estimatedData["id"].ToString();
            this.appId = estimatedData["appId"].ToString();
            this.peerId = estimatedData["peerId"].ToString();
            this.msg = Json.Parse(estimatedData["msg"].ToString()) as IDictionary<string, object>;
        }

        public readonly string cid;
        public readonly string fromPeerId;
        public readonly IDictionary<string, object> msg;
        public readonly string id;
        public readonly long timestamp;
        public readonly bool transient;
        public readonly string appId;
        public readonly string peerId;
        public readonly bool offline;
        public readonly bool hasMore;

        public AVIMNotice Restore(IDictionary<string, object> estimatedData)
        {
            return new AVIMMessageNotice(estimatedData);
        }
    }
}
