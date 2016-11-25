using System;

namespace LeanCloud.Common.Internal {
  public interface IAVModule {
    void OnModuleRegistered();
    void OnParseInitialized();
  }
}