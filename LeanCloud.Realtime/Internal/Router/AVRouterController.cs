using LeanCloud.Core.Internal;
using LeanCloud.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Realtime.Internal
{
    internal class AVRouterController : IAVRouterController
    {
        const string routerUrl = "http://router.g0.push.leancloud.cn/v1/route?appId={0}&secure=1";
        const string routerKey = "RouterState";
        public Task<RouterState> GetAsync(CancellationToken cancellationToken)
        {
            return readCache(cancellationToken).OnSuccess(_ =>
             {
                 var task = Task.FromResult<RouterState>(_.Result);

                 if (_.Result == null)
                 {
                     task = fromCloud(cancellationToken);
                 }

                 return task;
             }).Unwrap();
        }

        Task<RouterState> readCache(CancellationToken cancellationToken)
        {
            try
            {
                return AVPlugins.Instance.StorageController.LoadAsync().OnSuccess(_ =>
                 {
                     var currentCache = _.Result;
                     object routeCacheStr = null;
                     if (currentCache.TryGetValue(routerKey, out routeCacheStr))
                     {
                         var routeCache = Json.Parse(routeCacheStr.ToString()) as IDictionary<string, object>;
                         var routerState = new RouterState()
                         {
                             groupId = routeCache["groupId"] as string,
                             server = routeCache["server"] as string,
                             secondary = routeCache["secondary"] as string,
                             ttl = long.Parse((routeCache["ttl"].ToString())),
                         };
                         return routerState;
                     }
                     return null;
                 });
            }
            catch
            {
                return Task.FromResult<RouterState>(null);
            }
        }
        Task<RouterState> fromCloud(CancellationToken cancellationToken)
        {
            string url = string.Format(routerUrl, AVClient.CurrentConfiguration.ApplicationId);
            return AVClient.RequestAsync(uri: new Uri(url),
                method: "GET",
                headers: null,
                data: null,
                contentType: "",
                cancellationToken: CancellationToken.None).ContinueWith<RouterState>(t =>
                {
                    var httpStatus = (int)t.Result.Item1;
                    if (httpStatus != 200)
                    {
                        //throw new AVException(AVException.ErrorCode.ConnectionFailed, "can not reach router.", null);
                    }
                    try
                    {
                        var result = t.Result.Item2;

                        var routerState = Json.Parse(result) as IDictionary<string, object>;
                        var expire = DateTime.Now.AddSeconds(long.Parse(routerState["ttl"].ToString()));
                        routerState["expire"] = expire.UnixTimeStampSeconds();

                        //save to local cache async.
                        AVPlugins.Instance.StorageController.LoadAsync().OnSuccess(storage => storage.Result.AddAsync(routerKey, result));
                        var routerStateObj = new RouterState()
                        {
                            groupId = routerState["groupId"] as string,
                            server = routerState["server"] as string,
                            secondary = routerState["secondary"] as string,
                            ttl = long.Parse(routerState["ttl"].ToString()),
                        };

                        return routerStateObj;
                    }
                    catch (Exception)
                    {
                        return null;
                    }

                });
        }
    }
}
