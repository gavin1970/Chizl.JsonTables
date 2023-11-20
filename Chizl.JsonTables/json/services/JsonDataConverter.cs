using Chizl.Crypto.aes;
using System;
using System.ComponentModel;
using System.Data;
using System.Net;
using System.Security;

namespace Chizl.JsonTables.json
{
    internal class JsonDataConverter : JsonDataSet
    {
        internal JsonDataConverter(string dataSetName, SecureString encSalt) : base(dataSetName) { this.EncSalt = Utils.GetCredObj(encSalt); }

        #region Internal Properties
        internal Exception LastError { get; private set; }
        internal WarningException LastWarning { get; private set; }
        internal NetworkCredential EncSalt { get; }
        #endregion

        #region Private Methods
        /// <summary>
        /// All data to json data rows must be a string.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        private DataTable RowData(DataTable dt)
        {
            DataTable retVal = new DataTable(dt.TableName);

            //all data to json data row, must be a string
            foreach (DataColumn dc in dt.Columns)
                retVal.Columns.Add(dc.ColumnName, typeof(string));

            foreach (DataRow row in dt.Rows)
            {
                DataRow dTrow = retVal.NewRow();
                foreach (DataColumn dc in dt.Columns)
                {
                    if (dc.DataType == typeof(SecureString))
                    {
                        var secString = Utils.GetCredObj((SecureString)row[dc.ColumnName]).Password;
                        if (!secString.StartsWith(Constants.SEC_STR_WRAPPER))
                        {
                            using (CryptoAES crypto = new CryptoAES(EncSalt.SecurePassword, CHIZL_ENCODING_TYPE.UTF8Encoding))
                            {
                                if (crypto.EncryptString(secString, out string envVal))
                                    dTrow[dc.ColumnName] = $"{Constants.SEC_STR_WRAPPER}{envVal}";
                                else
                                    dTrow[dc.ColumnName] = secString;
                            }
                        }
                    }
                    else
                    {
                        //lets me sure it's being converted property.
                        if (Utils.ConvertTo<string>(row[dc.ColumnName], out string outputVale))
                            dTrow[dc.ColumnName] = outputVale;
                    }
                }
                retVal.Rows.Add(dTrow);
            }

            return retVal;
        }
        #endregion

