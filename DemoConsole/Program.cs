using System.Data;
using System.Net;
using System.Security;
using Chizl.JsonTables.json;
using Chizl.Crypto.aes;

internal class Program
{
    private enum ColNames
    {
        ID,
        Name,
        CreatedDate,
        ModifiedDate,
        Password
    }
    
    const string fileName = "./dataFile.dat";
    const string dataSetName = "DemoDataSet";
    const string securedFieldValue1 = "MyPass";
    const string securedFieldValue2 = "MyNewPass";
    const string tableName1 = "First_DemoTable";
    const string tableName2 = "Second_DemoTable";

    //byte[] bytes = Encoding.ASCII.GetBytes("YU7icKHkVp5aARqK");
    //foreach(byte c in bytes)
    //    Console.Write($"{c}, ");
    static readonly byte[] byteSec = new byte[] { 89, 85, 55, 105, 99, 75, 72, 107, 86, 112, 53, 97, 65, 82, 113, 75 };
    static readonly SecureString piSecureString = new();
    static readonly Guid testID = Guid.NewGuid();
        
    public static JsonHandler m_JsonProcessing = new();

    static void Main()
    {
        Heading($"Chizl.JsonTables Demo Console: DataFile: {fileName}", true);

        //Other way to create SecureString:
        //  piSecureString = JsonHandler.SecuredString("YU7icKHkVp5aARqK");

        //load PI SecureString based on byte array.  This could be input and easy creation as it's
        //the Salt for AES Encryption for SecureString columns in DataTables.
        foreach (byte b in byteSec)
            piSecureString.AppendChar(Convert.ToChar(b));

        // Connect and load existing dataset or prepare new one.
        // piSecureString is only used on fields that are SecuredString column types.
        // UseUTCDate default is true.  This is used if CreatedDate and ModifiedDate columns exists.
        m_JsonProcessing = new(fileName, dataSetName, piSecureString) { UseUTCDate = true };

        CreateTable(tableName1, false);                     //create new table (1)
        CreateTable(tableName2, false);                     //create new table (2)

        CreateRecords(tableName1, true, testID);            //create a new record (1)
        CreateRecords(tableName1, false, testID);           //Will FAIL because ID should be Unique based on DataTable
        CreateRecords(tableName1, false, Guid.Empty);       //create a second record (1a)
        CreateRecords(tableName2, true, Guid.Empty);        //create a new record (only 1 for table 2)

        bool isSecured = m_JsonProcessing.IsSecured(tableName1, ColNames.Password.ToString(), out CJRespInfo respStatus);
        if(respStatus.HasErrors)
        {
            PressAnyKey(respStatus.LastErrorMessage);
            return;
        }

        Guid guid1 = ReadRecords(tableName1, true);         //read all records, get first guid back for future select
        GetRecord(tableName1, guid1, true);                 //pull one record by id (1)
        
        UpdateRecord(tableName1, guid1, true);              //change 1 record by id (1)
        ReadRecords(tableName1, true);                      //view all records to see the 1 changed record (1)

        Guid guid2 = ReadRecords(tableName2, true);         //read all records, get first guid back for future select
        GetRecord(tableName2, guid2, true);                 //pull one record by id

        UpdateRecord(tableName2, guid2, true);              //change 1 record by id
        ReadRecords(tableName2, true);                      //view all records to see the 1 changed record

        DeleteRecord(tableName1, guid1, true);              //delete record
        ReadRecords(tableName1, true);                      //view all records to see 1 record was deleted

        DeleteRecord(tableName2, guid2, true);              //delete record
        ReadRecords(tableName2, true);                      //view all records to see 0 records exist

        DeleteTable(tableName1, true);                      //delete table
        DeleteTable(tableName2, true);                      //delete table

        CrtypoExample(true);                                //shows how crypto is used inside and can be used by itself.

        //clean up
        piSecureString.Dispose();
    }

