using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Internal;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Parse.Platform.Objects;
using System.Threading;
using Parse.Abstractions.Platform.Users;

namespace Parse.Tests;

[TestClass]
public class RelationTests
{
    [ParseClassName("TestObject")]
    private class TestObject : ParseObject { }

    [ParseClassName("Friend")]
    private class Friend : ParseObject { }

    private ParseClient Client { get; set; }

    [TestInitialize]
    public void SetUp()
    {
        // Initialize the client and ensure the instance is set
        Client = new ParseClient(new ServerConnectionData { Test = true });
        Client.Publicize();

        // Register the test classes
        Client.RegisterSubclass(typeof(TestObject));
        Client.RegisterSubclass(typeof(Friend));
        Client.RegisterSubclass(typeof(ParseUser));
        Client.RegisterSubclass(typeof(ParseSession));
        Client.RegisterSubclass(typeof(ParseUser));

        // **--- Mocking Setup ---**
        var hub = new MutableServiceHub(); // Use MutableServiceHub for mocking
        var mockUserController = new Mock<IParseUserController>();
        var mockObjectController = new Mock<IParseObjectController>();

        // **Mock SignUpAsync for ParseUser:**
        mockUserController
            .Setup(controller => controller.SignUpAsync(
                It.IsAny<IObjectState>(),
                It.IsAny<IDictionary<string, IParseFieldOperation>>(),
            It.IsAny<IServiceHub>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MutableObjectState { ObjectId = "some0neTol4v4" }); // Predefined ObjectId for User

        // **Mock SaveAsync for ParseObject (Friend objects):**
        int objectSaveCounter = 1; // Counter for Friend ObjectIds
        mockObjectController
            .Setup(controller => controller.SaveAsync(
                It.IsAny<IObjectState>(),
                It.IsAny<IDictionary<string, IParseFieldOperation>>(),
                It.IsAny<string>(),
                It.IsAny<IServiceHub>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => // Use a lambda to generate different ObjectIds for each Friend
            {
                return new MutableObjectState { ObjectId = $"mockFriendObjectId{objectSaveCounter++}" };
            });

        // **Inject Mocks into ServiceHub:**
        hub.UserController = mockUserController.Object;
        hub.ObjectController = mockObjectController.Object;

    }
    
    [TestCleanup]
    public void TearDown() => (Client.Services as ServiceHub).Reset();

    [TestMethod]
    public void TestRelationQuery()
    {
        ParseObject parent = new ServiceHub { }.CreateObjectWithoutData("Foo", "abcxyz");

        ParseRelation<ParseObject> relation = parent.GetRelation<ParseObject>("child");
        ParseQuery<ParseObject> query = relation.Query;

        // Client side, the query will appear to be for the wrong class.
        // When the server recieves it, the class name will be redirected using the 'redirectClassNameForKey' option.
        Assert.AreEqual("Foo", query.GetClassName());

        IDictionary<string, object> encoded = query.BuildParameters();

        Assert.AreEqual("child", encoded["redirectClassNameForKey"]);
    }

    [TestMethod]
    [Description("Tests AddRelationToUserAsync throws exception when user is null")] // Mock difficulty: 1
    public async Task AddRelationToUserAsync_ThrowsException_WhenUserIsNull()
    {

        var relatedObjects = new List<ParseObject>
            {
                new ParseObject("Friend", Client.Services) { ["name"] = "Friend1" }
            };

        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => UserManagement.AddRelationToUserAsync(null, "friends", relatedObjects));

    }
    [TestMethod]
    [Description("Tests AddRelationToUserAsync throws exception when relationfield is null")] // Mock difficulty: 1
    public async Task AddRelationToUserAsync_ThrowsException_WhenRelationFieldIsNull()
    {
        var user = new ParseUser() { Username = "TestUser", Password = "TestPass", Services = Client.Services };
        await user.SignUpAsync();
        var relatedObjects = new List<ParseObject>
            {
                    new ParseObject("Friend", Client.Services) { ["name"] = "Friend1" }
            };
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => UserManagement.AddRelationToUserAsync(user, null, relatedObjects));
    }

