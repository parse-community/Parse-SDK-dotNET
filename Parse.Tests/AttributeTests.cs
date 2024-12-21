using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Infrastructure.Control;
using Parse.Infrastructure.Utilities;

namespace Parse.Tests;


[TestClass]
public class AttributeTests
{
    [TestMethod]
    [Description("Tests that PreserveAttribute can set its boolean properties correctly.")]
    public void PreserveAttribute_SetPropertiesCorrectly()
    {
        var preserve = new PreserveAttribute { AllMembers = true, Conditional = true };
        Assert.IsTrue(preserve.AllMembers);
        Assert.IsTrue(preserve.Conditional);
        preserve.AllMembers = false;
        preserve.Conditional = false;
        Assert.IsFalse(preserve.AllMembers);
        Assert.IsFalse(preserve.Conditional);
    }
    [TestMethod]
    [Description("Test LinkerSafe attribute and ensures there is not exceptions on constructor.")]
    public void LinkerSafeAttribute_CanBeCreatedWithoutErrors()
    {
        var safe = new LinkerSafeAttribute();
        Assert.IsNotNull(safe);
    }
    [TestMethod]
    [Description("Tests that the PreserveWrapperTypes class has the Preserve attribute")]
    public void PreserveWrapperTypes_HasPreserveAttribute()
    {
        var attribute = typeof(PreserveWrapperTypes).GetTypeInfo().GetCustomAttribute<PreserveAttribute>(true);
        Assert.IsNotNull(attribute);
        Assert.IsTrue(attribute.AllMembers);
    }

    [TestMethod]
    [Description("Test that types exists in the AOTPreservations List with correct types.")]
    public void PreserveWrapperTypes_HasCorrectlyAOTTypesRegistered()// Mock difficulty: 1
    {
        var property = typeof(PreserveWrapperTypes).GetTypeInfo().GetDeclaredProperty("AOTPreservations");
        var list = property.GetValue(null) as List<object>;

        Assert.IsNotNull(list);
        Assert.IsTrue(list.Any(p => p.Equals(typeof(FlexibleListWrapper<object, object>))));
        Assert.IsTrue(list.Any(p => p.Equals(typeof(FlexibleListWrapper<float, float>))));

        Assert.IsTrue(list.Any(p => p.Equals(typeof(FlexibleDictionaryWrapper<object, object>))));
        Assert.IsTrue(list.Any(p => p.Equals(typeof(FlexibleDictionaryWrapper<double, float>))));
    }

  
}