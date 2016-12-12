// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Core.Internal;

namespace LeanCloud
{
    /// <summary>
    /// The AVCloud class provides methods for interacting with LeanCloud Cloud Functions.
    /// </summary>
    /// <example>
    /// For example, this sample code calls the
    /// "validateGame" Cloud Function and calls processResponse if the call succeeded
    /// and handleError if it failed.
    ///
    /// <code>
    /// var result =
    ///     await AVCloud.CallFunctionAsync&lt;IDictionary&lt;string, object&gt;&gt;("validateGame", parameters);
    /// </code>
    /// </example>
    public static class AVCloud
    {
        internal static IAVCloudCodeController CloudCodeController
        {
            get
            {
                return AVPlugins.Instance.CloudCodeController;
            }
        }

        /// <summary>
        /// Calls a cloud function.
        /// </summary>
        /// <typeparam name="T">The type of data you will receive from the cloud function. This
        /// can be an IDictionary, string, IList, AVObject, or any other type supported by
        /// AVObject.</typeparam>
        /// <param name="name">The cloud function to call.</param>
        /// <param name="parameters">The parameters to send to the cloud function. This
        /// dictionary can contain anything that could be passed into a AVObject except for
        /// ParseObjects themselves.</param>
        /// <returns>The result of the cloud call.</returns>
        public static Task<T> CallFunctionAsync<T>(String name, IDictionary<string, object> parameters)
        {
            return CallFunctionAsync<T>(name, parameters, CancellationToken.None);
        }

        /// <summary>
        /// Calls a cloud function.
        /// </summary>
        /// <typeparam name="T">The type of data you will receive from the cloud function. This
        /// can be an IDictionary, string, IList, AVObject, or any other type supported by
        /// AVObject.</typeparam>
        /// <param name="name">The cloud function to call.</param>
        /// <param name="parameters">The parameters to send to the cloud function. This
        /// dictionary can contain anything that could be passed into a AVObject except for
        /// ParseObjects themselves.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the cloud call.</returns>
        public static Task<T> CallFunctionAsync<T>(String name,
            IDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            return CloudCodeController.CallFunctionAsync<T>(name,
                parameters,
                AVUser.CurrentSessionToken,
                cancellationToken);
        }


        public static Task<T> RPCFunctionAsync<T>(String name, IDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            return CloudCodeController.RPCFunction<T>(name,
                parameters,
                AVUser.CurrentSessionToken,
                cancellationToken);
        }

        /// <summary>
        /// 获取 LeanCloud 服务器的时间
        /// <remarks>
        /// 如果获取失败，将返回 DateTime.MinValue
        /// </remarks>
        /// </summary>
        /// <returns>服务器的时间</returns>
        public static Task<DateTime> GetServerDateTimeAsync()
        {
            var command = new AVCommand(relativeUri: "date",
                method: "GET",
                sessionToken: null,
                data: null);
            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).ContinueWith(t =>
            {
                DateTime rtn = DateTime.MinValue;
                if (AVClient.IsSuccessStatusCode(t.Result.Item1))
                {
                    var date = AVDecoder.Instance.Decode(t.Result.Item2);
                    if (date != null)
                    {
                        if (date is DateTime)
                        {
                            rtn = (DateTime)date;
                        }
                    }
                }
                return rtn;
            });
        }

        /// <summary>
        /// 请求短信认证。
        /// </summary>
        /// <param name="mobilePhoneNumber">手机号。</param>
        /// <param name="name">应用名称。</param>
        /// <param name="op">进行的操作名称。</param>
        /// <param name="ttl">验证码失效时间。</param>
        /// <returns></returns>
        public static Task<bool> RequestSMSCodeAsync(string mobilePhoneNumber, string name, string op, int ttl)
        {
            return AVCloud.RequestSMSCodeAsync(mobilePhoneNumber, name, op, ttl, CancellationToken.None);
        }


        /// <summary>
        /// 请求发送验证码。
        /// </summary>
        /// <returns>是否发送成功。</returns>
        /// <param name="mobilePhoneNumber">手机号。</param>
        /// <param name="name">应用名称。</param>
        /// <param name="op">进行的操作名称。</param>
        /// <param name="ttl">验证码失效时间。</param>
        /// <param name="cancellationToken">Cancellation token。</param>
        public static Task<bool> RequestSMSCodeAsync(string mobilePhoneNumber, string name, string op, int ttl, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(mobilePhoneNumber))
            {
                throw new AVException(AVException.ErrorCode.MobilePhoneInvalid, "Moblie Phone number is invalid.", null);
            }

