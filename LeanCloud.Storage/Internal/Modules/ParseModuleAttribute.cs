using System;

namespace LeanCloud.Storage.Internal {
  [AttributeUsage(AttributeTargets.Assembly)]
  public class AVModuleAttribute : Attribute {
    /// <summary>
    /// Instantiates a new AVModuleAttribute.
    /// </summary>
    /// <param name="ModuleType">The type to which this module is applied.</param>
    public AVModuleAttribute(Type ModuleType) {
      this.ModuleType = ModuleType;
    }

    public Type ModuleType { get; private set; }
  }
}