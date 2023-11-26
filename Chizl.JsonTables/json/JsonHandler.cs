using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Security;
using System.Text;

namespace Chizl.JsonTables.json
{
    public class JsonHandler
    {
        private readonly DataSet mr_dataSet;
        private readonly JsonDataConverter mr_jsonConverter;

        #region Properties
        #region Public
        public bool FileExists { get; private set; } = false;
        public bool UseUTCDate { get; set; } = true;
        #endregion

        #region Private
        private JsonIO JsonFile { get; }
        private bool IsIntialized { get; set; } = false;
        private DateTime CurrentDateTime { get { return UseUTCDate ? DateTime.UtcNow : DateTime.Now; } }
        #endregion
        #endregion

        #region Constructor
        public JsonHandler() { }
        public JsonHandler(string dataFileName, string dataSetName) : this(dataFileName, dataSetName, new SecureString()) { }
        public JsonHandler(string dataFileName, string dataSetName, SecureString encSalt)
        {
            if (string.IsNullOrWhiteSpace(dataFileName))
                throw new ArgumentException(Constants.ARGS_MISSING, nameof(dataFileName));

            JsonFile = new JsonIO(dataFileName);
            var respStatus = new CJRespInfo();

            mr_jsonConverter = new JsonDataConverter(dataSetName, encSalt);
            mr_dataSet = new DataSet(dataSetName);

            if (!JsonFile.LoadFile(dataSetName, out JsonDataSet jsonDataSet, out respStatus))
                FileExists = JsonFile.FileExists;
            else
                FileExists = true;

            if (jsonDataSet != null && !jsonDataSet.DataSetName.Equals(Constants.DEFAULT_LOADING))
                mr_jsonConverter.ToDataSet(jsonDataSet, out mr_dataSet, out respStatus);

            StringBuilder sb = new StringBuilder();

            if (respStatus.HasErrors || respStatus.HasWarnings)
                sb = new StringBuilder($"While in {respStatus.ClassName} the following issues occured:");

            if (respStatus.HasErrors)
            {
                sb.AppendLine("Errors:");
                sb.AppendLine(string.Join(Environment.NewLine, respStatus.Errors));
            }

            if (respStatus.HasWarnings)
            {
                sb.AppendLine("Warnings:");
                sb.AppendLine(string.Join(Environment.NewLine, respStatus.Warnings));
            }

            if (sb.Length > 0)
                throw new Exception(sb.ToString());
            else
                IsIntialized = true;
        }
        #endregion

        private bool Intialized(ref CJRespInfo respStatus)
        {
            bool retVal = true;
            if (!IsIntialized || mr_dataSet == null)
            {
                respStatus.Errors.Add($"{Constants.NOT_INITIALIZED}");
                retVal = false; ;
            }

            return retVal;
        }
        #region Column Methods
        /// <summary>
        /// Checks to see if column exists
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="columnName">Column from tableName</param>
        /// <returns>bool</returns>
        public bool ColumnExists(string tableName, string columnName)
            => ColumnExists(tableName, columnName, out _);
        /// <summary>
        /// Checks to see if column exists
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="columnName">Column from tableName</param>
        /// <param name="respStatus">Status with all errors and all warnings.</param>
        /// <returns>bool</returns>
        public bool ColumnExists(string tableName, string columnName, out CJRespInfo respStatus)
        {
            bool retVal = false;
            respStatus = new CJRespInfo();
            if (!Intialized(ref respStatus))
                return retVal;

            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    respStatus.Errors.Add($"{Constants.ARGS_MISSING}\n\t{nameof(tableName)}");
                else if (string.IsNullOrWhiteSpace(columnName))
                    respStatus.Errors.Add($"{Constants.ARGS_MISSING}\n\t{nameof(columnName)}");
                else if (!mr_dataSet.Tables.Contains(tableName))
                    respStatus.Warnings.Add($"{Constants.TABLE_MISSING}\n\t{tableName}");
                else
                    retVal = mr_dataSet.Tables[tableName].Columns.Contains(columnName);
            }
            catch (Exception ex)
            {
                respStatus.Errors.Add($"Exception:\n\t{ex.Message}");
            }

