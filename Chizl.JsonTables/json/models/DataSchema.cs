namespace Chizl.JsonTables.json
{
    public class JsonDataColumn
    {
        public JsonDataColumn(string columnName, string dataType, bool unique) 
        { 
            ColumnName = columnName;
            DataType = dataType;
            Unique = unique.ToString();
        }
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public string Unique { get; set; }
    }
}
