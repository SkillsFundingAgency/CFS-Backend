using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.DataImporter
{
    public interface IExcelWriter<T>
    {
        byte[] Write(T data);
    }
}
