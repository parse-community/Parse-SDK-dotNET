using Parse.Abstractions.Infrastructure;

namespace Parse
{
    public static class RoleServiceExtensions
    {
        /// <summary>
        /// Gets a <see cref="ParseQuery{ParseRole}"/> over the Role collection.
        /// </summary>
        public static ParseQuery<ParseRole> GetRoleQuery(this IServiceHub serviceHub)
        {
            return serviceHub.GetQuery<ParseRole>();
        }
    }
}
