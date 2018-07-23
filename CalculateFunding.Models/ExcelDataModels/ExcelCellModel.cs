using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.ExcelDataModels
{
    public class ExcelCellModel
    {
        public object Value { get; set; }

        public string Description { get; set; }

        public bool IsRequired { get; set; }

        public string FieldType { get; set; }

        public string Comment
        {
            get
            {
                return $"Description: {Description}\r\n Required: {(IsRequired ? "Yes" : "No")}\r\n Type: {FieldType}";
            }
        }
    }
}
