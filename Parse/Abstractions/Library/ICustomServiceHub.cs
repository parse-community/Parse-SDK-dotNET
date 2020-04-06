using System;
using System.Collections.Generic;
using System.Text;

namespace Parse.Abstractions.Library
{
    public interface ICustomServiceHub : IServiceHub
    {
        IServiceHub Services { get; }
    }
}
