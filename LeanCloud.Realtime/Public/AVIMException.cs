using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime
{
    /// <summary>
    /// 实时通信的异常
    /// </summary>
    public class AVIMException : Exception
    {
        /// <summary>
        /// 错误代码
        /// </summary>
        public enum ErrorCode
        {
            /// <summary>
            /// Error code indicating that an unknown error or an error unrelated to LeanCloud
            /// occurred.
            /// </summary>
            OtherCause = -1,

            /// <summary>
            /// 服务端错误
            /// </summary>
            FromServer = 4000,

            /// <summary>
            /// websocket 连接非正常关闭，通常见于路由器配置对长连接限制的情况。SDK 会自动重连，无需人工干预。
            /// </summary>
            UnknownError = 1006,

            /// <summary>
            /// 应用不存在或应用禁用了实时通信服务
            /// </summary>
            APP_NOT_AVAILABLE = 4100,

            /// <summary>
            /// Client Id 格式错误，超过 64 个字符。
            /// </summary>
            INVALID_LOGIN = 4103,

            /// <summary>
            /// Session 没有打开就发送消息，或执行其他操作。常见的错误场景是调用 open session 后直接发送消息，正确的用法是在 Session 打开的回调里执行。
            /// </summary>
            SESSION_REQUIRED = 4105,

            /// <summary>
            /// 读超时，服务器端长时间没有收到客户端的数据，切断连接。SDK 包装了心跳包的机制，出现此错误通常是网络问题。SDK 会自动重连，无需人工干预。
            /// </summary>
            READ_TIMEOUT = 4107,

            /// <summary>
            /// 登录超时，连接后长时间没有完成 session open。通常是登录被拒绝等原因，出现此问题可能是使用方式有误，可以 创建工单，由我们技术顾问来给出建议。
            /// </summary>
            LOGIN_TIMEOUT = 4108,

            /// <summary>
            /// 包过长。消息大小超过 5 KB，请缩短消息或者拆分消息。
            /// </summary>
            FRAME_TOO_LONG = 4109,

            /// <summary>
            /// 设置安全域名后，当前登录的域名与安全域名不符合。
            /// </summary>
            INVALID_ORIGIN = 4110,

            /// <summary>
            /// 服务器内部错误，如果反复出现请收集相关线索并 创建工单，我们会尽快解决。
            /// </summary>
            INTERNAL_ERROR = 4200,

            /// <summary>
            /// 通过 API 发送消息超时
            /// </summary>
            SEND_MESSAGE_TIMEOUT = 4201,

            /// <summary>
            /// 上游 API 调用异常，请查看报错信息了解错误详情
            /// </summary>
            CONVERSATION_API_FAILED = 4301,

            /// <summary>
            /// 对话相关操作签名错误
            /// </summary>
            CONVERSATION_SIGNATURE_FAILED = 4302,

            /// <summary>
            /// 发送消息，或邀请等操作对应的对话不存在。
            /// </summary>
            CONVERSATION_NOT_FOUND = 4303,

            /// <summary>
            /// 对话成员已满，不能再添加。
            /// </summary>
            CONVERSATION_FULL = 4304,

            /// <summary>
            /// 对话操作被应用的云引擎 Hook 拒绝
            /// </summary>
            CONVERSATION_REJECTED_BY_APP = 4305,

            /// <summary>
            /// 更新对话操作失败
            /// </summary>
            CONVERSATION_UPDATE_FAILED = 4306,

            /// <summary>
            /// 该对话为只读，不能更新或增删成员。
            /// </summary>
            CONVERSATION_READ_ONLY = 4307,

            /// <summary>
            /// 该对话禁止当前用户发送消息
            /// </summary>
            CONVERSATION_NOT_ALLOWED = 4308,

            /// <summary>
            /// 更新对话的请求被拒绝，当前用户不在对话中
            /// </summary>
            CONVERSATION_UPDATE_REJECT = 4309,

            /// <summary>
            /// 查询对话失败，常见于慢查询导致的超时或受其他慢查询导致的数据库响应慢
            /// </summary>
            CONVERSATION_QUERY_FAILED = 4310,

            /// <summary>
            /// 拉取对话消息记录失败，常见与超时的情况
            /// </summary>
            CONVERSATION_LOG_FAILED = 4311,

            /// <summary>
            /// 拉去对话消息记录被拒绝，当前用户不再对话中
            /// </summary>
            CONVERSATION_LOG_REJECT = 4312,

            /// <summary>
            /// 该功能仅对系统对话有效
            /// </summary>
            SYSTEM_CONVERSATION_REQUIRED = 4313,

            /// <summary>
            /// 发送消息的对话不存在，或当前用户不在对话中
            /// </summary>
            INVALID_MESSAGING_TARGET = 4401,

            /// <summary>
            /// 发送的消息被应用的云引擎 Hook 拒绝
            /// </summary>
            MESSAGE_REJECTED_BY_APP = 4402,

            /// <summary>
            /// 客户端无法通过 WebSocket 发送数据包
            /// </summary>
            CAN_NOT_EXCUTE_COMMAND = 1002,

        }
        /// <summary>
        /// 用户云代码返回的错误码
        /// </summary>
        public int AppCode { get; private set; }


        internal AVIMException(ErrorCode code, string message, Exception cause = null)
            : base(message, cause)
        {
            this.Code = code;
        }

        internal AVIMException(int code, int appCode, string message, Exception cause = null)
            : this((ErrorCode)code, message,cause)
        {
            this.AppCode = appCode;
        }

        /// <summary>
        /// The LeanCloud error code associated with the exception.
        /// </summary>
        public ErrorCode Code { get; private set; }
    }
}
