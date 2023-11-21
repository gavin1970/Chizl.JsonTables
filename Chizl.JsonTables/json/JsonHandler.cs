using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Net;
using System.Security;

namespace Chizl.JsonTables.json
{
    public class JsonHandler
    {
        private readonly DataSet mr_dataSet;
        private readonly JsonDataConverter mr_jsonConverter;

        #region Properties
        #region Public
        public Exception LastError { get; private set; } = new Exception(null);
        public WarningException LastWarning { get; private set; } = new WarningException(null);

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

            mr_jsonConverter = new JsonDataConverter(dataSetName, encSalt);
            mr_dataSet = new DataSet(dataSetName);

            if (!JsonFile.LoadFile(dataSetName, out JsonDataSet jsonDataSet))
            {
                LastError = JsonFile.Error;
                if (LastError.GetType() == typeof(FileNotFoundException))
                {
                    FileExists = false;
                    LastError = new Exception(null);
                }
                else
                    FileExists = true;
            }
            else
                FileExists = true;

            if (jsonDataSet != null
                && !jsonDataSet.DataSetName.Equals("Invalid"))
            {
                if (!mr_jsonConverter.ToDataSet(jsonDataSet, out mr_dataSet))
                {
                    LastError = mr_jsonConverter.LastError;
                    throw LastError;
                }
            }
        }
        #endregion