            return retVal;
        }
        /// <summary>
        /// Add multiple columns to a table.
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="dataColumns">All data columns</param>
        /// <returns>bool</returns>
        public bool AddColumns(string tableName, DataColumn[] dataColumns)
            => AddColumns(tableName, dataColumns, out _);
        /// <summary>
        /// Add multiple columns to a table.
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="dataColumns">All data columns</param>
        /// <param name="respStatus">Status with all errors and all warnings.</param>
        /// <returns>bool</returns>
        public bool AddColumns(string tableName, DataColumn[] dataColumns, out CJRespInfo respStatus)
        {
            bool hadException = false;
            respStatus = new CJRespInfo();
            if (!Intialized(ref respStatus))
                return false;

            List<string> successList = new List<string>();
            foreach (DataColumn col in dataColumns)
            {
                bool success = AddColumn(tableName, col, out respStatus);
                //if not success, and has errors, then failed, lets break.
                //Could be warnings, warnings occur if column already exists.
                //We don't want to break for column exists.
                if (!success && respStatus.HasErrors)
                    break;
                else if (success)
                    successList.Add(col.ColumnName);
            }

            //back out all that were added.
            if (hadException)
            {
                foreach (string colName in successList)
                    RemoveColumn(tableName, colName, out respStatus);
            }

            return !hadException;
        }
        /// <summary>
        /// Add a column to a table if the column doesn't already exist.<br/>
        /// Will also create a table based on table name, if one doesn't already exist.<br/>
        /// Only successful if column was added.   Just because it wasn't added, doesn't <br/>
        /// mean it failed, could mean it already existed.  This is why I have a hadException<br/>
        /// flag that also is returned.
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="dataColumn">Data column represented</param>
        /// <returns>bool</returns>
        public bool AddColumn(string tableName, DataColumn dataColumn)
            => AddColumn(tableName, dataColumn, out _);
        /// <summary>
        /// Add a column to a table if the column doesn't already exist.<br/>
        /// Will also create a table based on table name, if one doesn't already exist.<br/>
        /// Only successful if column was added.   Just because it wasn't added, doesn't <br/>
        /// mean it failed, could mean it already existed.  This is why I have a hadException<br/>
        /// flag that also is returned.
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="dataColumn">Data column represented</param>
        /// <param name="hadException"></param>
        /// <returns>bool</returns>
        public bool AddColumn(string tableName, DataColumn dataColumn, out CJRespInfo respStatus)
        {
            bool retVal = false;
            respStatus = new CJRespInfo();
            if (!Intialized(ref respStatus))
                return retVal;

            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    respStatus.Errors.Add($"{Constants.ARGS_MISSING}\n\t{nameof(tableName)}");
                if (dataColumn == null)
                    respStatus.Errors.Add($"{Constants.ARGS_MISSING}\n\t{nameof(dataColumn)}");
                else
                {
                    //auto create table if doesn't exist.
                    if (!mr_dataSet.Tables.Contains(tableName))
                    {
                        DataTable dt = new DataTable(tableName);
                        mr_dataSet.Tables.Add(dt);
                    }

                    if (!mr_dataSet.Tables[tableName].Columns.Contains(dataColumn.ColumnName))
                    {
                        mr_dataSet.Tables[tableName].Columns.Add(dataColumn);
                        retVal = true;
                    }
                    else
                        respStatus.Warnings.Add($"Column '{dataColumn}' already exists in '{tableName}'.");
                }
            }
            catch (Exception ex)
            {
                respStatus.Errors.Add($"Exception:\n\t{ex.Message}");
            }

