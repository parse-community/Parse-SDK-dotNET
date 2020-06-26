namespace Parse.Abstractions.Infrastructure
{
    public interface ICustomServiceHub : IServiceHub
    {
        IServiceHub Services { get; }
    }
}
