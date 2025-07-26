using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parse.Abstractions.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Parse.Infrastructure;


namespace Parse.Tests;

[TestClass]
public class ParseQueryExtensionsTests
{
    private ParseClient Client { get; set; }

    [TestInitialize]
    public void SetUp()
    {
        // Each test needs a client instance to get the ServiceHub.
        Client = new ParseClient(new ServerConnectionData { Test = true });
    }

    [TestMethod]
    [Description("Tests that a LINQ '==' operator is translated to WhereEqualTo.")]
    public void Where_EqualsOperator_TranslatesToWhereEqualTo()
    {
        // Arrange
        var query = new ParseQuery<ParseObject>(Client.Services, "TestClass");

        // Act: Use the LINQ Where extension method.
        var resultQuery = query.Where(obj => obj.Get<string>("name") == "Zeke");
        var constraint = resultQuery.GetConstraint("name");

        // Assert
        Assert.IsNotNull(constraint);
        Assert.AreEqual("Zeke", constraint);
    }

    [TestMethod]
    [Description("Tests that a LINQ '>' operator is translated to WhereGreaterThan.")]
    public void Where_GreaterThanOperator_TranslatesToWhereGreaterThan()
    {
        // Arrange
        var query = new ParseQuery<ParseObject>(Client.Services, "TestClass");

        // Act
        var resultQuery = query.Where(obj => obj.Get<int>("score") > 1000);
        var constraint = resultQuery.GetConstraint("score") as IDictionary<string, object>;

        // Assert
        Assert.IsNotNull(constraint);
        Assert.IsTrue(constraint.ContainsKey("$gt"));
        Assert.AreEqual(1000, constraint["$gt"]);
    }

    [TestMethod]
    [Description("Tests that a LINQ '!=' operator is translated to WhereNotEqualTo.")]
    public void Where_NotEqualsOperator_TranslatesToWhereNotEqualTo()
    {
        // Arrange
        var query = new ParseQuery<ParseObject>(Client.Services, "TestClass");

        // Act
        var resultQuery = query.Where(obj => obj.Get<string>("status") != "inactive");
        var constraint = resultQuery.GetConstraint("status") as IDictionary<string, object>;

        // Assert
        Assert.IsNotNull(constraint);
        Assert.IsTrue(constraint.ContainsKey("$ne"));
        Assert.AreEqual("inactive", constraint["$ne"]);
    }

    [TestMethod]
    [Description("Tests that OrderBy with a property is translated correctly.")]
    public void OrderBy_Property_TranslatesToOrderBy()
    {
        // Arrange
        var query = new ParseQuery<ParseObject>(Client.Services, "TestClass");

        // Act
        var resultQuery = query.OrderBy(obj => obj.CreatedAt);
        var parameters = resultQuery.BuildParameters();

        // Assert
        Assert.IsTrue(parameters.ContainsKey("order"));
        Assert.AreEqual("createdAt", parameters["order"]);
    }

    [TestMethod]
    [Description("Tests that a complex LINQ expression with '&&' is translated correctly.")]
    public void Where_AndAlsoOperator_TranslatesToMultipleConstraints()
    {
        // Arrange
        var query = new ParseQuery<ParseObject>(Client.Services, "Player");

        // Act
        var resultQuery = query.Where(p => p.Get<int>("score") > 100 && p.Get<bool>("active") == true);
        var scoreConstraint = resultQuery.GetConstraint("score") as IDictionary<string, object>;
        var activeConstraint = resultQuery.GetConstraint("active");

        // Assert
        Assert.IsNotNull(scoreConstraint);
        Assert.AreEqual(100, scoreConstraint["$gt"]);
        Assert.IsNotNull(activeConstraint);
        Assert.AreEqual(true, activeConstraint);
    }
}