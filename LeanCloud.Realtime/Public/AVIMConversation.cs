using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeanCloud.Realtime.Internal;
using LeanCloud;
using LeanCloud.Storage.Internal;
using System.Collections;
using LeanCloud.Core.Internal;

namespace LeanCloud.Realtime
{
    /// <summary>
    /// 对话
    /// </summary>
    public class AVIMConversation : IEnumerable<KeyValuePair<string, object>>
    {

        private DateTime? updatedAt;

        private DateTime? createdAt;

        private DateTime? lastMessageAt;

        private AVObject convState;

        internal readonly Object mutex = new Object();
        //private readonly IDictionary<string, object> estimatedData = new Dictionary<string, object>();

        internal AVIMClient _currentClient;

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>
    .GetEnumerator()
        {
            lock (mutex)
            {
                return ((IEnumerable<KeyValuePair<string, object>>)convState).GetEnumerator();
            }
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            lock (mutex)
            {
                return ((IEnumerable<KeyValuePair<string, object>>)convState).GetEnumerator();
            }
        }
        virtual public object this[string key]
        {
            get
            {
                return convState[key];
            }
            set
            {
                convState[key] = value;
            }
        }
        public ICollection<string> Keys
        {
            get
            {
                lock (mutex)
                {
                    return convState.Keys;
                }
            }
        }

        internal IDictionary<string, object> EncodeAttributes()
        {
            var currentOperations = convState.StartSave();
            var jsonToSave = AVObject.ToJSONObjectForSaving(currentOperations);
            return jsonToSave;
        }
        internal void MergeFromPushServer(IDictionary<string, object> json)
        {
            if (json.Keys.Contains("cdate"))
            {
                createdAt = DateTime.Parse(json["cdate"].ToString());
                updatedAt = DateTime.Parse(json["cdate"].ToString());
                json.Remove("cdate");
            }
            if (json.Keys.Contains("lm"))
            {
                var ts = double.Parse(json["lm"].ToString());
                updatedAt = ts.UnixTimeStampSeconds();
                lastMessageAt = ts.UnixTimeStampSeconds();
                json.Remove("lm");
            }
            if (json.Keys.Contains("c"))
            {
                Creator = json["c"].ToString();
                json.Remove("c");
            }
            if (json.Keys.Contains("m"))
            {
                MemberIds = json["m"] as IList<string>;
                json.Remove("m");
            }
            if (json.Keys.Contains("mu"))
            {
                MuteMemberIds = json["mu"] as IList<string>;
                json.Remove("mu");
            }
            if (json.Keys.Contains("tr"))
            {
                IsTransient = bool.Parse(json["tr"].ToString());
                json.Remove("tr");
            }
            if (json.Keys.Contains("sys"))
            {
                IsSystem = bool.Parse(json["sys"].ToString());
                json.Remove("sys");
            }
            if (json.Keys.Contains("cid"))
            {
                ConversationId = json["cid"].ToString();
                json.Remove("cid");
            }

            if (json.Keys.Contains("name"))
            {
                Name = json["name"].ToString();
                json.Remove("name");
            }
            if (json.Keys.Contains("attr"))
            {

            }
        }

        /// <summary>
        /// 当前的AVIMClient，一个对话理论上只存在一个AVIMClient。
        /// </summary>
        public AVIMClient CurrentClient
        {
            get
            {
                return _currentClient;
            }
            set
            {
                _currentClient = value;
            }
        }
        /// <summary>
        /// 对话的唯一ID
        /// </summary>
        public string ConversationId { get; internal set; }

        /// <summary>
        /// 对话在全局的唯一的名字
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 对话中存在的 Client 的 Id 列表
        /// </summary>
        public IList<string> MemberIds { get; internal set; }

        /// <summary>
        /// 对该对话静音的成员列表
        /// <remarks>
        /// 对该对话设置了静音的成员，将不会收到离线消息的推送。
        /// </remarks>
        /// </summary>
        public IList<string> MuteMemberIds { get; internal set; }

        /// <summary>
        /// 对话的创建者
        /// </summary>
        public string Creator { get; private set; }

        /// <summary>
        /// 是否为聊天室
        /// </summary>
        public bool IsTransient { get; internal set; }

        /// <summary>
        /// 是否系统对话
        /// </summary>
        public bool IsSystem { get; internal set; }

        /// <summary>
        /// 对话创建的时间
        /// </summary>
        public DateTime? CreatedAt
        {
            get
            {
                DateTime? nullable;
                lock (this.mutex)
                {
                    nullable = this.createdAt;
                }
                return nullable;
            }
            private set
            {
                lock (this.mutex)
                {
                    this.createdAt = value;
                }
            }
        }

        /// <summary>
        /// 对话更新的时间
        /// </summary>
        public DateTime? UpdatedAt
        {
            get
            {
                DateTime? nullable;
                lock (this.mutex)
                {
                    nullable = this.updatedAt;
                }
                return nullable;
            }
            private set
            {
                lock (this.mutex)
                {
                    this.updatedAt = value;
                }
            }
        }

        public DateTime? LastMessageAt
        {
            get
            {
                DateTime? nullable;
                lock (this.mutex)
                {
                    nullable = this.lastMessageAt;
                }
                return nullable;
            }
            private set
            {
                lock (this.mutex)
                {
                    this.lastMessageAt = value;
                }
            }
        }

