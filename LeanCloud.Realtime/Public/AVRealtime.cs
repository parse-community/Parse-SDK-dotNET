using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeanCloud;
using System.Reflection;

namespace LeanCloud.Realtime
{
    public class AVRealtime
    {
        private static readonly object mutex = new object();
        public Configuration CurrentConfiguration { get; internal set; }
        public AVRealtime(Configuration config)
        {
            lock (mutex)
            {
                AVClient.Initialize(config.ApplicationId, config.ApplicationKey);
                CurrentConfiguration = config;
            }
        }

        public AVRealtime(string applicationId, string applicationKey)
            : this(new Configuration()
            {
                ApplicationId = applicationId,
                ApplicationKey = applicationKey
            })
        {

        }
        public Task<AVIMClient> CreateClient(string clientId, ISignatureFactory signatureFactory = null, string tag = null)
        {
            CurrentConfiguration = new Configuration()
            {
                ApplicationId = CurrentConfiguration.ApplicationId,
                ApplicationKey = CurrentConfiguration.ApplicationKey,
                SignatureFactory = signatureFactory
            };
            var client = new AVIMClient(clientId, tag)
            {
                SignatureFactory = signatureFactory
            };
            return client.ConnectAsync().ContinueWith(t =>
            {
                return Task.FromResult(client);
            }).Unwrap();
            //return Task.FromResult(new AVIMClient(clientId, tag)
            //{
            //    SignatureFactory = signatureFactory
            //});
        }

        public struct Configuration
        {
            public ISignatureFactory SignatureFactory { get; set; }
            public string ApplicationId { get; set; }
            public string ApplicationKey { get; set; }
        }
        static AVRealtime()
        {
            versionString = "net-portable/" + Version;
        }
        private static readonly string versionString;
        internal static string VersionString
        {
            get
            {
                return versionString;
            }
        }
        internal static System.Version Version
        {
            get
            {
                AssemblyName assemblyName = new AssemblyName(typeof(AVRealtime).GetTypeInfo().Assembly.FullName);
                return assemblyName.Version;
            }
        }
    }
}
