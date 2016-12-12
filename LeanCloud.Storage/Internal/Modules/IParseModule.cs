using System;

namespace LeanCloud.Storage.Internal {
  public interface IAVModule {
    void OnModuleRegistered();
    void OnParseInitialized();
  }
}