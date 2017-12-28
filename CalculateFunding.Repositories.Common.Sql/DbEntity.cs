using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CalculateFunding.Repositories.Common.Sql
{
    public abstract class DbEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTimeOffset CreatedAt { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTimeOffset UpdatedAt { get; set; }

        public bool Deleted { get; set; }

        [Timestamp]
        public byte[] Timestamp { get; set; }
    }
}