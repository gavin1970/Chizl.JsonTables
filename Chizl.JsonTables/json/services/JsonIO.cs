using Newtonsoft.Json;
using System;
using System.IO;

namespace Chizl.JsonTables.json
{
    internal class JsonIO
    {
        private readonly string m_FileName;
        private static readonly object m_Locker = new object();

        internal bool FileExists { get { return m_FileName != null && File.Exists(m_FileName); } }

        internal JsonIO(string fileName) => m_FileName = string.IsNullOrWhiteSpace(fileName)
                ? throw new ArgumentException(Constants.ARGS_MISSING, nameof(fileName))
                : fileName;

        internal bool SaveToFile(JsonDataSet jsonDataSet, out CJRespInfo respStatus)
        {
            bool retVal = false;
            respStatus = new CJRespInfo();

            lock (m_Locker)
            {
                try
                {
                    if (jsonDataSet == null)
                        respStatus.Errors.Add($"{Constants.ARGS_MISSING}\n\t{nameof(jsonDataSet)}");
                    else
                    {
                        string json = JsonConvert.SerializeObject(jsonDataSet, Formatting.Indented);

                        //using (FileStream fsOverwrite = new FileStream(m_FileName, FileMode.Create))
                        //using (StreamWriter writer = new StreamWriter(fsOverwrite))
                        //    writer.WriteLine(json);
                       
                        File.WriteAllText(m_FileName, json);
                        retVal = true;
                    }
                }
                catch (Exception ex)
                {
                    respStatus.Errors.Add($"Exception:\n\t{ex.Message}");
                }
            }

            return retVal;
        }
        internal bool LoadFile(out JsonDataSet jsonDataSet, out CJRespInfo respStatus) => LoadFile(string.Empty, out jsonDataSet, out respStatus);
        internal bool LoadFile(string dataSetName, out JsonDataSet jsonDataSet, out CJRespInfo respStatus)
        {
            bool retVal = false;
            respStatus = new CJRespInfo();

            if (string.IsNullOrWhiteSpace(dataSetName))
                dataSetName = Constants.DEFAULT_DATASET;

            lock (m_Locker)
            {
                jsonDataSet = new JsonDataSet(dataSetName);

                try
                {
                    if (string.IsNullOrWhiteSpace(m_FileName))
                        respStatus.Errors.Add($"{Constants.ARGS_MISSING}\n\t{nameof(m_FileName)}");
                    else if (!FileExists)
                        respStatus.Warnings.Add($"{Constants.FILE_MISSING}\n\t{m_FileName}");
                    else
                    {
                        string json = File.ReadAllText(m_FileName);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            JsonDataSet allDataSetData = JsonConvert.DeserializeObject<JsonDataSet>(json);
                            if (allDataSetData == null)
                                respStatus.Errors.Add($"{Constants.JSON_FORMAT_EXCEPTION}\n\t{m_FileName}"); 
                            else if (allDataSetData.DataSetName.Equals(dataSetName))
                            {
                                jsonDataSet = allDataSetData;
                                retVal = true;
                            }
                            else
                                respStatus.Errors.Add($"{Constants.DATASET_MISSING}\n\t{dataSetName}");
                        }
                        else
                            respStatus.Warnings.Add($"{Constants.DATA_MISSING}\n\t{m_FileName}");
                    }
                }
                catch (Exception ex)
                {
                    //catch the unexpected
                    respStatus.Errors.Add($"Exception:\n\t{ex.Message}");
                }
            }

            return retVal;
        }
    }
}