            Dictionary<string, object> strs = new Dictionary<string, object>()
            {
                { "mobilePhoneNumber", mobilePhoneNumber },
            };
            if (!string.IsNullOrEmpty(name))
            {
                strs.Add("name", name);
            }
            if (!string.IsNullOrEmpty(op))
            {
                strs.Add("op", op);
            }
            if (ttl > 0)
            {
                strs.Add("ttl", ttl);
            }
            var command = new AVCommand("requestSmsCode",
               method: "POST",
               sessionToken: null,
               data: strs);
            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).ContinueWith(t =>
            {
                return AVClient.IsSuccessStatusCode(t.Result.Item1);
            });
        }

        /// <summary>
        /// 请求发送验证码。
        /// </summary>
        /// <returns>是否发送成功。</returns>
        /// <param name="mobilePhoneNumber">手机号。</param>
        public static Task<bool> RequestSMSCodeAsync(string mobilePhoneNumber)
        {
            return AVCloud.RequestSMSCodeAsync(mobilePhoneNumber, CancellationToken.None);
        }


        /// <summary>
        /// 请求发送验证码。
        /// </summary>
        /// <returns>是否发送成功。</returns>
        /// <param name="mobilePhoneNumber">手机号。</param>
        public static Task<bool> RequestSMSCodeAsync(string mobilePhoneNumber, CancellationToken cancellationToken)
        {
            return AVCloud.RequestSMSCodeAsync(mobilePhoneNumber, null, null, 0, cancellationToken);
        }

        /// <summary>
        /// 
        // 发送手机短信，并指定模板以及传入模板所需的参数。
        //
        // Exceptions:
        //   AVOSCloud.AVException:
        //   手机号为空。
        ///
        /// <param name="mobilePhoneNumber"></param>
        /// <param name="template"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        public static Task<bool> RequestSMSCodeAsync(string mobilePhoneNumber, string template, IDictionary<string, object> env)
        {

            if (string.IsNullOrEmpty(mobilePhoneNumber))
            {
                throw new AVException(AVException.ErrorCode.MobilePhoneInvalid, "Moblie Phone number is invalid.", null);
            }
            Dictionary<string, object> strs = new Dictionary<string, object>()
            {
                { "mobilePhoneNumber", mobilePhoneNumber },
            };
            strs.Add("template", template);
            foreach (var key in env.Keys)
            {
                strs.Add(key, env[key]);
            }
            var command = new AVCommand("requestSmsCode",
                method: "POST",
                sessionToken: null,
                data: strs);
            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).ContinueWith(t =>
            {
                return AVClient.IsSuccessStatusCode(t.Result.Item1);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mobilePhoneNumber"></param>
        /// <returns></returns>
        public static Task<bool> RequestVoiceCodeAsync(string mobilePhoneNumber)
        {
            if (string.IsNullOrEmpty(mobilePhoneNumber))
            {
                throw new AVException(AVException.ErrorCode.MobilePhoneInvalid, "Moblie Phone number is invalid.", null);
            }
            Dictionary<string, object> strs = new Dictionary<string, object>()
            {
                { "mobilePhoneNumber", mobilePhoneNumber },
                { "smsType", "voice" },
                { "IDD","+86" }
            };

            var command = new AVCommand("requestSmsCode",
                method: "POST",
                sessionToken: null,
                data: strs);

            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).ContinueWith(t =>
            {
                return AVClient.IsSuccessStatusCode(t.Result.Item1);
            });
        }

        /// <summary>
        /// 验证是否是有效短信验证码。
        /// </summary>
        /// <returns>是否验证通过。</returns>
        /// <param name="mobilePhoneNumber">手机号</param>
        /// <param name="code">验证码。</param>
        public static Task<bool> VerifySmsCodeAsync(string code, string mobilePhoneNumber)
        {
            return AVCloud.VerifySmsCodeAsync(code, mobilePhoneNumber, CancellationToken.None);
        }

        /// <summary>
        /// 验证是否是有效短信验证码。
        /// </summary>
        /// <returns>是否验证通过。</returns>
        /// <param name="code">验证码。</param>
        /// <param name="mobilePhoneNumber">手机号</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static Task<bool> VerifySmsCodeAsync(string code, string mobilePhoneNumber, CancellationToken cancellationToken)
        {
            var command = new AVCommand("verifySmsCode/" + code.Trim() + "?mobilePhoneNumber=" + mobilePhoneNumber.Trim(),
                method: "POST",
                sessionToken: null,
                data: null);

            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).ContinueWith(t =>
            {
                return AVClient.IsSuccessStatusCode(t.Result.Item1);
            });
        }

    }
}
