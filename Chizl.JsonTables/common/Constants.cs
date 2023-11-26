namespace Chizl.JsonTables.json
{
    public enum CJ_RESP_STATUS
    {
        Success = 0,
        Warning = 1,
        Error = 2
    }

    public enum METHOD_HISTORY
    {
        THIS_METHOD = 0,
        CALLING_METHOD = 1,
        PREVIOUS_METHOD = 2,
    }

    public static class ClassExtension
    {
        public static int ToInt(this METHOD_HISTORY mh)
        {
            return (int)mh;
        }
        public static int ToInt(this CJ_RESP_STATUS mh)
        {
            return (int)mh;
        }
    }

    internal class Constants
    {
        public static readonly string NOT_INITIALIZED = "Instance of class not initialized.";
        public static readonly string SEC_STR_WRAPPER = "#B64AES#";
        public static readonly string BASE_DATASET_NAME = "ChizlJsonTables";
        public static readonly string DEFAULT_LOADING = "Loading";
        public static readonly string JSON_FORMAT_EXCEPTION = "Unexpected Json format.";
        public static readonly string ARGS_MISSING = "Missing required argument.";
        public static readonly string TABLE_MISSING = "Table not found.";
        public static readonly string TABLE_EXISTS = "Table already exists.";
        public static readonly string COLUMN_MISSING = "Column name not found.";
        public static readonly string FILE_MISSING = "File does not exist.";
        public static readonly string DATA_MISSING = "Missing content from file.";
        public static readonly string DATASET_MISSING = "Missing dataset from file.";
        public static readonly string COLUMN_EXISTS = "Column name already exists.";
        public static readonly string COLUMN_CREATED_DATE = "CreatedDate";
        public static readonly string COLUMN_MODIFIED_DATE = "ModifiedDate";
    }
}
