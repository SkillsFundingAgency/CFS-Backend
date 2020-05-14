using System;

namespace CalculateFunding.Services.Publishing
{
    public class LastPage
    {
        private readonly int _value;

        public LastPage(int totalCount, int top, int minPageCount = 1)
        {
            _value = Math.Max((int)Math.Ceiling((decimal) totalCount/ top), minPageCount);
        }
        
        public static implicit operator int(LastPage lastPage)
        {
            return lastPage._value;
        }
    }
}