    #region Table Examples
    static void CreateTable(string tableName, bool clearConsole)
    {
        Heading($"Create Table: '{tableName}'", clearConsole);

        bool success = false;
        string msg;

        //validate table exists
        if (!m_JsonProcessing.TableExists(tableName, out CJRespInfo respStatus))
        {
            // Table Defs 
            List<DataColumn> allCols = new() {
                new($"{ColNames.ID}", typeof(Guid)) { Unique=true },
                new($"{ColNames.Name}", typeof(string)),
                new($"{ColNames.CreatedDate}", typeof(DateTime)),   //internally auto-add, on insert of new record.
                new($"{ColNames.ModifiedDate}", typeof(DateTime)),  //internally auto-update with UTC Date/Time based on Name of colnum, "ModifiedDate"
                new($"{ColNames.Password}", typeof(SecureString))   //secured data.
            };

            respStatus = new CJRespInfo();
            if (tableName.Equals(tableName1))
            {
                //Create columns by passing the whole array.  Good thing to know, by using   AddColumns(),
                //If an error occurs, it will remove successfully added columns.  If you use AddColumn(), you must remove anything prior on your own.
                if (m_JsonProcessing.AddColumns(tableName, allCols.ToArray(), out respStatus))
                {
                    if (m_JsonProcessing.Flush(out respStatus))
                        Console.WriteLine($"Success, Table '{tableName}' created with ALL columns at once.\nExpecting the next to have warnings on all columns since they already exist.\n---------------------------");
                    else
                    {
                        if (respStatus.HasErrors)
                            Console.WriteLine($"Exception occured during Flush() for ALL columns to table '{tableName}':\n\t{respStatus.AllErrorMessages}");

                        //could have errors and warnings, which is why this isn't an "else if"
                        if (respStatus.HasWarnings)
                            Console.WriteLine($"Warnings occured during Flush() for ALL columns to table '{tableName}':\n\t{respStatus.AllWarningMessages}");
                    }
                }
                else
                {
                    if (respStatus.HasErrors)
                        Console.WriteLine($"Exception occured adding ALL columns to table '{tableName}':\n\t{respStatus.AllErrorMessages}");

                    //could have errors and warnings, which is why this isn't an "else if"
                    if (respStatus.HasWarnings)
                        Console.WriteLine($"Exception occured adding ALL columns to table '{tableName}':\n\t{respStatus.AllWarningMessages}");
                }
            }

            //accept warnings, but not errors
            if (respStatus.Status != CJ_RESP_STATUS.Error)
            {
                // Create columns 1 by 1
                foreach (DataColumn col in allCols)
                {
                    success = m_JsonProcessing.AddColumn(tableName, col, out respStatus);
                    //looking for 'hadException' because if column already exists, it's
                    //not an exception, but not successfully added.
                    if (success)
                        Console.WriteLine($"Successfully added Column '{col.ColumnName}' to table '{tableName}'.");
                    else
                    {
                        if (respStatus.HasErrors)
                        {
                            Console.WriteLine($"Exception occured adding columns table:\n\t{respStatus.AllErrorMessages}");
                            //failed to add a single column, time to exit.
                            break;
                        }
                        
                        //could have errors and warnings, which is why this isn't an "else if"
                        if (respStatus.HasWarnings)
                            Console.WriteLine(respStatus.AllWarningMessages);  //display why column wasn't added, if message exists.
                    }
                }
            }

            // Save to Disk 
            if (success) success = m_JsonProcessing.Flush(out respStatus);

            // Display success of failure 
            msg = success ? 
                $"Success, Table '{tableName}' created." :
                    respStatus.HasErrors ?
                        $"See above errors." :
                        $"See above warnings.";
        }
        else
            msg = $"Table '{tableName}' already exists.";

        PressAnyKey(msg);
    }
    static void DeleteTable(string tableName, bool clearConsole)
    {
        Heading($"Drop Table Example: '{tableName}'", clearConsole);
        string msg;

        if (m_JsonProcessing.TableExists(tableName, out CJRespInfo respStatus))
        {
            if (m_JsonProcessing.RemoveTable(tableName, out respStatus))
            {
                msg = $"Success, Table '{tableName}' removed.";

                if (!m_JsonProcessing.Flush(out respStatus))
                {
                    if (respStatus.HasErrors)
                        msg = $"Exception:\n\t{respStatus.AllErrorMessages}";
                    else if (respStatus.HasWarnings)
                        msg = $"Warnings:\n\t{respStatus.AllWarningMessages}";
                    else
                        msg = "DeleteTable() #1: Unknown issue, this shouldn't ever occur";
                }
            }
            else
            {
                if (respStatus.HasErrors)
                    msg = respStatus.AllErrorMessages;
                else if (respStatus.HasWarnings)
                    msg = respStatus.AllErrorMessages;
                else
                    msg = "DeleteTable() #2: Unknown issue, this shouldn't ever occur";
            }
        }
        else
            msg = $"Table '{tableName}' doesn't exists.";

        PressAnyKey(msg);
    }
    #endregion

