using Parse.Core.Internal;
using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Parse.Test
{
    [TestClass]
    public class FileStateTests
    {
        [TestMethod]
        public void TestSecureUrl()
        {
            Uri unsecureUri = new Uri("http://files.parsetfss.com/yolo.txt");
            Uri secureUri = new Uri("https://files.parsetfss.com/yolo.txt");
            Uri randomUri = new Uri("http://random.server.local/file.foo");

            FileState state = new FileState
            {
                Name = "A",
                Url = unsecureUri,
                MimeType = null
            };

            Assert.AreEqual(unsecureUri, state.Url);
            Assert.AreEqual(secureUri, state.SecureUrl);

            // Make sure the proper port was given back.
            Assert.AreEqual(443, state.SecureUrl.Port);

            state = new FileState
            {
                Name = "B",
                Url = randomUri,
                MimeType = null
            };

            Assert.AreEqual(randomUri, state.Url);
            Assert.AreEqual(randomUri, state.Url);
        }
    }
}
