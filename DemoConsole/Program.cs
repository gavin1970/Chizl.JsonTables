﻿using Chizl.JsonTables.json;
using Chizl.Crypto.aes;
using System.Data;
using System.Net;
using System.Security;
using System.Diagnostics.CodeAnalysis;

internal class Program
{
    private enum ColNames
    {
        ID,
        Name,
        CreatedDate,
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
        
    [AllowNull]
    static JsonHandler m_JsonProcessing;

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
        m_JsonProcessing = new(fileName, dataSetName, piSecureString);

        CreateTable(tableName1, false);             //create new table (1)
        CreateTable(tableName2, false);             //create new table (2)
        CreateRecords(tableName1, true);            //create a new record (1)
        CreateRecords(tableName1, false);           //create a second record (1a)
        CreateRecords(tableName2, true);            //create a new record (only 1 for table 2)

        Guid guid1 = ReadRecords(tableName1, true); //read all records, get first guid back for future select
        GetRecord(tableName1, guid1, true);         //pull one record by id (1)
        
        UpdateRecord(tableName1, guid1, true);      //change 1 record by id (1)
        ReadRecords(tableName1, true);              //view all records to see the 1 changed record (1)

        Guid guid2 = ReadRecords(tableName2, true); //read all records, get first guid back for future select
        GetRecord(tableName2, guid2, true);         //pull one record by id

        UpdateRecord(tableName2, guid2, true);      //change 1 record by id
        ReadRecords(tableName2, true);              //view all records to see the 1 changed record

        DeleteRecord(tableName1, guid1, true);      //delete record
        ReadRecords(tableName1, true);              //view all records to see 1 record was deleted

        DeleteRecord(tableName2, guid2, true);      //delete record
        ReadRecords(tableName2, true);              //view all records to see 0 records exist

        DeleteTable(tableName1, true);              //delete table
        DeleteTable(tableName2, true);              //delete table

        CrtypoExample(true);    //shows how crypto is used inside and can be used by itself.

        //clean up
        piSecureString.Dispose();
    }

    #region Table Examples
    static void CreateTable(string tableName, bool clearConsole)
    {
        Heading($"Create Table: '{tableName}'", clearConsole);

        bool success = true;
        string msg;

        //validate table exists
        if (!m_JsonProcessing.TableExists(tableName))
        {
            // Table Defs 
            List<DataColumn> allCols = new() {
                new($"{ColNames.ID}", typeof(Guid)),
                new($"{ColNames.Name}", typeof(string)),
                new($"{ColNames.CreatedDate}", typeof(DateTime)),
                new($"{ColNames.Password}", typeof(SecureString))   //secured data.
            };

            // Create columns by passing the whole array.
            // success = m_JsonProcessing.AddColumns(tableName, allCols.ToArray());

            // Create columns 1 by 1
            foreach (DataColumn col in allCols)
            {
                success = m_JsonProcessing.AddColumn(tableName, col, out bool hadException);
                if (!success && hadException)
                    Console.WriteLine($"Exception occured adding columns table:\n\t{m_JsonProcessing.LastError.Message}");
            }

            // Save to Disk 
            if (success) success = m_JsonProcessing.Flush();

            // Display success of failure 
            msg = success ? 
                $"Success, Table '{tableName}' created." : 
                $"Exception Occured:\n\t{m_JsonProcessing.LastError.Message}";
        }
        else
            msg = $"Table '{tableName}' created already exists.";

        PressAnyKey(msg);
    }
    static void DeleteTable(string tableName, bool clearConsole)
    {
        Heading($"Drop Table Example: '{tableName}'", clearConsole);
        string msg;

        if (m_JsonProcessing.TableExists(tableName))
        {
            if (!m_JsonProcessing.RemoveTable(tableName))
                msg = $"Exception occured while removing table:\n\t{m_JsonProcessing.LastError.Message}";
            else if (!m_JsonProcessing.Flush())
                msg = $"Exception occured while saving after removing table:\n\t{m_JsonProcessing.LastError.Message}";
            else
                msg = $"Success, Table '{tableName}' removed.";
        }
        else
            msg = $"Table '{tableName}' doesn't exists.";

        PressAnyKey(msg);
    }
    #endregion

