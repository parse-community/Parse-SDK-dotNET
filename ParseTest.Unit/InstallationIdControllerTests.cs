using Parse;
using Parse.Internal;
using NUnit.Framework;
using System;

namespace ParseTest {
  [TestFixture]
  public class InstallationIdControllerTests {
    [TearDown]
    public void TearDown() {
      ParseClient.ApplicationSettings.Clear();
    }

    [Test]
    public void TestConstructor() {
      var controller = new InstallationIdController();
      Assert.False(ParseClient.ApplicationSettings.ContainsKey("InstallationId"));
    }

    [Test]
    public void TestGet() {
      var controller = new InstallationIdController();
      var installationId = controller.Get();
      Assert.True(ParseClient.ApplicationSettings.ContainsKey("InstallationId"));

      ParseClient.ApplicationSettings.Clear();

      var newInstallationId = controller.Get();
      Assert.AreEqual(installationId, newInstallationId);
      Assert.False(ParseClient.ApplicationSettings.ContainsKey("InstallationId"));

      controller.Clear();

      newInstallationId = controller.Get();
      Assert.AreNotEqual(installationId, newInstallationId);
      Assert.True(ParseClient.ApplicationSettings.ContainsKey("InstallationId"));
    }

    [Test]
    public void TestSet() {
      var controller = new InstallationIdController();
      var installationId = controller.Get();
      Assert.True(ParseClient.ApplicationSettings.ContainsKey("InstallationId"));

      var installationId2 = Guid.NewGuid();
      controller.Set(installationId2);
      Assert.True(ParseClient.ApplicationSettings.ContainsKey("InstallationId"));
      Assert.AreEqual(installationId2.ToString(), ParseClient.ApplicationSettings["InstallationId"]);

      var installationId3 = controller.Get();
      Assert.AreEqual(installationId2, installationId3);

      ParseClient.ApplicationSettings.Clear();

      controller.Set(installationId);
      Assert.True(ParseClient.ApplicationSettings.ContainsKey("InstallationId"));
      Assert.AreEqual(installationId.ToString(), ParseClient.ApplicationSettings["InstallationId"]);

      controller.Clear();

      controller.Set(installationId2);
      Assert.True(ParseClient.ApplicationSettings.ContainsKey("InstallationId"));
      Assert.AreEqual(installationId2.ToString(), ParseClient.ApplicationSettings["InstallationId"]);
    }
  }
}