    #region Record Examples
    static void CreateRecords(string tableName, bool clearConsole, Guid id)
    {
        Heading($"Create Records Example for Table: '{tableName}'", clearConsole);
        string msg;

        if (!clearConsole && id.Equals(testID))
            Console.WriteLine($"Expecting the next record create to FAIL, because the record id '{id}' already exists.");

        //validate table exists
        if (m_JsonProcessing.TableExists(tableName, out CJRespInfo respStatus))
        {
            //create a new record
            if(m_JsonProcessing.CreateNewRow(tableName, out DataRow dr, out respStatus))
            {
                id = id == Guid.Empty ? Guid.NewGuid() : id;
                dr[$"{ColNames.ID}"] = id;
                dr[$"{ColNames.Name}"] = "Chizl Tester";
                dr[$"{ColNames.Password}"] = JsonHandler.SecuredString(securedFieldValue1);

                //Colums named 'CreatedDate' or 'ModifiedDate' are auto filled, no requirement to set them yourself.
                //if you do set them yourself, they will not be overwritten.

                if (m_JsonProcessing.SaveRecord(tableName, dr, out respStatus))
                    msg = $"Success, Table '{tableName}' saved record id: {id}.";
                else if(respStatus.HasErrors)
                    msg = $"Exception occured while saving record:\n\t{respStatus.AllErrorMessages}";
                else
                    msg = $"Warnings occured while saving record:\n\t{respStatus.AllWarningMessages}";
            }
            else if(respStatus.HasErrors)
                msg = $"Exception occured while creating record from table:\n\t{respStatus.AllErrorMessages}";
            else
                msg = $"Warnings occured while creating record from table:\n\t{respStatus.AllWarningMessages}";
        }
        else
            msg = $"Table '{tableName}' doesn't exists.";

        PressAnyKey(msg);
    }
    static Guid ReadRecords(string tableName, bool clearConsole)
    {
        Heading($"Read Records from Table: '{tableName}'", clearConsole);
        string msg;
        Guid retVal = default;

        //validate table exists and pull all data as DataTable
        ConsoleColor orgColor = Console.ForegroundColor;
        if (m_JsonProcessing.GetTable(tableName, out DataTable dt, out CJRespInfo respStatus))
        {
            foreach (DataRow dr in dt.Rows)
            {
                //is a Guid
                var id = dr[$"{ColNames.ID}"];
                //is a string
                var name = dr[$"{ColNames.Name}"];
                //is a DateTime
                var createdDate = dr[$"{ColNames.CreatedDate}"];
                //is a DateTime
                var modifiedDate = dr[$"{ColNames.ModifiedDate}"];
                //is a SecureString
                var secPass = JsonHandler.GetColumn<SecureString>(dr, $"{ColNames.Password}");
                //Example 1 of pulling SecureString as clear value from DataRow.
                //NOTE: Slightly less secure, since SecureString is converted to string value and returned.
                var unSecPass1 = JsonHandler.GetColumn<string>(dr, $"{ColNames.Password}");
                //Example 2 of getting a clear value from the local SecureString var
                //NOTE: More secure, since the SecureString being passed in, returns as a NetworkCredentials
                //object.  "Password" is a property of the NetworkCredentials object being returned.
                var unSecPass2 = JsonHandler.SecuredString(secPass).Password;

                //this is only to show the alerting messages in orange.
                bool passChanged = unSecPass1.Equals(securedFieldValue2);
                string passWasAlert = passChanged ? $" <- Value Was: {securedFieldValue1}{Environment.NewLine}" : $"{Environment.NewLine}";
                string cd = ((DateTime)createdDate).ToString("yyMMddHHmmss");
                string md = ((DateTime)modifiedDate).ToString("yyMMddHHmmss");
                bool dateChanged = !cd.Equals(md);  //because milliseconds would be different, these were converted to string down to the seconds.
                string dateWasAlert = dateChanged ? $" <- Value Was: {createdDate}{Environment.NewLine}" : $"{Environment.NewLine}";

                //let display
                Console.WriteLine("ID:\t\t\t{0}", id);
                Console.WriteLine("Name:\t\t\t{0}", name);
                Console.WriteLine("CreateDate:\t\t{0}", createdDate);
                Console.Write("ModifiedDate:\t\t{0}", modifiedDate);
                if (dateChanged)
                    Console.ForegroundColor = ConsoleColor.DarkYellow;

                Console.Write("{0}", dateWasAlert);

                if (dateChanged)
                    Console.ForegroundColor = orgColor;

                Console.WriteLine("Secured Password:\t{0}", secPass);
                Console.Write("Unsecured Password v1:\t{0}", unSecPass1);
                if (passChanged)
                    Console.ForegroundColor = ConsoleColor.DarkYellow;

                Console.Write("{0}", passWasAlert);

                if (passChanged)
                    Console.ForegroundColor = orgColor;

                Console.Write("Unsecured Password v2:\t{0}", unSecPass2);

                if (passChanged)
                    Console.ForegroundColor = ConsoleColor.DarkYellow;

                Console.Write("{0}", passWasAlert);

                if (passChanged)
                    Console.ForegroundColor = orgColor;

                Console.WriteLine(new string('-', 10));

                //just grabbing the first records ID for future user.
                if (id.GetType().Name == "Guid" && retVal == default)
                    retVal = (Guid)id;
            }

            if (dt.Rows.Count == 0)
                msg = "No Records Exist";
            else
                msg = "Successfully displayed records.";
        }
        else
        {
            if(respStatus.HasErrors)
                msg = $"Response while getting table: '{tableName}'\n\t{respStatus.AllErrorMessages}.";
            else 
                msg = $"Response while getting table: '{tableName}'\n\t{respStatus.AllWarningMessages}.";
        }

        PressAnyKey(msg);

        return retVal;
    }
    static void GetRecord(string tableName, Guid guid, bool clearConsole)
    {
        Heading($"Search Records in Table: '{tableName}' for ID '{guid}'", clearConsole);
        string msg;

        //validate table exists
        if (m_JsonProcessing.TableExists(tableName, out CJRespInfo respStatus))
        {
            if (m_JsonProcessing.GetRecords(tableName, $"ID='{guid}'", string.Empty, out DataRow[] data, out respStatus))
            {
                foreach (DataRow dr in data)
                {
                    var id = dr[$"{ColNames.ID}"];
                    var name = dr[$"{ColNames.Name}"];
                    var createdDate = dr[$"{ColNames.CreatedDate}"];
                    var secPass = JsonHandler.GetColumn<SecureString>(dr, $"{ColNames.Password}");
                    var unSecPass1 = JsonHandler.GetColumn<string>(dr, $"{ColNames.Password}");
                    var unSecPass2 = JsonHandler.SecuredString(secPass).Password;

                    Console.WriteLine("ID:\t\t\t{0}", id);
                    Console.WriteLine("Name:\t\t\t{0}", name);
                    Console.WriteLine("CreateDate:\t\t{0}", createdDate);
                    Console.WriteLine("Secured Password:\t{0}", secPass);
                    Console.WriteLine("Unsecured Password v1:\t{0}", unSecPass1);
                    Console.WriteLine("Unsecured Password v2:\t{0}", unSecPass2);
                    Console.WriteLine(new string('-', 10));
                }

                if (data.Length == 0)
                    msg = "Successfully searched, but no records were found.";
                else
                    msg = "Successfully displayed records.";
            }
            else 
            {
                if(respStatus.HasErrors)
                    msg = $"Exception occured while searching for record.\n\t{respStatus.AllErrorMessages}";
                else
                    msg = $"Warnings occured while searching for record.\n\t{respStatus.AllWarningMessages}";
            }
        }
        else
            msg = $"Table '{tableName}' doesn't exists.";

        PressAnyKey(msg);
    }
    static void UpdateRecord(string tableName, Guid guid, bool clearConsole)
    {
        Heading($"Search Records in Table: '{tableName}' for ID '{guid}' and update password.", clearConsole);
        string msg;

        //validate table exists
        if (m_JsonProcessing.TableExists(tableName, out CJRespInfo respStatus))
        {
            //get record
            if (m_JsonProcessing.GetRecords(tableName, $"ID='{guid}'", string.Empty, out DataRow[] data, out respStatus))
            {
                //check length, so we can have a custom message during else.
                if (data.Length.Equals(0))
                    msg = "Record not found.";
                else
                {
                    //required because compiler doesn't understand that the above sets msg
                    //when no data and the foreach below will set msg if there is data.
                    msg = "dumb compiler";   //this will be overwritten in the foreach.
                }

                try
                {
                    foreach (DataRow dr in data)    //or update them all
                    {
                        dr[$"{ColNames.Password}"] = JsonHandler.SecuredString(securedFieldValue2);

                        //To save more than 1 at a time, use: m_JsonProcessing.SaveRecords(tableName: ..., dataRows: DataRows[], where: ...)
                        //Saves each record by themselves.
                        if (m_JsonProcessing.SaveRecord(tableName: tableName, dataRow: dr, where: $"ID='{guid}'", out int affectedCount, out respStatus) && affectedCount > 0)
                            msg = "Successfully updated record.";
                        else if (affectedCount.Equals(0))
                            msg = $"Table '{tableName}' didn't save any records matching your criteria.";
                        else if(respStatus.HasErrors)
                            msg = $"Exception occured while updating the record.\n\t{respStatus.AllErrorMessages}";
                        else if(respStatus.HasWarnings)
                            msg = $"Warnings occured while updating the record.\n\t{respStatus.AllWarningMessages}";
                    }
                }
                catch(Exception e)
                {
                    msg = $"Unexpected Exception occured while saving records.\n\t{e.Message}";
                }
            }
            else if (respStatus.HasErrors)
                msg = $"Exception occured while searching for record.\n\t{respStatus.AllErrorMessages}";
            else
                msg = $"Warnings occured while searching for record.\n\t{respStatus.AllErrorMessages}";
        }
        else
            msg = $"Table '{tableName}' doesn't exists.";

        PressAnyKey(msg);
    }
    static void DeleteRecord(string tableName, Guid guid, bool clearConsole)
    {
        Heading($"Delete Record in Table: '{tableName}' ID of '{guid}'", clearConsole);
        string msg;

        //validate table exists
        if (m_JsonProcessing.TableExists(tableName, out CJRespInfo respStatus))
        {
            if (m_JsonProcessing.DeleteRecords(tableName, $"ID='{guid}'", out int affectedCount, out respStatus) && affectedCount > 0)
                msg = "Successfully deleted record.";
            else if (respStatus.HasErrors)
                msg = $"Exception occured while updating the record.\n\t{respStatus.AllErrorMessages}";
            else if (affectedCount.Equals(0))
                msg = $"Table '{tableName}' didn't find any records matching ID='{guid}' for deletion.";
            else
                msg = $"Warnings occured while updating the record.\n\t{respStatus.AllWarningMessages}";
        }
        else
            msg = $"Table '{tableName}' doesn't exists.";


        PressAnyKey(msg);
    }
    #endregion