            return retVal;
        }
        /// <summary>
        /// Will remove a column from a table, if it exists.
        /// </summary>
        /// <param name="columnName">Column from tableName</param>
        /// <param name="tableName">Table represented</param>
        /// <returns>bool</returns>
        public bool RemoveColumn(string tableName, string columnName)
            => RemoveColumn(tableName, columnName, out _);
        /// <summary>
        /// Will remove a column from a table, if it exists.
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="columnName">Column from tableName</param>
        /// <param name="respStatus">Status with all errors and all warnings.</param>
        /// <returns>bool</returns>
        public bool RemoveColumn(string tableName, string columnName, out CJRespInfo respStatus)
        {
            bool retVal = false;
            respStatus = new CJRespInfo();
            if (!Intialized(ref respStatus))
                return retVal;

            try
            {
                if (ColumnExists(tableName, columnName, out respStatus))
                {
                    mr_dataSet.Tables[tableName].Columns.Remove(columnName);
                    retVal = true;
                }
            }
            catch (Exception ex)
            {
                respStatus.Errors.Add($"Exception:\n\t{ex.Message}");
            }

            return retVal;
        }
        /// <summary>
        /// Return if column is secured or not.
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="columnName">Column from tableName</param>
        /// <returns>bool</returns>
        public bool IsSecured(string tableName, string columnName)
            => IsSecured(tableName, columnName, out _);
        /// <summary>
        /// Return if column is secured or not.
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="columnName">Column from tableName</param>
        /// <param name="respStatus">Status with all errors and all warnings.</param>
        /// <returns>bool</returns>
        public bool IsSecured(string tableName, string columnName, out CJRespInfo respStatus)
        {
            bool retVal = false;
            respStatus = new CJRespInfo();
            if (!Intialized(ref respStatus))
                return retVal;

            try
            {
                if (ColumnExists(tableName, columnName, out respStatus))
                    retVal = mr_dataSet.Tables[tableName].Columns[columnName].DataType == typeof(SecureString);
                else
                    respStatus.Errors.Add($"{Constants.COLUMN_MISSING}\n\t{columnName}");
            }
            catch (Exception ex)
            {
                respStatus.Errors.Add($"Exception:\n\t{ex.Message}");
            }

            return retVal;
        }
        /// <summary>
        /// Generic to get column from a DataRow
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <param name="columnName">Column from tableName</param>
        /// <returns>Generic</returns>
        /// <example>
        /// int userId = dataHelper.GetColumn<int>(dataRow, "UserID")
        /// </example>
        public static T GetColumn<T>(DataRow dr, string columnName, T defaultValue = default)
        {
            T retVal;

            var value = dr[columnName];
            if (Utils.ConvertTo<T>(value, out T newVal))
                retVal = newVal;
            else
                retVal = defaultValue;

            return retVal;
        }
        #endregion

        #region Table Methods
        /// <summary>
        /// Check if Table Exists
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <returns>bool</returns>
        public bool TableExists(string tableName)
            => TableExists(tableName, out _);
        /// <summary>
        /// Check if Table Exists
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="respStatus">Status with all errors and all warnings.</param>
        /// <returns>bool</returns>
        public bool TableExists(string tableName, out CJRespInfo respStatus)
        {
            bool retVal = false;
            respStatus = new CJRespInfo();
            if (!Intialized(ref respStatus))
                return retVal;

            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    respStatus.Errors.Add($"{Constants.ARGS_MISSING}\n\t{nameof(tableName)}");
                else
                    retVal = mr_dataSet.Tables.Contains(tableName);
            }
            catch (Exception ex)
            {
                respStatus.Errors.Add($"Exception:\n\t{ex.Message}");
            }

            return retVal;
        }
        /// <summary>
        /// Get a table and all it's data.
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="dataTable"></param>
        /// <returns>bool</returns>
        public bool GetTable(string tableName, out DataTable dataTable)
            => GetTable(tableName, out dataTable, out _);
        /// <summary>
        /// Get a table and all it's data.
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="dataTable"></param>
        /// <param name="respStatus">Status with all errors and all warnings.</param>
        /// <returns>bool</returns>
        public bool GetTable(string tableName, out DataTable dataTable, out CJRespInfo respStatus)
        {
            bool retVal = false;
            dataTable = new DataTable(tableName);
            respStatus = new CJRespInfo();
            if (!Intialized(ref respStatus))
                return retVal;

            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    respStatus.Errors.Add($"{Constants.ARGS_MISSING}\n\t{nameof(tableName)}");
                else if (!mr_dataSet.Tables.Contains(tableName))
                    respStatus.Warnings.Add($"{Constants.TABLE_MISSING}\n\t{nameof(tableName)}");
                else
                {
                    dataTable = mr_dataSet.Tables[tableName];
                    retVal = true;
                }
            }
            catch (Exception ex)
            {
                respStatus.Errors.Add($"Exception:\n\t{ex.Message}");
            }

            return retVal;
        }
        /// <summary>
        /// Add a table by DataTable
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="respStatus">Status with all errors and all warnings.</param>
        /// <returns>bool</returns>
        public bool AddTable(DataTable dataTable, bool replaceIfExists = false)
            => AddTable(dataTable, out _, replaceIfExists);
        /// <summary>
        /// Add a table by DataTable
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="respStatus">Status with all errors and all warnings.</param>
        /// <param name="replaceIfExists"></param>
        /// <returns>bool</returns>
        public bool AddTable(DataTable dataTable, out CJRespInfo respStatus, bool replaceIfExists = false)
        {
            bool retVal = false;
            respStatus = new CJRespInfo();
            if (!Intialized(ref respStatus))
                return retVal;

            try
            {
                if (dataTable == null)
                    respStatus.Errors.Add($"{Constants.ARGS_MISSING}\n\t{nameof(dataTable)}");
                else
                {
                    if (mr_dataSet.Tables.Contains(dataTable.TableName))
                    {
                        if (replaceIfExists)
                            mr_dataSet.Tables.Remove(dataTable.TableName);
                        else
                            respStatus.Warnings.Add($"{Constants.TABLE_EXISTS}\n\t{dataTable.TableName}");
                    }

                    if (!respStatus.HasErrorOrWarnings)
                    {
                        mr_dataSet.Tables.Add(dataTable);
                        retVal = true;
                    }
                }
            }
            catch (Exception ex)
            {
                respStatus.Errors.Add($"Exception:\n\t{ex.Message}");
            }

            return retVal;
        }
        /// <summary>
        /// Update table or Add if new table.
        /// </summary>
        /// <param name="dataTable"></param>
        /// <returns>bool</returns>
        public bool UpdateTable(DataTable dataTable)
            => AddTable(dataTable, out _, true);
        /// <summary>
        /// Update table or Add if new table.
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="respStatus">Status with all errors and all warnings.</param>
        /// <returns>bool</returns>
        public bool UpdateTable(DataTable dataTable, out CJRespInfo respStatus)
            => AddTable(dataTable, out respStatus, true);
        /// <summary>
        /// Delete a table
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <returns>bool</returns>
        public bool RemoveTable(string tableName)
            => RemoveTable(tableName, out _);
        /// <summary>
        /// Delete a table
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="respStatus">Status with all errors and all warnings.</param>
        /// <returns>bool</returns>
        public bool RemoveTable(string tableName, out CJRespInfo respStatus)
        {
            bool retVal = false;
            respStatus = new CJRespInfo();
            if (!Intialized(ref respStatus))
                return retVal;

            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    throw new ArgumentException(Constants.ARGS_MISSING, nameof(tableName));
                else if (mr_dataSet.Tables.Contains(tableName))
                {
                    mr_dataSet.Tables.Remove(tableName);
                    retVal = true;
                }
            }
            catch (Exception ex)
            {
                respStatus.Errors.Add($"Exception:\n\t{ex.Message}");
            }

            return retVal;
        }
        #endregion

        #region Record Methods
        /// <summary>
        /// Create an empty row to be used with SaveRecord().
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="dataRow">Empty Row from table.</param>
        /// <returns>bool</returns>
        public bool CreateNewRow(string tableName, out DataRow dataRow) 
            => CreateNewRow(tableName, out dataRow, out _);
        /// <summary>
        /// Create an empty row to be used with SaveRecord().
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="dataRow">Empty Row from table.</param>
        /// <param name="respStatus">Status with all errors and all warnings.</param>
        /// <returns>bool</returns>
        public bool CreateNewRow(string tableName, out DataRow dataRow, out CJRespInfo respStatus)
        {
            bool retVal = false;
            respStatus = new CJRespInfo();
            if (!Intialized(ref respStatus))
            {
                dataRow = null;
                return retVal;
            }

            if (!TableExists(tableName, out respStatus))
            {
                respStatus.Errors.Add($"{Constants.TABLE_MISSING}\n\t{nameof(tableName)}");
                dataRow = new DataTable(Guid.NewGuid().ToString()).NewRow();
            }
            else
            {
                dataRow = mr_dataSet.Tables[tableName].NewRow();
                retVal = true;
            }

            return retVal;
        }
        /// <summary>
        /// Save new record.
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="dataRow">DataRow created by CreateNewRow()</param>
        /// <returns>bool</returns>
        public bool SaveRecord(string tableName, DataRow dataRow)
            => SaveRecord(tableName, dataRow, string.Empty, out _, out _);
        /// <summary>
        /// Save new record.
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="dataRow">DataRow created by CreateNewRow()</param>
        /// <param name="respStatus">Status with all errors and all warnings.</param>
        /// <returns>bool</returns>
        public bool SaveRecord(string tableName, DataRow dataRow, out CJRespInfo respStatus)
            => SaveRecord(tableName, dataRow, string.Empty, out _, out respStatus);
        /// <summary>
        /// Save new record.
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="dataRow">DataRow created by CreateNewRow()</param>
        /// <param name="affectCount">Record count that were added or updated.</param>
        /// <returns>bool</returns>
        public bool SaveRecord(string tableName, DataRow dataRow, out int affectCount)
            => SaveRecord(tableName, dataRow, string.Empty, out affectCount, out _);
        /// <summary>
        /// Add or update record.  Where should be String.Empty if creating a record.<br/>
        /// NOTE: dataRow doesn't require all columns during an update, only the columns to modify.<br/>
        /// If record exists and column ModifiedDate exists, ModifiedDate will be overwritten with current time based on UseUTCDate
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="dataRow">Empty Row from table.</param>
        /// <param name="where">Search and updated specified records.</param>
        /// <returns>bool</returns>
        public bool SaveRecord(string tableName, DataRow dataRow, string where)
            => SaveRecord(tableName, dataRow, where, out _, out _);
        /// <summary>
        /// Add or update record.  Where should be String.Empty if creating a record.<br/>
        /// NOTE: dataRow doesn't require all columns during an update, only the columns to modify.<br/>
        /// If record exists and column ModifiedDate exists, ModifiedDate will be overwritten with current time based on UseUTCDate
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="dataRow">Empty Row from table.</param>
        /// <param name="where">Search and updated specified records.</param>
        /// <param name="affectCount">Record count that were added or updated.</param>
        /// <returns>bool</returns>
        public bool SaveRecord(string tableName, DataRow dataRow, string where, out int affectCount)
            => SaveRecord(tableName, dataRow, where, out affectCount, out _);
        /// <summary>
        /// Add or update record.  Where should be String.Empty if creating a record.<br/>
        /// NOTE: dataRow doesn't require all columns during an update, only the columns to modify.<br/>
        /// If record exists and column ModifiedDate exists, ModifiedDate will be overwritten with current time based on UseUTCDate
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="dataRow">Empty Row from table.</param>
        /// <param name="where">Search and updated specified records.</param>
        /// <param name="respStatus">Status with all errors and all warnings.</param>
        /// <returns>bool</returns>
        public bool SaveRecord(string tableName, DataRow dataRow, string where, out CJRespInfo respStatus)
            => SaveRecord(tableName, dataRow, where, out _, out respStatus);
        /// <summary>
        /// Add or update record.  Where should be String.Empty if creating a record.<br/>
        /// NOTE: dataRow doesn't require all columns during an update, only the columns to modify.<br/>
        /// If record exists and column ModifiedDate exists, ModifiedDate will be overwritten with current time based on UseUTCDate
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="dataRow">Empty Row from table.</param>
        /// <param name="where">Search and updated specified records.</param>
        /// <param name="affectCount">Record count that were added or updated.</param>
        /// <param name="respStatus">Status with all errors and all warnings.</param>
        /// <returns>bool</returns>
        public bool SaveRecord(string tableName, DataRow dataRow, string where, out int affectCount, out CJRespInfo respStatus)
        {
            bool retVal = false;
            bool addRecord = false;
            affectCount = 0;
            respStatus = new CJRespInfo();
            if (!Intialized(ref respStatus))
                return retVal;

            where = Utils.CleanQuery(where);
            DataRow[] allEffectedData;

            if (!TableExists(tableName, out respStatus))
                respStatus.Errors.Add($"{Constants.TABLE_MISSING}\n\t{nameof(tableName)}");
            else
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(where))
                    {
                        if (GetRecords(tableName, where, string.Empty, out allEffectedData, out respStatus))
                        {
                            affectCount = allEffectedData.Length;
                            foreach (DataRow dr in allEffectedData)
                            {
                                foreach (DataColumn dc in dataRow.Table.Columns)
                                    dr[dc.ColumnName] = dataRow[dc.ColumnName];

                                if (dr.Table.Columns.Contains(Constants.COLUMN_MODIFIED_DATE))
                                    dr[Constants.COLUMN_MODIFIED_DATE] = CurrentDateTime;
                            }
                        }
                        else
                            addRecord = true;
                    }
                    else
                        addRecord = true;

                    if (addRecord)
                    {
                        if (dataRow.Table.Columns.Contains(Constants.COLUMN_CREATED_DATE) &&
                            dataRow[Constants.COLUMN_CREATED_DATE].GetType() == typeof(DBNull))
                            dataRow[Constants.COLUMN_CREATED_DATE] = CurrentDateTime;

                        if (dataRow.Table.Columns.Contains(Constants.COLUMN_MODIFIED_DATE) &&
                            dataRow[Constants.COLUMN_MODIFIED_DATE].GetType() == typeof(DBNull))
                            dataRow[Constants.COLUMN_MODIFIED_DATE] = CurrentDateTime;

                        mr_dataSet.Tables[tableName].Rows.Add(dataRow);
                        affectCount = 1;
                    }

                    retVal = Flush(out respStatus);
                }
                catch (Exception ex)
                {
                    respStatus.Errors.Add($"Exception:\n\t{ex.Message}");
                }
            }

            return retVal;
        }
        /// <summary>
        /// Search for records.
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="where">Records to search for.</param>
        /// <param name="orderBy">Column and Sort Order.</param>
        /// <param name="dataRows">All DataRows to return.</param>
        /// <param name="respStatus">Status with all errors and all warnings.</param>
        /// <returns>bool</returns>
        public bool GetRecords(string tableName, string where, string orderBy, out DataRow[] dataRows, out CJRespInfo respStatus)
        {
            bool retVal = false;
            dataRows = Array.Empty<DataRow>();
            respStatus = new CJRespInfo();
            if (!Intialized(ref respStatus))
                return retVal;

            if (!TableExists(tableName, out respStatus))
                respStatus.Errors.Add($"{Constants.TABLE_MISSING}\n\t{nameof(tableName)}");
            else
            {
                try
                {
                    where = Utils.CleanQuery(where);
                    orderBy = Utils.CleanQuery(orderBy);

                    dataRows = mr_dataSet.Tables[tableName].Select(where, orderBy);
                    retVal = true;
                }
                catch (Exception ex)
                {
                    respStatus.Errors.Add($"Exception:\n\t{ex.Message}");
                }
            }

            return retVal;
        }
        /// <summary>
        /// Delete one or more records.
        /// </summary>
        /// <param name="tableName">Table represented</param>
        /// <param name="where">Records to delete.</param>
        /// <param name="affectCount">Record count that were deleted.</param>
        /// <param name="respStatus">Status with all errors and all warnings.</param>
        /// <returns>bool</returns>
        public bool DeleteRecords(string tableName, string where, out int affectCount, out CJRespInfo respStatus)
        {
            bool retVal = false;
            affectCount = 0;
            respStatus = new CJRespInfo();
            if (!Intialized(ref respStatus))
                return retVal;

            if (!TableExists(tableName, out respStatus))
                respStatus.Errors.Add($"{Constants.TABLE_MISSING}\n\t{nameof(tableName)}");
            else
            {
                try
                {
                    where = Utils.CleanQuery(where);
                    
                    DataRow[] drs = mr_dataSet.Tables[tableName].Select(where);
                    affectCount = drs.Length;
                    foreach (DataRow dr in drs)
                        mr_dataSet.Tables[tableName].Rows.Remove(dr);

                    if (drs.Length > 0)
                        retVal = Flush(out respStatus);
                    else
                        retVal = true;
                }
                catch (Exception ex)
                {
                    affectCount = 0;
                    respStatus.Errors.Add($"Exception:\n\t{ex.Message}");
                }
            }

            return retVal;
        }
        #endregion

        #region Encryption Methods
        /// <summary>
        /// Converts string to SecureString and will encrypt if needed.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>SecureString</returns>
        public static SecureString SecuredString(string text)
        {
            return Utils.GetCredObj(text).SecurePassword;
        }
        /// <summary>
        /// Converts SecureString to NetworkCredential, that can then pull Password or SecureString.<br/>
        /// <code>
        /// var clearPass = SecuredString(secureString).Password;<br/>
        /// var secureString = SecuredString(secureString).SecurePassword;
        /// </code>
        /// </summary>
        /// <param name="text"></param>
        /// <returns>NetworkCredential</returns>
        public static NetworkCredential SecuredString(SecureString text)
        {
            return Utils.GetCredObj(text);
        }
        #endregion

        #region IO Methods
        /// <summary>
        /// Just in case, Flush() isn't noticed, SaveToDisk calls Flush()
        /// </summary>
        /// <returns>bool</returns>
        public bool SaveToDisk() 
            => SaveToDisk(out _);
        /// <summary>
        /// Just in case, Flush() isn't noticed, SaveToDisk calls Flush()
        /// </summary>
        /// <param name="respStatus">Status with all errors and all warnings.</param>
        /// <returns>bool</returns>
        public bool SaveToDisk(out CJRespInfo respStatus)
        {
            return Flush(out respStatus);
        }
        /// <summary>
        /// Saves DataSet, all Tables, and all Records to disk along with table schema.
        /// </summary>
        /// <returns>bool</returns>
        public bool Flush() 
            => Flush(out _);
        /// <summary>
        /// Saves DataSet, all Tables, and all Records to disk along with table schema.
        /// </summary>
        /// <param name="respStatus">Status with all errors and all warnings.</param>
        /// <returns>bool</returns>
        public bool Flush(out CJRespInfo respStatus)
        {
            respStatus = new CJRespInfo();
            if (!Intialized(ref respStatus))
                return false;

            if (mr_jsonConverter.FromDataSet(mr_dataSet, out JsonDataSet jds, out respStatus))
                return JsonFile.SaveToFile(jds, out respStatus);

            return false;
        }
        #endregion
    }
}