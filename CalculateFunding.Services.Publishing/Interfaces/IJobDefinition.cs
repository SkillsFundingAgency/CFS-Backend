using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IJobDefinition
    {
        string TriggerMessage { get; }
        string Id { get; }
    }
}
