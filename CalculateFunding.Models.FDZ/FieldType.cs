namespace CalculateFunding.Models.FDZ
{
    public enum FieldType
    {
        /// <summary>
        /// The SQL row identifier for this row, eg the autoincrement number or GUID
        /// </summary>
        RowIdentifier,

        /// <summary>
        /// String
        /// </summary>
        String,

        /// <summary>
        /// Integer
        /// </summary>
        Integer,

        /// <summary>
        /// Decimal
        /// </summary>
        Decimal,

        /// <summary>
        /// Date and time
        /// </summary>
        DateTime,


        /// <summary>
        /// Date
        /// </summary>
        Date,

        /// <summary>
        /// Identifier value for this row. Eg UKRPN, LACode, URN
        /// Based on the <see cref="IdentifierType">IdentifierType</see>
        /// </summary>
        Identifier,
    }
}
