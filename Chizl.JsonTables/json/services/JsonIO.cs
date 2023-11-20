using Newtonsoft.Json;
using System;
using System.IO;

namespace Chizl.JsonTables.json
{
    internal class JsonIO
    {
        private string m_FileName;
        private static readonly object m_Locker = new object();

        internal Exception Error { get; set; } = new Exception(null);

        internal bool FileExists { get { return m_FileName == null ? false : File.Exists(m_FileName); } }

        internal JsonIO(string fileName) => m_FileName = string.IsNullOrWhiteSpace(fileName)
                ? throw new ArgumentException(Constants.ARGS_MISSING, nameof(fileName))
                : fileName;

        private void ClearError() => Error = new Exception(null);

        internal bool SaveToFile(JsonDataSet jsonDataSet)
        {
            bool retVal = false;

            lock (m_Locker)
            {
                ClearError();

                try
                {
                    if (jsonDataSet == null)
                        Error = new ArgumentException(Constants.ARGS_MISSING, nameof(jsonDataSet));
                    else
                    {
                        string json = JsonConvert.SerializeObject(jsonDataSet, Formatting.Indented);
                        File.WriteAllText(m_FileName, json);
                        retVal = true;
                    }
                }
                catch (Exception ex)
                {
                    Error = ex;
                }
            }

            return retVal;
        }
        internal bool LoadFile(out JsonDataSet jsonDataSet)
        {
            return LoadFile(string.Empty, out jsonDataSet);
        }
        internal bool LoadFile(string dataSetName, out JsonDataSet jsonDataSet)
        {
            bool retVal = false;

            if (string.IsNullOrWhiteSpace(dataSetName))
                dataSetName = Constants.DEFAULT_LOADING;

            lock ((m_Locker))
            {
                ClearError();
                jsonDataSet = new JsonDataSet(dataSetName);

                try
                {
                    if (string.IsNullOrWhiteSpace(m_FileName))
                        Error = new ArgumentException(Constants.ARGS_MISSING, nameof(m_FileName));
                    else if (!FileExists)
                        Error = new FileNotFoundException(Constants.FILE_MISSING, nameof(m_FileName));
                    else
                    {
                        string json = File.ReadAllText(m_FileName);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            jsonDataSet = (JsonDataSet)JsonConvert.DeserializeObject(json, typeof(JsonDataSet));
                            if (jsonDataSet != null)
                                retVal = true;
                            else
                                Error = new FormatException(nameof(json));
                        }
                        else
                            Error = new FileLoadException(Constants.DATA_MISSING, nameof(m_FileName));
                    }
                }
                catch (Exception ex)
                {
                    //catch the unexpected
                    Error = ex;
                }
            }

            return retVal;
        }
    }
}
