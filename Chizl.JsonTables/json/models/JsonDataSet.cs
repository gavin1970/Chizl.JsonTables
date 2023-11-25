using System;
using System.Collections.Generic;

namespace Chizl.JsonTables.json
{
    public class JsonDataSet 
    {
        public string DataSetName { get; } = string.Empty;
        public JsonDataSet(string dataSetName)
        {
            DataSetName = dataSetName ?? throw new ArgumentNullException(nameof(dataSetName));
        }
        public List<JsonDataTable> DataTables { get; set; } = new List<JsonDataTable>();
    }
}