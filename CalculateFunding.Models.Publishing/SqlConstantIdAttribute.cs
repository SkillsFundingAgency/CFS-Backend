using System;

namespace CalculateFunding.Models.Publishing
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SqlConstantIdAttribute : Attribute
    {
        public int Id { get; set; }

        public SqlConstantIdAttribute(int id)
        {
            Id = id;
        }
    }
}
