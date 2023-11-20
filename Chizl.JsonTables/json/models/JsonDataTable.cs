using System;
using System.Collections.Generic;
using System.Data;

namespace Chizl.JsonTables.json
{
    public class JsonDataTable
    {
        public string TableName { get; set; } = string.Empty;
        public List<JsonDataColumn> Schema { get; set; } = new List<JsonDataColumn>();
        public DataTable Table { get; set; }
        public JsonDataTable(string tableName)
        {
            TableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
        }
    }
}