    #region Record Examples
    static void CreateRecords(string tableName, bool clearConsole)
    {
        Heading($"Create Records Example for Table: '{tableName}'", clearConsole);
        string msg;

        //validate table exists
        if (m_JsonProcessing.TableExists(tableName))
        {
            //create a new record
            if(m_JsonProcessing.CreateRecord(tableName, out DataRow dr))
            {
                dr[$"{ColNames.ID}"] = Guid.NewGuid();
                dr[$"{ColNames.Name}"] = "Chizl Tester";
                dr[$"{ColNames.CreatedDate}"] = DateTime.UtcNow;
                dr[$"{ColNames.Password}"] = JsonHandler.SecuredString(securedFieldValue1);

                if (m_JsonProcessing.SaveRecord(tableName, dr))
                    msg = $"Success, Table '{tableName}' record save.";
                else
                    msg = $"Exception occured while saving record:\n\t{m_JsonProcessing.LastError.Message}";
            }
            else
                msg = $"Exception occured while creating record from table:\n\t{m_JsonProcessing.LastError.Message}";
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
        if (m_JsonProcessing.GetTable(tableName, out DataTable dt))
        {
            foreach (DataRow dr in dt.Rows)
            {
                //is a Guid
                var id = dr[$"{ColNames.ID}"];
                //is a string
                var name = dr[$"{ColNames.Name}"];
                //is a DateTime
                var createdDate = dr[$"{ColNames.CreatedDate}"];
                //is a SecureString
                var secPass = m_JsonProcessing.GetColumn<SecureString>(dr, $"{ColNames.Password}");
                //version 1 of pulling SecureString as clear value from DataRow.
                //NOTE: Less secure, since SecureString is converted to string value in object and returned.
                var unSecPass1 = m_JsonProcessing.GetColumn<string>(dr, $"{ColNames.Password}");
                //version 2 of getting a clear value from the local SecureString var
                //NOTE: Most secure, since it's returning SecureString as a NetworkCredentials
                //object and "Password" is a property of NetworkCredentials.
                var unSecPass2 = JsonHandler.SecuredString(secPass).Password;

                bool valChanged = unSecPass1.Equals(securedFieldValue2);
                string wasAlert = valChanged ? $" <- Value Was: {securedFieldValue1}{Environment.NewLine}" : $"{Environment.NewLine}";

                Console.WriteLine("ID:\t\t\t{0}", id);
                Console.WriteLine("Name:\t\t\t{0}", name);
                Console.WriteLine("CreateDate:\t\t{0}", createdDate);
                Console.WriteLine("Secured Password:\t{0}", secPass);
                Console.Write("Unsecured Password v1:\t{0}", unSecPass1);
                if (valChanged)
                    Console.ForegroundColor = ConsoleColor.DarkYellow;

                Console.Write("{0}", wasAlert);

                if (valChanged)
                    Console.ForegroundColor = orgColor;

                Console.Write("Unsecured Password v2:\t{0}", unSecPass2);

                if (valChanged)
                    Console.ForegroundColor = ConsoleColor.DarkYellow;

                Console.Write("{0}", wasAlert);

                if (valChanged)
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
            msg = $"Response while getting table: '{tableName}'\n\t{m_JsonProcessing.LastError.Message}.";


        PressAnyKey(msg);

        return retVal;
    }
    static void GetRecord(string tableName, Guid guid, bool clearConsole)
    {
        Heading($"Search Records in Table: '{tableName}' for ID '{guid}'", clearConsole);
        string msg;

        //validate table exists
        if (m_JsonProcessing.TableExists(tableName))
        {
            if (m_JsonProcessing.GetRecords(tableName, $"ID='{guid}'", string.Empty, out DataRow[] data))
            {
                foreach (DataRow dr in data)
                {
                    var id = dr[$"{ColNames.ID}"];
                    var name = dr[$"{ColNames.Name}"];
                    var createdDate = dr[$"{ColNames.CreatedDate}"];
                    var secPass = m_JsonProcessing.GetColumn<SecureString>(dr, $"{ColNames.Password}");
                    var unSecPass1 = m_JsonProcessing.GetColumn<string>(dr, $"{ColNames.Password}");
                    var unSecPass2 = JsonHandler.SecuredString(secPass).Password;

                    Console.WriteLine("ID:\t\t\t{0}", id);
                    Console.WriteLine("Name:\t\t\t{0}", name);
                    Console.WriteLine("CreateDate:\t\t{0}", createdDate);
                    Console.WriteLine("Secured Password:\t{0}", secPass);
                    Console.WriteLine("Unsecured Password v1:\t{0}", unSecPass1);
                    Console.WriteLine("Unsecured Password v2:\t{0}", unSecPass2);
                    Console.WriteLine(new string('-', 10));
                }

                if(data.Length == 0)
                    msg = "Successfully searched, but no records were found.";
                else
                    msg = "Successfully displayed records.";
            }
            else if(m_JsonProcessing.LastError != null)
                msg = $"Exception occured while searching for record.\n\t{m_JsonProcessing.LastError.Message}";
            else
                msg = "Unable to get record for unknown reasons";

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
        if (m_JsonProcessing.TableExists(tableName))
        {
            //get record
            if (m_JsonProcessing.GetRecords(tableName, $"ID='{guid}'", string.Empty, out DataRow[] data))
            {
                //check length, so we can have a custom message during else.
                if (data.Length > 0)
                {
                    //BUG IN VISUAL STUDIO?   I have to add the msg="" here or VS thinks it might be null on PressAnyKey(msg) below.
                    //The if statement above, checks data length before the for loop specifically for that reason.
                    //No reason this should be required, so I believe it's a BUG.
                    msg = "";  //<-- do this or initialize at the start.  Leaving this here to show the issue.

                    try
                    {
                        //DataRow dr = data[0];         //get first record, should never be more than 1 in this case.
                        foreach (DataRow dr in data)    //or update them all
                        {
                            dr[$"{ColNames.Password}"] = JsonHandler.SecuredString(securedFieldValue2);

                            //To save more than 1 at a time, use: m_JsonProcessing.SaveRecords(tableName: ..., dataRows: DataRows[], where: ...)
                            //Saves each record by themselves.
                            if (m_JsonProcessing.SaveRecord(tableName: tableName, dataRow: dr, where: $"ID='{guid}'"))
                            {
                                msg = "Successfully updated record.";
                            }
                            else
                            {
                                msg = $"Exception occured while updating the record.\n\t{m_JsonProcessing.LastError.Message}";
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        msg = $"Unexpected Exception occured while saving records.\n\t{e.Message}";
                    }

                }
                else
                {
                    msg = "Record not found.";
                }
            }
            else if (m_JsonProcessing.LastError != null)
            {
                msg = $"Exception occured while searching for record.\n\t{m_JsonProcessing.LastError.Message}";
            }
            else
            {
                msg = "Unable to get record for unknown reasons";
            }
        }
        else
        {
            msg = $"Table '{tableName}' doesn't exists.";
        }

        PressAnyKey(msg);
    }
    static void DeleteRecord(string tableName, Guid guid, bool clearConsole)
    {
        Heading($"Delete Record in Table: '{tableName}' ID of '{guid}'", clearConsole);
        string msg;

        //validate table exists
        if (m_JsonProcessing.TableExists(tableName))
        {
            if (m_JsonProcessing.DeleteRecords(tableName, $"ID='{guid}'"))
                msg = "Successfully deleted record.";
            else
                msg = $"Failed to delete record\n\t{m_JsonProcessing.LastError.Message}";
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