using CalculateFunding.Models.Providers;
using CalculateFunding.Services.Core;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Providers.UnitTests
{
    public class ProviderVersionByDateBuilder : TestEntityBuilder
    {
        private string _id;
        private int? _day;
        private int? _month;
        private int? _year;

        public ProviderVersionByDateBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public ProviderVersionByDateBuilder WithDay(int day)
        {
            _day = day;

            return this;
        }
        
        public ProviderVersionByDateBuilder WithMonth(int month)
        {
            _month = month;

            return this;
        }
        
        public ProviderVersionByDateBuilder WithYear(int year)
        {
            _year = year;

            return this;
        }
        

        public ProviderVersionByDate Build()
        {
            return new ProviderVersionByDate
            {
                Id = _id ?? NewRandomString(),
                Day = _day.GetValueOrDefault(NewRandomNumberBetween(1, 28)),
                Month = _month.GetValueOrDefault(NewRandomNumberBetween(1, 12)),
                Year = _year.GetValueOrDefault(NewRandomNumberBetween(2020, 2999))
            };
        }
    }
}