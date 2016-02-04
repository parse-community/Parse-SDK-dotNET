using System;

namespace Parse.Common.Internal {
  [AttributeUsage(AttributeTargets.Assembly)]
  public class ParseModuleAttribute : Attribute {
    /// <summary>
    /// Instantiates a new ParseModuleAttribute.
    /// </summary>
    /// <param name="ModuleType">The type to which this module is applied.</param>
    public ParseModuleAttribute(Type ModuleType) {
      this.ModuleType = ModuleType;
    }

    public Type ModuleType { get; private set; }
  }
}