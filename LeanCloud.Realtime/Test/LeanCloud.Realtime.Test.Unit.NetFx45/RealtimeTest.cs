using Moq;
using NUnit.Framework;
using LeanCloud;
using LeanCloud.Core.Internal;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;


namespace LeanCloud.Realtime.Test.Unit.NetFx45
{
    [TestFixture]
    public class RealtimeTest
    {
        [SetUp]
        public void SetUp()
        {

        }

        [TearDown]
        public void TearDown()
        {
            AVPlugins.Instance.Reset();
        }

        [Test]
        [AsyncStateMachine(typeof(RealtimeTest))]
        public Task TestConnectRouter()
        {
            //AVClient.Initialize("3knLr8wGGKUBiXpVAwDnryNT-gzGzoHsz", "3RpBhjoPXJjVWvPnVmPyFExt");
            var realtime = new AVRealtime("3knLr8wGGKUBiXpVAwDnryNT-gzGzoHsz", "3RpBhjoPXJjVWvPnVmPyFExt");
            return realtime.CreateClient("junwu").ContinueWith(t =>
             {
                 var client = t.Result;
                 Console.WriteLine(client.State.ToString());
                 return Task.FromResult(0);
             }).Unwrap();
            //var avObject = new AVObject("TestObject");
            //avObject["key"] = "value";
            //return avObject.SaveAsync().ContinueWith(t =>
            //{
            //    Console.WriteLine(avObject.ObjectId);
            //    return Task.FromResult(0);
            //}).Unwrap();
        }
        [Test]
        public void TestInitRealtime()
        {
            //AVClient.Initialize("3knLr8wGGKUBiXpVAwDnryNT-gzGzoHsz", "3RpBhjoPXJjVWvPnVmPyFExt");
            var realtime = new AVRealtime("3knLr8wGGKUBiXpVAwDnryNT-gzGzoHsz", "3RpBhjoPXJjVWvPnVmPyFExt");
            realtime.CreateClient("junwu").ContinueWith(t =>
            {
                var client = t.Result;
                Console.WriteLine(client.State.ToString());
                return Task.FromResult(0);
            }).Unwrap().Wait();
            //var avObject = new AVObject("TestObject");
            //avObject["key"] = "value";
            //return avObject.SaveAsync().ContinueWith(t =>
            //{
            //    Console.WriteLine(avObject.ObjectId);
            //    return Task.FromResult(0);
            //}).Unwrap();
        }
    }
}