    [TestMethod]
    [Description("Tests UpdateUserRelationAsync throws exception when user is null")] // Mock difficulty: 1
    public async Task UpdateUserRelationAsync_ThrowsException_WhenUserIsNull()
    {
        var relatedObjectsToAdd = new List<ParseObject>
            {
                new ParseObject("Friend", Client.Services) { ["name"] = "Friend1" }
            };
        var relatedObjectsToRemove = new List<ParseObject>
                {
                    new ParseObject("Friend", Client.Services) { ["name"] = "Friend2" }
                };


        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => UserManagement.UpdateUserRelationAsync(null, "friends", relatedObjectsToAdd, relatedObjectsToRemove));
    }
    [TestMethod]
    [Description("Tests UpdateUserRelationAsync throws exception when relationfield is null")] // Mock difficulty: 1
    public async Task UpdateUserRelationAsync_ThrowsException_WhenRelationFieldIsNull()
    {
        var user = new ParseUser() { Username = "TestUser", Password = "TestPass", Services = Client.Services };
        await user.SignUpAsync();

        var relatedObjectsToAdd = new List<ParseObject>
            {
                new ParseObject("Friend", Client.Services) { ["name"] = "Friend1" }
            };
        var relatedObjectsToRemove = new List<ParseObject>
                {
                    new ParseObject("Friend", Client.Services) { ["name"] = "Friend2" }
                };


        await Assert.ThrowsExceptionAsync<ArgumentException>(() => UserManagement.UpdateUserRelationAsync(user, null, relatedObjectsToAdd, relatedObjectsToRemove));
    }
    [TestMethod]
    [Description("Tests DeleteUserRelationAsync throws exception when user is null")] // Mock difficulty: 1
    public async Task DeleteUserRelationAsync_ThrowsException_WhenUserIsNull()
    {
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => UserManagement.DeleteUserRelationAsync(null, "friends"));
    }
    [TestMethod]
    [Description("Tests DeleteUserRelationAsync throws exception when relationfield is null")] // Mock difficulty: 1
    public async Task DeleteUserRelationAsync_ThrowsException_WhenRelationFieldIsNull()
    {
        var user = new ParseUser() { Username = "TestUser", Password = "TestPass", Services = Client.Services };
        await user.SignUpAsync();

        await Assert.ThrowsExceptionAsync<ArgumentException>(() => UserManagement.DeleteUserRelationAsync(user, null));
    }
    [TestMethod]
    [Description("Tests GetUserRelationsAsync throws exception when user is null")] // Mock difficulty: 1
    public async Task GetUserRelationsAsync_ThrowsException_WhenUserIsNull()
    {
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => UserManagement.GetUserRelationsAsync(null, "friends"));
    }
    [TestMethod]
    [Description("Tests GetUserRelationsAsync throws exception when relationfield is null")] // Mock difficulty: 1
    public async Task GetUserRelationsAsync_ThrowsException_WhenRelationFieldIsNull()
    {
        var user = new ParseUser() { Username = "TestUser", Password = "TestPass", Services = Client.Services };
        await user.SignUpAsync();

        await Assert.ThrowsExceptionAsync<ArgumentException>(() => UserManagement.GetUserRelationsAsync(user, null));
    }



    [TestMethod]
    [Description("Tests that AddRelationToUserAsync throws when a related object is unsaved")]
    public async Task AddRelationToUserAsync_ThrowsException_WhenRelatedObjectIsUnsaved()
    {
        // Arrange: Create and sign up a test user.
        var user = new ParseUser() { Username = "TestUser", Password = "TestPass", Services = Client.Services };
        await user.SignUpAsync();

        // Create an unsaved Friend object (do NOT call SaveAsync).
        var unsavedFriend = new ParseObject("Friend", Client.Services) { ["name"] = "UnsavedFriend" };
        var relatedObjects = new List<ParseObject> { unsavedFriend };

        // Act & Assert: Expect an exception when trying to add an unsaved object.
        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            UserManagement.AddRelationToUserAsync(user, "friends", relatedObjects));
    }


    
}

