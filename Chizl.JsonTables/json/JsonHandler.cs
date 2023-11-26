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
        public List<CJRespInfo> RespInfo { get; private set; } = new List<CJRespInfo>();

        public bool FileExists { get; private set; } = false;
        #endregion
        
        #region Private
        private JsonIO JsonFile { get; }
        #endregion
        #endregion

        #region Constructor
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
        }
        #endregion

        #region Column Methods
        /// <summary>
        /// Checks to see if column exists
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public bool ColumnExists(string tableName, string columnName, out CJRespInfo respStatus)
        {
            bool retVal = false;
            RespInfo = new List<CJRespInfo>();
            respStatus = new CJRespInfo();

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
        public bool AddColumns(string tableName, DataColumn[] dataColumns, out CJRespInfo respStatus)
        {
            bool hadException = false;
            respStatus = new CJRespInfo();

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
        /// <param name="tableName"></param>
        /// <param name="dataColumn"></param>
        /// <param name="hadException"></param>
        /// <returns></returns>
        public bool AddColumn(string tableName, DataColumn dataColumn, out CJRespInfo respStatus)
        {
            bool retVal = false;
            respStatus = new CJRespInfo();

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
        /// <param name="columnName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public bool RemoveColumn(string tableName, string columnName, out CJRespInfo respStatus)
        {
            bool retVal = false;
            respStatus = new CJRespInfo();

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
        /// <param name="columnName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public bool IsSecured(string tableName, string columnName, out CJRespInfo respStatus)
        {
            bool retVal = false;
            respStatus = new CJRespInfo();

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
        /// <param name="columnName"></param>
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
        public bool TableExists(string tableName, out CJRespInfo respStatus)
        {
            bool retVal = false;
            respStatus = new CJRespInfo();

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
        public bool GetTable(string tableName, out DataTable dataTable, out CJRespInfo respStatus)
        {
            bool retVal = false;
            dataTable = new DataTable(tableName);
            respStatus = new CJRespInfo();

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
        public bool AddTable(DataTable dataTable, out CJRespInfo respStatus, bool replaceIfExists = false)
        {
            bool retVal = false;
            respStatus = new CJRespInfo();

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
                    
                    if(!respStatus.HasErrorOrWarnings)
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
        public bool UpdateTable(DataTable dataTable, out CJRespInfo respStatus) => AddTable(dataTable, out respStatus, true);
        public bool RemoveTable(string tableName, out CJRespInfo respStatus)
        {
            bool retVal = false;
            respStatus = new CJRespInfo();

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
        public bool CreateRecord(string tableName, out DataRow dataRow, out CJRespInfo respStatus)
        {
            bool retVal = false;

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
        public bool SaveRecord(string tableName, DataRow dataRow, out CJRespInfo respStatus)
        {
            return SaveRecord(tableName, dataRow, string.Empty, out int _, out respStatus);
        }
        public bool SaveRecord(string tableName, DataRow dataRow, out int affectCount, out CJRespInfo respStatus)
        {
            return SaveRecord(tableName, dataRow, string.Empty, out affectCount, out respStatus);
        }
        public bool SaveRecord(string tableName, DataRow dataRow, string where, out int affectCount, out CJRespInfo respStatus)
        {
            bool retVal = false;
            bool addRecord = false;
            affectCount = 0;
            respStatus = new CJRespInfo();

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
                                {
                                    dr[dc.ColumnName] = dataRow[dc.ColumnName];
                                }

                                if (dr.Table.Columns.Contains("ModifiedDate"))
                                    dr["ModifiedDate"] = DateTime.UtcNow;
                            }
                        }
                        else
                            addRecord = true;
                    }
                    else
                        addRecord = true;

                    if (addRecord)
                    {
                        if (dataRow.Table.Columns.Contains("CreatedDate"))
                            dataRow["CreatedDate"] = DateTime.UtcNow;
                        if (dataRow.Table.Columns.Contains("ModifiedDate"))
                            dataRow["ModifiedDate"] = DateTime.UtcNow;

                        mr_dataSet.Tables[tableName].Rows.Add(dataRow);
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
        public bool GetRecords(string tableName, string where, string orderBy, out DataRow[] dataRows, out CJRespInfo respStatus)
        {
            bool retVal = false;
            dataRows = Array.Empty<DataRow>();
            respStatus = new CJRespInfo();

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
        public bool DeleteRecords(string tableName, string where, out int affectCount, out CJRespInfo respStatus)
        {
            bool retVal = false;
            affectCount = 0;
            respStatus = new CJRespInfo();

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
        /// <param name="encrypted"></param>
        /// <returns></returns>
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
        /// <returns></returns>
        public static NetworkCredential SecuredString(SecureString text)
        {
            return Utils.GetCredObj(text);
        }
        #endregion

        #region IO Methods
        /// <summary>
        /// Just in case, Flush() isn't noticed, SaveToDisk calls Flush()
        /// </summary>
        /// <returns></returns>
        public bool SaveToDisk(out CJRespInfo respStatus)
        {
            return Flush(out respStatus);
        }
        /// <summary>
        /// Saves DataSet, all Tables, and all Records to disk along with table schema.
        /// </summary>
        /// <returns></returns>
        public bool Flush(out CJRespInfo respStatus)
        {
            if (mr_jsonConverter.FromDataSet(mr_dataSet, out JsonDataSet jds, out respStatus))
                return JsonFile.SaveToFile(jds, out respStatus);

            return false;
        }
        #endregion
    }
}