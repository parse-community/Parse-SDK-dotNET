using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Platform.Files;

namespace Parse.Tests;

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
            Location = unsecureUri,
            MediaType = null
        };

        Assert.AreEqual(unsecureUri, state.Location);
        Assert.AreEqual(secureUri, state.SecureLocation);

        // Make sure the proper port was given back.
        Assert.AreEqual(443, state.SecureLocation.Port);

        state = new FileState
        {
            Name = "B",
            Location = randomUri,
            MediaType = null
        };

        Assert.AreEqual(randomUri, state.Location);
        Assert.AreEqual(randomUri, state.Location);
    }
}
