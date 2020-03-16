using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface ITransactionFactory
    {
        Transaction NewTransaction<T>() where T : class;
    }
}
