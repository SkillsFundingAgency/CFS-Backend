using CalculateFunding.Services.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Tests.Common
{
    public class StaticDateTimeService : ICurrentDateTime
    {
        private DateTime _dateTime;

        public StaticDateTimeService()
        {
            _dateTime = DateTime.UtcNow;
        }

        public void SetNow(DateTime dateTime)
        {
            _dateTime = dateTime;
        }

        public DateTime GetUtcNow()
        {
            return _dateTime;
        }
    }
}