        /// <summary>
        /// 对话的自定义属性
        /// </summary>
        [System.Obsolete("不再推荐使用，请使用 AVIMConversation[key] = value,新版的 ConversationQuery 不再支持查询 Attributes 字段的内部属性。")]
        public IDictionary<string, object> Attributes
        {
            get
            {
                return fetchedAttributes.Merge(pendingAttributes);
            }
            private set
            {
                Attributes = value;
            }
        }
        internal IDictionary<string, object> fetchedAttributes;
        internal IDictionary<string, object> pendingAttributes;

        /// <summary>
        /// 已知 id，在本地构建一个 AVIMConversation 对象
        /// </summary>
        public AVIMConversation(string id)
            : this(null, null, null, null, null, false, false, null)
        {
            this.ConversationId = id;
        }

        /// <summary>
        /// AVIMConversation Build 驱动器
        /// </summary>
        /// <param name="source"></param>
        /// <param name="name"></param>
        /// <param name="members"></param>
        /// <param name="isTransient"></param>
        /// <param name="attributes"></param>
        internal AVIMConversation(AVIMConversation source = null,
            string name = null,
            string creator = null,
            IList<string> members = null,
            IList<string> muteMembers = null,
            bool isTransient = false,
            bool isSystem = false,
            IDictionary<string, object> attributes = null)
        {
            convState = source != null ? source.convState : new AVObject("_Conversation");
            this.Name = source?.Name;
            this.MemberIds = source?.MemberIds;
            this.Creator = source?.Creator;
            this.MuteMemberIds = source?.MuteMemberIds;


            if (!string.IsNullOrEmpty(name))
            {
                this.Name = name;
            }
            if (!string.IsNullOrEmpty(creator))
            {
                this.Creator = creator;
            }
            if (members != null)
            {
                this.MemberIds = members;
            }
            if (muteMembers != null)
            {
                this.MuteMemberIds = muteMembers;
            }

            this.IsTransient = isTransient;
            this.IsSystem = isSystem;
        }

        #region 属性操作
        public Task SaveAsync()
        {
            var cmd = new ConversationCommand()
               .Generate(this);

            var convCmd = cmd.Option("update")
                .AppId(AVClient.CurrentConfiguration.ApplicationId)
                .PeerId(this.CurrentClient.Id);

            return AVIMClient.AVCommandRunner.RunCommandAsync(convCmd);

        }
        #endregion

        ///// <summary>
        ///// 向该对话发送普通的文本消息。
        ///// </summary>
        ///// <param name="textContent">文本消息的内容，一般就是一个不超过5KB的字符串。</param>
        ///// <returns></returns>
        //public Task<Tuple<bool, AVIMTextMessage>> SendTextMessageAsync(AVIMTextMessage textMessage)
        //{
        //    return SendMessageAsync<AVIMTextMessage>(textMessage);
        //}

        /// <summary>
        /// 向该对话发送消息。
        /// </summary>
        /// <param name="avMessage"></param>
        /// <returns></returns>
        public Task<AVIMMessage> SendMessageAsync(AVIMMessage avMessage)
        {
            if (this.CurrentClient == null) throw new Exception("当前对话未指定有效 AVIMClient，无法发送消息。");
            if (this.CurrentClient.State != AVIMClient.Status.Connecting) throw new Exception("未能连接到服务器，无法发送消息。");
            return this.CurrentClient.SendMessageAsync(this, avMessage);
        }

        /// <summary>
        /// 从本地构建一个对话
        /// </summary>
        /// <param name="convId">对话的 objectId</param>
        /// <param name="client"></param>
        /// <returns></returns>
        public static AVIMConversation CreateWithoutData(string convId, AVIMClient client)
        {
            return new AVIMConversation()
            {
                ConversationId = convId,
                CurrentClient = client
            };
        }

        /// <summary>
        /// 设置自定义属性
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        [System.Obsolete("不再推荐使用，请使用 AVIMConversation[key] = value,新版的 ConversationQuery 不再支持查询 attr 字段的内部属性。")]
        public void Attribute(string key, object value)
        {
            if (pendingAttributes == null)
            {
                pendingAttributes = new Dictionary<string, object>();
            }
            pendingAttributes[key] = value;
        }

        #region 成员操作相关接口
        /// <summary>
        /// CurrentClient 主动加入到对话中
        /// <para>签名操作</para>
        /// </summary>
        /// <returns></returns>
        public Task<bool> JoinAsync()
        {
            return AddMembersAsync(CurrentClient.Id);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public Task<bool> AddMembersAsync(string clientId)
        {
            var cmd = new ConversationCommand()
                .ConId(this.ConversationId)
                .Member(clientId)
                .Option("add")
                .AppId(AVClient.CurrentConfiguration.ApplicationId)
                .PeerId(clientId);
            var memberList = new List<string>() { clientId };
            return CurrentClient.AttachSignature(cmd, CurrentClient.SignatureFactory.CreateConversationSignature(this.ConversationId, CurrentClient.Id, memberList, "invite")).OnSuccess(_ =>
            {
                return AVIMClient.AVCommandRunner.RunCommandAsync(cmd).OnSuccess(t =>
                {
                    return t.Result.Item2.ContainsKey("added");
                });
            }).Unwrap();
        }

        #endregion
    }
}
