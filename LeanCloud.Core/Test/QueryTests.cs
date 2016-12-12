using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using LeanCloud;
using LeanCloud.Core.Internal;
using System.Configuration;
using System.Runtime.CompilerServices;
using LeanCloud.Storage;

namespace ParseTest
{
    [TestFixture]
    public class QueryTests
    {
        [SetUp]
        public void SetUp()
        {
            string appId = ConfigurationManager.AppSettings["appId"];
            string appKey = ConfigurationManager.AppSettings["appKey"];
            AVClient.Initialize(appId, appKey);
        }

        [Test]
        [AsyncStateMachine(typeof(QueryTests))]
        public Task CQLQueryTest()
        {
            string cql = "select * from Todo where location='会议室'";

            return AVQuery<AVObject>.DoCloudQueryAsync(cql).ContinueWith(t =>
            {
                Assert.False(t.IsFaulted);
                Assert.False(t.IsCanceled);
                Assert.True(t.Result.Count() > 0);
                return Task.FromResult(0);
            });
        }
        [Test]
        [AsyncStateMachine(typeof(QueryTests))]
        public Task CQLQueryWithPlaceholderTest()
        {
            string cql = "select * from Todo where location=?";
            

            return AVQuery<AVObject>.DoCloudQueryAsync(cql,"会议室").ContinueWith(t =>
            {
                Assert.False(t.IsFaulted);
                Assert.False(t.IsCanceled);
                Assert.True(t.Result.Count() > 0);
                return Task.FromResult(0);
            });
        }
    }
}