public static class UserManagement
{
    public static async Task AddRelationToUserAsync(ParseUser user, string relationField, IList<ParseObject> relatedObjects)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user), "User must not be null.");
        }

        if (string.IsNullOrEmpty(relationField))
        {
            throw new ArgumentException("Relation field must not be null or empty.", nameof(relationField));
        }

        if (relatedObjects == null || relatedObjects.Count == 0)
        {
            Debug.WriteLine("No objects provided to add to the relation.");
            return;
        }

        var relation = user.GetRelation<ParseObject>(relationField);

        foreach (var obj in relatedObjects)
        {
            relation.Add(obj);
        }

        await user.SaveAsync();
        Debug.WriteLine($"Added {relatedObjects.Count} objects to the '{relationField}' relation for user '{user.Username}'.");
    }
    public static async Task UpdateUserRelationAsync(ParseUser user, string relationField, IList<ParseObject> toAdd, IList<ParseObject> toRemove)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user), "User must not be null.");
        }

        if (string.IsNullOrEmpty(relationField))
        {
            throw new ArgumentException("Relation field must not be null or empty.", nameof(relationField));
        }

        var relation = user.GetRelation<ParseObject>(relationField);

        // Add objects to the relation
        if (toAdd != null && toAdd.Count > 0)
        {
            foreach (var obj in toAdd)
            {
                relation.Add(obj);
            }
            Debug.WriteLine($"Added {toAdd.Count} objects to the '{relationField}' relation.");
        }

        // Remove objects from the relation
        if (toRemove != null && toRemove.Count > 0)
        {

            foreach (var obj in toRemove)
            {
                relation.Remove(obj);
            }
            Debug.WriteLine($"Removed {toRemove.Count} objects from the '{relationField}' relation.");
        }

        await user.SaveAsync();
    }
    public static async Task DeleteUserRelationAsync(ParseUser user, string relationField)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user), "User must not be null.");
        }

        if (string.IsNullOrEmpty(relationField))
        {
            throw new ArgumentException("Relation field must not be null or empty.", nameof(relationField));
        }

        var relation = user.GetRelation<ParseObject>(relationField);
        var relatedObjects = await relation.Query.FindAsync();


        foreach (var obj in relatedObjects)
        {
            relation.Remove(obj);
        }

        await user.SaveAsync();
        Debug.WriteLine($"Removed all objects from the '{relationField}' relation for user '{user.Username}'.");
    }
    public static async Task ManageUserRelationsAsync(ParseClient client)
    {
        // Get the current user
        var user = await ParseClient.Instance.GetCurrentUser();

        if (user == null)
        {
            Debug.WriteLine("No user is currently logged in.");
            return;
        }

        const string relationField = "friends"; // Example relation field name

        // Create related objects to add
        var relatedObjectsToAdd = new List<ParseObject>
    {
        new ParseObject("Friend", client.Services) { ["name"] = "Alice" },
        new ParseObject("Friend", client.Services) { ["name"] = "Bob" }
    };

        // Save related objects to the server before adding to the relation
        foreach (var obj in relatedObjectsToAdd)
        {
            await obj.SaveAsync();
        }

        // Add objects to the relation
        await AddRelationToUserAsync(user, relationField, relatedObjectsToAdd);

        // Query the relation
        var relatedObjects = await GetUserRelationsAsync(user, relationField);

        // Update the relation (add and remove objects)
        var relatedObjectsToRemove = new List<ParseObject> { relatedObjects[0] }; // Remove the first related object
        var newObjectsToAdd = new List<ParseObject>
    {
        new ParseObject("Friend", client.Services) { ["name"] = "Charlie" }
    };

        foreach (var obj in newObjectsToAdd)
        {
            await obj.SaveAsync();
        }

        await UpdateUserRelationAsync(user, relationField, newObjectsToAdd, relatedObjectsToRemove);

    }
    public static async Task<IList<ParseObject>> GetUserRelationsAsync(ParseUser user, string relationField)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user), "User must not be null.");
        }

        if (string.IsNullOrEmpty(relationField))
        {
            throw new ArgumentException("Relation field must not be null or empty.", nameof(relationField));
        }

        var relation = user.GetRelation<ParseObject>(relationField);

        var results = await relation.Query.FindAsync();
        Debug.WriteLine($"Retrieved {results.Count()} objects from the '{relationField}' relation for user '{user.Username}'.");
        return results.ToList();
    }

}

