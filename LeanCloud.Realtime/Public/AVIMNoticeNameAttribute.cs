using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime
{
    /// <summary>
    /// 子类化 AVIMNotice 的类型标识
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class AVIMNoticeNameAttribute: Attribute
    {
        /// <summary>
        /// 默认的构造函数
        /// </summary>
        /// <param name="noticeTypeName">类型名称</param>
        public AVIMNoticeNameAttribute(string noticeTypeName)
        {
            NoticeTypeName = noticeTypeName;
        }

        /// <summary>
        /// 通知的类型名称
        /// </summary>
        public string NoticeTypeName { get; private set; }
    }
}
