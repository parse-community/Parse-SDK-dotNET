using LeanCloud;
using LeanCloud.Core.Internal;
using LeanCloud.Storage.Internal;
using LeanCloud.Realtime.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Realtime
{

    /// <summary>
    /// 代表一个实时通信的终端用户
    /// </summary>
    public class AVIMClient
    {
        /// <summary>
        /// 
        /// </summary>
        public enum Status : int
        {
            /// <summary>
            /// 未初始化
            /// </summary>
            None = -1,

            /// <summary>
            /// 正在连接
            /// </summary>
            Connecting = 0,

            /// <summary>
            /// 已连接
            /// </summary>
            Online = 1,

            /// <summary>
            /// 连接已断开
            /// </summary>
            Offline = 2
        }

        private AVIMClient.Status state;
        public AVIMClient.Status State
        {
            get
            {
                return state;
            }
            private set
            {
                state = value;
            }
        }

        public string Tag
        {
            get;
            private set;
        }
        /// <summary>
        /// 客户端的标识,在一个 Application 内唯一。
        /// </summary>
        private readonly string clientId;

        public string Id
        {
            get { return clientId; }
        }

        private ISignatureFactory _signatureFactory;

        /// <summary>
        /// 签名接口
        /// </summary>
        public ISignatureFactory SignatureFactory
        {
            get
            {
                if (_signatureFactory == null)
                {
                    if (useLeanEngineSignaturFactory)
                    {
                        _signatureFactory = new LeanEngineSignatureFactory();
                    }
                    else
                    {
                        _signatureFactory = new DefaulSiganatureFactory();
                    }
                }
                return _signatureFactory;
            }
            set
            {
                _signatureFactory = value;
            }
        }
        private bool useLeanEngineSignaturFactory;
        /// <summary>
        /// 启用 LeanEngine 云函数签名
        /// </summary>
        public void UseLeanEngineSignatureFactory()
        {
            useLeanEngineSignaturFactory = true;
        }

        //private static readonly IAVIMPlatformHooks platformHooks;

        //internal static IAVIMPlatformHooks PlatformHooks { get { return platformHooks; } }

        private IWebSocketClient websocketClient
        {
            get
            {
                return AVIMCorePlugins.Instance.WebSocketController;
            }
        }

        private static readonly IAVIMCommandRunner commandRunner;

        internal static IAVIMCommandRunner AVCommandRunner { get { return commandRunner; } }

        internal static IAVRouterController RouterController
        {
            get
            {
                return AVIMCorePlugins.Instance.RouterController;
            }
        }

        private EventHandler<AVIMNotice> m_OnNoticeReceived;
        /// <summary>
        /// 接收到服务器的消息时激发的事件
        /// </summary>
        public event EventHandler<AVIMNotice> OnNoticeReceived
        {
            add
            {
                m_OnNoticeReceived += value;
            }
            remove
            {
                m_OnNoticeReceived -= value;
            }
        }

        private EventHandler<AVIMMessage> m_OnMessageReceieved;
        public event EventHandler<AVIMMessage> OnMessageReceieved
        {
            add
            {
                m_OnMessageReceieved += value;
            }
            remove
            {
                m_OnMessageReceieved -= value;
            }
        }

        private EventHandler<string> m_OnDisconnected;
        public event EventHandler<string> OnDisconnected
        {
            add
            {
                m_OnDisconnected += value;
            }
            remove
            {
                m_OnDisconnected -= value;
            }
        }


        IDictionary<string, Action<AVIMNotice>> noticeHandlers = new Dictionary<string, Action<AVIMNotice>>();

        IDictionary<int, Action<AVIMMessage>> messageHandlers = new Dictionary<int, Action<AVIMMessage>>();

        IDictionary<int, IAVIMMessage> adpaters = new Dictionary<int, IAVIMMessage>();
        /// <summary>
        /// 注册服务端指令接受时的代理
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="hanlder"></param>
        internal void RegisterNotice<T>(Action<T> hanlder)
            where T : AVIMNotice
        {
            var typeName = AVIMNotice.GetNoticeTypeName<T>();
            Action<AVIMNotice> b = (target) =>
            {
                hanlder((T)target);
            };
            noticeHandlers[typeName] = b;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="invoker"></param>
        public void RegisterMessage<T>(Action<IAVIMMessage> invoker)
             where T : AVIMMessage, new()
        {
            RegisterMessage<T>(invoker, new T());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="invoker"></param>
        /// <param name="adpater"></param>
        public void RegisterMessage<T>(Action<IAVIMMessage> invoker, IAVIMMessage adpater)
            where T : AVIMMessage
        {
            int typeEnum = AVIMMessage.GetMessageType<T>();
            if (typeEnum < 0) return;
            messageHandlers[typeEnum] = invoker;
            adpaters[typeEnum] = adpater;
        }

        void RegisterNotices()
        {
            RegisterNotice<AVIMMessageNotice>((notice) =>
            {
                int adpaterKey = int.Parse(notice.msg.Grab(AVIMProtocol.LCTYPE).ToString());
                if (!adpaters.ContainsKey(adpaterKey)) return;
                var adpater = adpaters[adpaterKey];
                adpater.RestoreAsync(notice.msg).OnSuccess(_ =>
                {
                    var handler = messageHandlers[adpaterKey];
                    handler(_.Result);
                });
            });
        }
        private void WebsocketClient_OnMessage(string obj)
        {
            var estimatedData = Json.Parse(obj) as IDictionary<string, object>;
            var cmd = estimatedData["cmd"].ToString();
            if (!AVIMNotice.noticeFactories.Keys.Contains(cmd)) return;
            var registerNoticeInterface = AVIMNotice.noticeFactories[cmd];
            var notice = registerNoticeInterface.Restore(estimatedData);
            if (noticeHandlers == null) return;
            if (!noticeHandlers.Keys.Contains(cmd)) return;

            var handler = noticeHandlers[cmd];
            handler(notice);
        }

        /// <summary>
        /// 创建 AVIMClient 对象
        /// </summary>
        /// <param name="clientId"></param>
        public AVIMClient(string clientId)
            : this(clientId, null)
        {

        }

        /// <summary>
        /// 创建 AVIMClient 对象
        /// </summary>
        /// <param name="clientId"></param>
        public AVIMClient(string clientId, string tag)
        {
            this.clientId = clientId;
            Tag = tag ?? tag;
        }

        /// <summary>
        /// 创建与 Realtime 云端的长连接
        /// </summary>
        /// <returns></returns>
        public Task ConnectAsync()
        {
            return ConnectAsync(CancellationToken.None);
        }

        /// <summary>
        /// 创建与 Realtime 云端的长连接
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task ConnectAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(clientId)) throw new Exception("当前 ClientId 为空，无法登录服务器。");
            state = Status.Connecting;
            return RouterController.GetAsync(cancellationToken).OnSuccess(_ =>
            {
                return OpenAsync(_.Result.server);
            }).Unwrap().OnSuccess(t =>
            {
                var cmd = new SessionCommand()
                .UA(AVRealtime.VersionString)
                .Option("open")
                .AppId(AVClient.CurrentConfiguration.ApplicationId)
                .PeerId(clientId);

                return AttachSignature(cmd, this.SignatureFactory.CreateConnectSignature(this.clientId)).OnSuccess(_ =>
                {
                    return AVIMClient.AVCommandRunner.RunCommandAsync(cmd);
                }).Unwrap();

            }).Unwrap().OnSuccess(s =>
            {
                state = Status.Online;
                var response = s.Result.Item2;
                websocketClient.OnMessage += WebsocketClient_OnMessage;
                websocketClient.OnClosed += WebsocketClient_OnClosed;
                websocketClient.OnError += WebsocketClient_OnError;
                RegisterNotices();
            });
        }

        private void WebsocketClient_OnError(string obj)
        {
            m_OnDisconnected?.Invoke(this, obj);
        }

        private void WebsocketClient_OnClosed()
        {
            state = Status.Offline;
        }

        /// <summary>
        /// 打开 WebSocket 链接
        /// </summary>
        /// <param name="wss"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal Task OpenAsync(string wss, CancellationToken cancellationToken = default(CancellationToken))
        {
            var tcs = new TaskCompletionSource<bool>();
            websocketClient.Open(wss);
            Action onOpend = null;
            onOpend = (() =>
            {
                websocketClient.OnOpened -= onOpend;
                tcs.SetResult(true);
            });
            websocketClient.OnOpened += onOpend;

            Action<string> onError = null;
            onError = ((reason) =>
            {
                websocketClient.OnError -= onError;
                tcs.SetResult(false);
                tcs.TrySetException(new AVIMException(AVIMException.ErrorCode.FromServer, "try to open websocket at " + wss + "failed.The reason is " + reason, null));
            });

            websocketClient.OnError += onError;
            return tcs.Task;
        }

        /// <summary>
        /// 创建对话
        /// </summary>
        /// <param name="conversation">对话</param>
        /// <param name="isUnique">是否创建唯一对话，当 isUnique 为 true 时，如果当前已经有相同成员的对话存在则返回该对话，否则会创建新的对话。该值默认为 false。</param>
        /// <returns></returns>
        public Task<AVIMConversation> CreateConversationAsync(AVIMConversation conversation, bool isUnique = true)
        {
            var cmd = new ConversationCommand()
                .Generate(conversation)
                .Unique(isUnique);

            var convCmd = cmd.Option("start")
                .AppId(AVClient.CurrentConfiguration.ApplicationId)
                .PeerId(clientId);

            return AttachSignature(convCmd, this.SignatureFactory.CreateStartConversationSignature(this.clientId, conversation.MemberIds)).OnSuccess(_ =>
             {
                 return AVIMClient.AVCommandRunner.RunCommandAsync(convCmd).OnSuccess(t =>
                 {
                     var result = t.Result;
                     if (result.Item1 < 1)
                     {
                         conversation.MemberIds.Add(Id);
                         conversation = new AVIMConversation(source: conversation, creator: Id);
                         conversation.MergeFromPushServer(result.Item2);
                     }

                     return conversation;
                 });
             }).Unwrap();

        }

        /// <summary>
        /// 创建与目标成员的对话
        /// </summary>
        /// <param name="members">目标成员</param>
        /// <param name="isUnique">是否是唯一对话</param>
        /// <returns></returns>
        public Task<AVIMConversation> CreateConversationAsync(IList<string> members = null, bool isUnique = true, IDictionary<string, object> options = null)
        {
            var conversation = new AVIMConversation(members: members);
            foreach (var key in options?.Keys)
            {
                conversation[key] = options[key];
            }

            return CreateConversationAsync(conversation, isUnique);
        }

        /// <summary>
        /// 创建与目标成员的对话
        /// </summary>
        /// <param name="member">目标成员</param>
        /// <param name="isUnique">是否是唯一对话</param>
        /// <returns></returns>
        public Task<AVIMConversation> CreateConversationAsync(string member = "", bool isUnique = true, IDictionary<string, object> options = null)
        {
            var members = new List<string>() { member };

            return CreateConversationAsync(members, isUnique, options);
        }

        /// <summary>
        /// 创建聊天室（即：暂态对话）
        /// </summary>
        /// <param name="conversationName">聊天室名称</param>
        /// <returns></returns>
        public Task<AVIMConversation> CreateChatRoomAsync(string conversationName)
        {
            var conversation = new AVIMConversation() { Name = conversationName, IsTransient = false };
            return CreateConversationAsync(conversation);
        }

        public Task<AVIMConversation> GetConversation(string id, bool noCache)
        {
            if (!noCache) return Task.FromResult(new AVIMConversation() { ConversationId = id });
            else
            {
                return Task.FromResult(new AVIMConversation() { ConversationId = id });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conversation"></param>
        /// <param name="avMessage"></param>
        /// <returns></returns>
        public Task<AVIMMessage> SendMessageAsync(AVIMConversation conversation, AVIMMessage avMessage)
        {
            var cmd = new MessageCommand()
                .Message(avMessage.EncodeJsonString())
                .ConvId(conversation.ConversationId)
                .Receipt(avMessage.Receipt)
                .Transient(avMessage.Transient)
                .AppId(AVClient.CurrentConfiguration.ApplicationId)
                .PeerId(this.clientId);

            return AVIMClient.AVCommandRunner.RunCommandAsync(cmd).ContinueWith<AVIMMessage>(t =>
            {
                if (t.IsFaulted)
                {
                    throw t.Exception;
                }
                else
                {
                    var response = t.Result.Item2;
                    avMessage.Id = response["uid"].ToString();
                    avMessage.ServerTimestamp = long.Parse(response["t"].ToString());

                    return avMessage;
                }
            });
        }

        internal Task<AVIMCommand> AttachSignature(AVIMCommand command, Task<AVIMSignature> SignatureTask)
        {
            var tcs = new TaskCompletionSource<AVIMCommand>();
            if (SignatureTask == null)
            {
                tcs.SetResult(command);
                return tcs.Task;
            }
            return SignatureTask.OnSuccess(_ =>
            {
                if (_.Result != null)
                {
                    var signature = _.Result;
                    command.Argument("t", signature.Timestamp);
                    command.Argument("n", signature.Nonce);
                    command.Argument("s", signature.SignatureContent);
                }
                return command;
            });
        }

        static AVIMClient()
        {
            commandRunner = new AVIMCommandRunner(AVIMCorePlugins.Instance.WebSocketController);

            AVIMNotice.RegisterInterface<AVIMMessageNotice>(new AVIMMessageNotice());
        }

    }
}
