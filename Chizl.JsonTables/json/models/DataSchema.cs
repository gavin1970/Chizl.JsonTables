namespace Chizl.JsonTables.json
{
    public class JsonDataColumn
    {
        public JsonDataColumn(string columnName, string dataType) 
        { 
            ColumnName = columnName;
            DataType = dataType;
        }
        public string ColumnName { get; set; }
        public string DataType { get; set; }
    }
}
