using System;
using System.Data;

namespace CalculateFunding.Services.SqlExport
{
    public abstract class DataTableBuilder<TDto> : IDataTableBuilder<TDto>
    {
        private volatile bool _isSetUp;

        public DataTable DataTable { get; set; }

        public string TableName { get; protected set; }

        public bool HasNoData => TableName.IsNullOrWhitespace();

        protected abstract DataColumn[] GetDataColumns(TDto dto);

        protected DataColumn NewDataColumn<T>(string name,
            int maxLength = -1,
            bool allowNull = false,
            object defaultValue = null)
            => new DataColumn(name, typeof(T))
            {
                AllowDBNull = allowNull,
                MaxLength = maxLength,
                DefaultValue = defaultValue
            };

        public void AddRows(params TDto[] rows)
        {
            foreach (TDto row in rows)
            {
                if (!_isSetUp)
                {
                    EnsureDataTableExists(row);
                    EnsureTableNameIsSet(row);

                    _isSetUp = true;
                }

                lock (DataTable)
                {
                    AddDataRowToDataTable(row);
                }
            }
        }

        protected abstract void AddDataRowToDataTable(TDto dto);

        protected abstract void EnsureTableNameIsSet(TDto dto);

        protected void EnsureDataTableExists(TDto dto)
        {
            if (DataTable != null)
            {
                return;
            }

            lock (this)
            {
                if (DataTable != null)
                {
                    return;
                }
                
                DataTable = new DataTable();
                DataTable.Columns.AddRange(GetDataColumns(dto));
            }
        }

        protected object DbNullSafe(object value)
            => value ?? DBNull.Value;
    }
}