    #region Crypto Examples
    static void CrtypoExample(bool clearConsole)
    {
        Heading("AES Encrypting Text", clearConsole);

        string string2Encrypt = "The quick brown fox jumps over the lazy dog";
        NetworkCredential netCred = new(string.Empty, "SaltedPassword");

        Console.WriteLine($"Encrypting:\t{string2Encrypt}");  //extra line feed
        Console.WriteLine($"Salt:\t\t{netCred.Password}\n");

        using (CryptoAES crypto = new(netCred.SecurePassword, CHIZL_ENCODING_TYPE.UnicodeEncoding))
        {
            if (crypto.EncryptString(string2Encrypt, out string encryptedString))
            {
                Console.WriteLine($"Base64 Encrypted String:\n\t{encryptedString}\n");

                if (crypto.DecryptString(encryptedString, out NetworkCredential encData))
                {
                    if (encData.Password.Equals(string2Encrypt))
                        Console.WriteLine($"Success decrypting text.\n\t{encData.Password}");
                    else
                        Console.WriteLine($"Filed to decrypt text.\n\t{crypto.LastError.Message}");
                }
                else
                    Console.WriteLine($"Filed to encrypting text.\n\t{crypto.LastError.Message}");
            }
            else
                Console.WriteLine($"Filed to encrypt text.\n\t{crypto.LastError.Message}");
        }

        PressAnyKey();
    }
    #endregion

    #region Template Methods
    static void PressAnyKey() => PressAnyKey(string.Empty);
    static void PressAnyKey(string msg)
    {
        if(!string.IsNullOrWhiteSpace(msg))
            Console.WriteLine($"{Environment.NewLine}{msg}{Environment.NewLine}");

        Console.WriteLine(new string('-', 20));
        Console.WriteLine("Press any key to continue.");
        Console.WriteLine(new string('-', 20));
        Console.WriteLine(Environment.NewLine);
        Console.ReadKey(true);
    }
    static void Heading(string msg, bool clearConsole = true)
    {
        if(clearConsole)
            Console.Clear();

        int len = msg.Length + 8;

        Console.WriteLine(new string('=', len));
        Console.WriteLine($"==[ {msg} ]==");
        Console.WriteLine(new string('=', len));
    }
    #endregion
}