        #region Column Methods
        /// <summary>
        /// Checks to see if column exists
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public bool ColumnExists(string tableName, string columnName)
        {
            bool retVal = false;

            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    LastError = new ArgumentException(Constants.ARGS_MISSING, nameof(tableName));
                else if (string.IsNullOrWhiteSpace(columnName))
                    LastError = new ArgumentException(Constants.ARGS_MISSING, nameof(columnName));
                else
                {
                    if (mr_dataSet.Tables.Contains(tableName))
                        retVal = mr_dataSet.Tables[tableName].Columns.Contains(columnName);
                    else
                        retVal = false;
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
            }

            return retVal;
        }
        public bool AddColumns(string tableName, DataColumn[] dataColumns)
        {
            bool hadException = false;

            List<string> successList = new List<string>();
            foreach (DataColumn col in dataColumns)
            {
                bool success = AddColumn(tableName, col, out hadException);
                if (!success && hadException)
                    break;
                else if (success)
                    successList.Add(col.ColumnName);
            }

            //back out all that were added.
            if (hadException)
            {
                foreach (string colName in successList)
                    RemoveColumn(tableName, colName);
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
        public bool AddColumn(string tableName, DataColumn dataColumn, out bool hadException)
        {
            bool retVal = false;
            hadException = false;

            try
            {
                if (mr_dataSet == null)
                    LastError = new ArgumentException(Constants.TABLE_MISSING, nameof(tableName));
                else if (dataColumn == null)
                    LastError = new ArgumentException(Constants.ARGS_MISSING, nameof(dataColumn));
                else
                {
                    LastError = new Exception(null);

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
                        LastWarning = new WarningException($"Column '{dataColumn}' already exists in '{tableName}'.");
                }
            }
            catch (Exception ex)
            {
                hadException = true;
                LastError = ex;
            }

            return retVal;
        }
        /// <summary>
        /// Will remove a column from a table, if it exists.
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public bool RemoveColumn(string tableName, string columnName)
        {
            bool retVal = false;

            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    LastError = new ArgumentException(Constants.ARGS_MISSING, nameof(tableName));
                else if (string.IsNullOrWhiteSpace(columnName))
                    LastError = new ArgumentException(Constants.ARGS_MISSING, nameof(columnName));
                else if (mr_dataSet.Tables.Contains(tableName)
                    && mr_dataSet.Tables[tableName].Columns.Contains(columnName))
                {
                    mr_dataSet.Tables[tableName].Columns.Remove(columnName);
                    retVal = true;
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
            }

            return retVal;
        }
        /// <summary>
        /// Return if column is secured or not.
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public bool IsSecured(string tableName, string columnName)
        {
            bool retVal = false;

            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    LastError = new ArgumentException(Constants.ARGS_MISSING, nameof(tableName));
                else if (string.IsNullOrWhiteSpace(columnName))
                    LastError = new ArgumentException(Constants.ARGS_MISSING, nameof(columnName));
                else if (mr_dataSet.Tables.Contains(tableName))
                {
                    if (mr_dataSet.Tables[tableName].Columns.Contains(columnName))
                        retVal = mr_dataSet.Tables[tableName].Columns[columnName].DataType == typeof(SecureString);
                    else
                        LastError = new ArgumentException(Constants.COLUMN_MISSING, nameof(columnName));
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
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
        public T GetColumn<T>(DataRow dr, string columnName, T defaultValue = default)
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
        public bool TableExists(string tableName)
        {
            bool retVal = false;

            try
            {
                if (tableName == null)
                    LastError = new ArgumentException(Constants.ARGS_MISSING, nameof(tableName));
                else
                    retVal = mr_dataSet.Tables.Contains(tableName);
            }
            catch (Exception ex)
            {
                LastError = ex;
            }

            return retVal;
        }
        public bool GetTable(string tableName, out DataTable dataTable)
        {
            bool retVal = false;
            dataTable = new DataTable(tableName);

            try
            {
                if (tableName == null)
                    LastError = new ArgumentException(Constants.ARGS_MISSING, nameof(tableName));
                else if (!mr_dataSet.Tables.Contains(tableName))
                    LastError = new ArgumentException(Constants.TABLE_MISSING, nameof(tableName));
                else
                {
                    dataTable = mr_dataSet.Tables[tableName];
                    retVal = true;
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
            }

            return retVal;
        }
        public bool AddTable(DataTable dataTable, bool updateIfExist = false)
        {
            bool retVal = false;

            try
            {
                if (dataTable == null)
                    LastError = new ArgumentException(Constants.ARGS_MISSING, nameof(dataTable));
                else
                {
                    if (mr_dataSet.Tables.Contains(dataTable.TableName))
                    {
                        if (updateIfExist)
                            mr_dataSet.Tables.Remove(dataTable.TableName);
                        else
                            throw new Exception(Constants.TABLE_MISSING);
                    }

                    mr_dataSet.Tables.Add(dataTable);
                    retVal = true;
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
            }

            return retVal;
        }
        public bool UpdateTable(DataTable dataTable) => AddTable(dataTable, true);
        public bool RemoveTable(string tableName)
        {
            bool retVal = false;

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
                LastError = ex;
            }

            return retVal;
        }
        #endregion

        #region Record Methods
        public bool CreateRecord(string tableName, out DataRow dataRow)
        {
            bool retVal = false;

            if (!TableExists(tableName))
            {
                LastError = new ArgumentException(Constants.TABLE_MISSING, nameof(tableName));
                dataRow = new DataTable(Guid.NewGuid().ToString()).NewRow();
            }
            else
            {
                dataRow = mr_dataSet.Tables[tableName].NewRow();
                retVal = true;
            }

            return retVal;
        }
        public bool SaveRecord(string tableName, DataRow dataRow)
        {
            return SaveRecord(tableName, dataRow, string.Empty, out int _);
        }
        public bool SaveRecord(string tableName, DataRow dataRow, out int affectCount)
        {
            return SaveRecord(tableName, dataRow, string.Empty, out affectCount);
        }
        public bool SaveRecord(string tableName, DataRow dataRow, string where, out int affectCount)
        {
            bool retVal = false;
            bool addRecord = false;
            affectCount = 0;

            where = Utils.CleanQuery(where);

            if (!TableExists(tableName))
                LastError = new ArgumentException(Constants.TABLE_MISSING, nameof(tableName));
            else
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(where))
                    {
                        if (GetRecords(tableName, where, string.Empty, out DataRow[] retData))
                        {
                            affectCount = retData.Length;
                            foreach (DataRow dr in retData)
                            {
                                foreach (DataColumn dc in mr_dataSet.Tables[tableName].Columns)
                                {
                                    dr[dc.ColumnName] = dataRow[dc.ColumnName];
                                }
                            }
                        }
                        else
                            addRecord = true;
                    }
                    else
                        addRecord = true;

                    if (addRecord)
                        mr_dataSet.Tables[tableName].Rows.Add(dataRow);

                    retVal = Flush();
                }
                catch (Exception ex)
                {
                    LastError = ex;
                }
            }

            return retVal;
        }
        public bool GetRecords(string tableName, string where, string orderBy, out DataRow[] dataRows)
        {
            bool retVal = false;
            dataRows = Array.Empty<DataRow>();

            if (!TableExists(tableName))
                LastError = new Exception($"Table '{tableName}' doesn't exist.");
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
                    LastError = ex;
                }
            }

            return retVal;
        }
        public bool DeleteRecords(string tableName, string where, out int affectCount)
        {
            bool retVal = false;
            affectCount = 0;

            if (!TableExists(tableName))
                LastError = new Exception($"Table '{tableName}' doesn't exist.");
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
                        retVal = Flush();
                    else
                        retVal = true;
                }
                catch (Exception ex)
                {
                    affectCount = 0;
                    LastError = ex;
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
        public bool SaveToDisk()
        {
            return Flush();
        }
        /// <summary>
        /// Saves DataSet, all Tables, and all Records to disk along with table schema.
        /// </summary>
        /// <returns></returns>
        public bool Flush()
        {
            if (mr_jsonConverter.FromDataSet(mr_dataSet, out JsonDataSet jds))
                return JsonFile.SaveToFile(jds);

            LastError = mr_jsonConverter.LastError;
            return false;
        }
        #endregion
    }
}