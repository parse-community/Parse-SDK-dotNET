namespace Parse.Common.Internal
{
    public interface IParseModule
    {
        void ExecuteModuleRegistrationHook();
        void ExecuteLibraryInitializationHook();
    }
}