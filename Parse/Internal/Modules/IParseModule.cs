using System;

namespace Parse.Common.Internal {
  public interface IParseModule {
    void OnModuleRegistered();
    void OnParseInitialized();
  }
}