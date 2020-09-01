using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Tests.Common.Helpers;
using System;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    public class ProfileTotalBuilder : TestEntityBuilder
    {
        private string _typeValue;
        private int? _occurrence;
        private int? _year;
        private decimal? _value;
        private bool _isPaid;
        private DateTimeOffset? _actualDate;
        private int? _installmentNumber;
        private decimal? _profileRemainingPercentage;

        public ProfileTotalBuilder WithProfileRemainingPercentage(decimal profileRemainingPercentage)
        {
            _profileRemainingPercentage = profileRemainingPercentage;

            return this;
        }

        public ProfileTotalBuilder WithInstallmentNumber(int installmentNumber)
        {
            _installmentNumber = installmentNumber;

            return this;
        }

        public ProfileTotalBuilder WithActualDate(DateTimeOffset? actualDate)
        {
            _actualDate = actualDate;

            return this;
        }

        public ProfileTotalBuilder WithIsPaid(bool isPaid)
        {
            _isPaid = isPaid;

            return this;
        }
        
        
        public ProfileTotalBuilder WithTypeValue(string typeValue)
        {
            _typeValue = typeValue;

            return this;
        }

        public ProfileTotalBuilder WithOccurrence(int occurrence)
        {
            _occurrence = occurrence;

            return this;
        }

        public ProfileTotalBuilder WithYear(int year)
        {
            _year = year;

            return this;
        }

        public ProfileTotalBuilder WithValue(decimal value)
        {
            _value = value;

            return this;
        }
        
        public ProfileTotal Build()
        {
            return new ProfileTotal
            {
                Year = _year.GetValueOrDefault(NewRandomYear()),
                Occurrence = _occurrence.GetValueOrDefault(NewRandomNumberBetween(0, 3)),
                Value = _value.GetValueOrDefault(NewRandomNumberBetween(1000, 99999)),
                TypeValue = _typeValue ?? NewRandomMonth(),
                IsPaid = _isPaid,
                ActualDate = _actualDate,
                InstallmentNumber = _installmentNumber.GetValueOrDefault(),
                ProfileRemainingPercentage = _profileRemainingPercentage
            };
        }     
    }
}