        #region Internal Methods
        internal bool DT2JT(DataTable dt, out JsonDataTable jdt)
        {
            bool retVal = false;
            jdt = new JsonDataTable(dt.TableName);

            //JsonSerializerSettings config = new () { ReferenceLoopHandling = ReferenceLoopHandling.Serialize };

            try
            {
                //For secure encrypted columns, it is suggested to use dt.Columns[0].DataType = typeof(SecureString);
                foreach(DataColumn col in dt.Columns)
                    jdt.Schema.Add(new JsonDataColumn(col.ColumnName, col.DataType.FullName));

                //var colSchema = dt.Columns.Cast<DataColumn>().Select(dc => new { dc.ColumnName, DataType = dc.DataType.FullName });
                //jdt.TableSchema = JsonConvert.SerializeObject(colSchema);
                jdt.Table = RowData(dt);

                retVal = true;
            } 
            catch(Exception ex)
            {
                LastError = ex;
            }

            return retVal;
        }
        internal bool JT2DT(JsonDataTable jdt, out DataTable dt)
        {
            bool retVal = false;
            dt = new DataTable(jdt.TableName);

            if (jdt == null)
                throw new ArgumentException(Constants.ARGS_MISSING, nameof(jdt));
            else
            {
                try
                {
                    //reset
                    LastWarning = new WarningException(null);

                    //make sure Scema name exists
                    if (jdt.Schema.Count > 0)
                    {
                        //create a new DataTable.  
                        //Since DataTable couldn't be a reference on ".Columns.Add()",
                        //I had to improvise and use a tmp, then copy to dt at the end.
                        using (DataTable tmp = new DataTable(jdt.TableName))
                        {

                            foreach(JsonDataColumn dcs in jdt.Schema)
                                tmp.Columns.Add(new DataColumn() { ColumnName = dcs.ColumnName, DataType = Type.GetType(dcs.DataType.ToString()) });

                            //DataTable dataOnly = JsonConvert.DeserializeObject<DataTable>(jdt.DataRows);
                            foreach(DataRow dr in jdt.Table.Rows)
                            {
                                //create a new row for new temp table
                                DataRow newDr = tmp.NewRow();
                                foreach(DataColumn dc in tmp.Columns)
                                {
                                    //if column is PI or requires security
                                    if (dc.DataType == typeof(SecureString))
                                    {
                                        //pull data
                                        var text = dr[dc.ColumnName].ToString();
                                        //check to ensure it's branded as Base64 AES
                                        if (text.IndexOf(Constants.SEC_STR_WRAPPER)==-1)
                                            LastWarning = new WarningException($"Column '{dc.ColumnName}' has an invalid secure string: '{text}'.\n'{Constants.SEC_STR_WRAPPER}' was expected at the start of all SecureStrings. Data could possible be invalid.");
                                        else
                                            text = text.Replace(Constants.SEC_STR_WRAPPER, ""); //remove brand
                                            
                                        //lets decrypt from Json field based on salt that was set.
                                        using (CryptoAES crypto = new CryptoAES(EncSalt.SecurePassword, CHIZL_ENCODING_TYPE.UTF8Encoding))
                                        {
                                            //decrypt string
                                            if (crypto.DecryptString(text, out NetworkCredential netCred))
                                                newDr[dc.ColumnName] = netCred.SecurePassword;          //secure data
                                            else
                                                //can't decrypt, so assume it's not encrypted and secure data as is.
                                                newDr[dc.ColumnName] = Utils.GetCredObj(text).SecurePassword; 
                                        }
                                    }
                                    else
                                    {
                                        //This was a bit tricky, but this works perfectly.
                                        //Calling a Generic method with a required "Type" based on a string stating type, within Json.
                                        //Had a method that already converts called "ConverTo", but that was a little more difficult to
                                        //call from Invoke. This wrapper method called "Converts" will call "ConvertTo".
                                        var methName = typeof(Utils).GetMethod("Converts");
                                        //get MethodInfo of Generic Method
                                        var methInfo = methName.MakeGenericMethod(dc.DataType);
                                        //Class that Converts belongs to
                                        Utils obj = new Utils();
                                        //Data to convert as params
                                        object[] args = new object[1] { dr[dc.ColumnName] };
                                        //execute and return new value in Type.
                                        var newVal = methInfo.Invoke(obj, args); 
                                        //set value with correct Type required.
                                        newDr[dc.ColumnName] = newVal;
                                    }
                                }
                                tmp.Rows.Add(newDr);
                            }

                            dt = tmp.Copy();
                        }
                    }

                    retVal = true;
                } 
                catch(Exception ex)
                {
                    LastError = ex;
                }
            }

            return retVal;
        }
        /// <summary>
        /// Loop through tables and convert each for Json setup.
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="jds"></param>
        /// <returns></returns>
        internal bool FromDataSet(DataSet ds, out JsonDataSet jds)
        {
            //set DataSet name
            jds = new JsonDataSet(ds.DataSetName);

            try
            {
                //loop through tables
                foreach (DataTable dt in ds.Tables)
                {
                    //set table schema and convert data to strings
                    if (DT2JT(dt, out JsonDataTable jdt))
                        jds.JsonDataTables.Add(jdt);
                }
            } 
            catch(Exception ex)
            {
                LastError = ex;
            }

            return LastError == null;
        }
        /// <summary>
        /// Loop through Json dataTables and convert to DataSet with all Tables.
        /// </summary>
        /// <param name="jds"></param>
        /// <param name="ds"></param>
        /// <returns></returns>
        internal bool ToDataSet(JsonDataSet jds, out DataSet ds)
        {
            ds = new DataSet();

            try
            {
                //if DataSet name exist in Json
                if (!string.IsNullOrWhiteSpace(jds.DataSetName))
                    ds.DataSetName = jds.DataSetName;
                else
                    ds.DataSetName = $"{Constants.BASE_DATASET_NAME}_{DateTime.UtcNow.Ticks}";

                //loop through Json tables and add them to DataSet
                foreach (JsonDataTable jdt in jds.JsonDataTables)
                {
                    //convert Json table to DataTable
                    if (JT2DT(jdt, out DataTable dt))
                        ds.Tables.Add(dt);
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
            }

            return LastError == null;
        }
        #endregion
    }
}