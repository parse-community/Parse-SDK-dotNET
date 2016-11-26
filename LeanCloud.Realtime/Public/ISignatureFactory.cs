using LeanCloud;
using LeanCloud.Core.Internal;
using LeanCloud.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace LeanCloud.Realtime
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISignatureFactory
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        Task<AVIMSignature> CreateConnectSignature(string clientId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conversationId"></param>
        /// <param name="clientId"></param>
        /// <param name="targetIds"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        Task<AVIMSignature> CreateConversationSignature(string conversationId, string clientId, IList<string> targetIds, string action);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="targetIds"></param>
        /// <returns></returns>
        Task<AVIMSignature> CreateStartConversationSignature(string clientId, IList<string> targetIds);


        /// <summary>        
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="conversationId"></param>
        /// <returns></returns>
        Task<AVIMSignature> CreateQueryHistorySignature(string clientId, string conversationId);
    }

    internal class DefaulSiganatureFactory : ISignatureFactory
    {
        Task<AVIMSignature> ISignatureFactory.CreateConnectSignature(string clientId)
        {
            return Task.FromResult<AVIMSignature>(null);
        }

        Task<AVIMSignature> ISignatureFactory.CreateConversationSignature(string conversationId, string clientId, IList<string> targetIds, string action)
        {
            return Task.FromResult<AVIMSignature>(null);
        }

        Task<AVIMSignature> ISignatureFactory.CreateQueryHistorySignature(string clientId, string conversationId)
        {
            return Task.FromResult<AVIMSignature>(null);
        }

        Task<AVIMSignature> ISignatureFactory.CreateStartConversationSignature(string clientId, IList<string> targetIds)
        {
            return Task.FromResult<AVIMSignature>(null);
        }
    }

    internal class LeanEngineSignatureFactory : ISignatureFactory
    {
        public Task<AVIMSignature> CreateConnectSignature(string clientId)
        {
            var data = new Dictionary<string, object>();
            data.Add("client_id", clientId);
            return AVCloud.CallFunctionAsync<IDictionary<string,object>>("sign2", data).OnSuccess(_ => 
            {
                var jsonData = _.Result;
                var s = jsonData["signature"].ToString();
                var n = jsonData["nonce"].ToString();
                var t = long.Parse(jsonData["timestamp"].ToString());
                var signature = new AVIMSignature(s,t,n);
                return signature;
            });
        }

        public Task<AVIMSignature> CreateConversationSignature(string conversationId, string clientId, IList<string> targetIds, string action)
        {
            var data = new Dictionary<string, object>();
            data.Add("client_id", clientId);
            data.Add("conv_id", conversationId);
            data.Add("members", targetIds);
            data.Add("action", action);
            return AVCloud.CallFunctionAsync<IDictionary<string, object>>("sign2", data).OnSuccess(_ =>
            {
                var jsonData = _.Result;
                var s = jsonData["signature"].ToString();
                var n = jsonData["nonce"].ToString();
                var t = long.Parse(jsonData["timestamp"].ToString());
                var signature = new AVIMSignature(s, t, n);
                return signature;
            });
        }

        public Task<AVIMSignature> CreateQueryHistorySignature(string clientId, string conversationId)
        {
            return Task.FromResult<AVIMSignature>(null);
        }

        public Task<AVIMSignature> CreateStartConversationSignature(string clientId, IList<string> targetIds)
        {
            var data = new Dictionary<string, object>();
            data.Add("client_id", clientId);
            data.Add("members", targetIds);
            return AVCloud.CallFunctionAsync<IDictionary<string, object>>("sign2", data).OnSuccess(_ =>
            {
                var jsonData = _.Result;
                var s = jsonData["signature"].ToString();
                var n = jsonData["nonce"].ToString();
                var t = long.Parse(jsonData["timestamp"].ToString());
                var signature = new AVIMSignature(s, t, n);
                return signature;
            });
        }
    }
}
