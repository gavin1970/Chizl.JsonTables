using System.Security.Cryptography;     //ICryptoTransform, Aes
using System.Text;                      //Encoding, UnicodeEncoding, ASCIIEncoding, UTF8Encoding
using System.Security;                  //SecuritySafeCritical, SecureString
using System.Net;                       //NetworkCredential
using System;
using System.Linq;
using System.IO;

namespace Chizl.Crypto.aes
{
    public enum CHIZL_ENCODING_TYPE
    {
        UTF8Encoding,
        UnicodeEncoding,
        ASCIIEncoding
    }
    public enum CHIZL_ENCRYP_RESPONSE
    {
        Key,
        Vector
    }

    [SecuritySafeCritical]
    public class CryptoAES : IDisposable
    {
        internal ICryptoTransform EncryptorTransform;
        internal ICryptoTransform DecryptorTransform;
        static private bool _disposed = false;

        //defaults, changed with salt
        [SecuritySafeCritical]
        private readonly byte[] Key = { 143, 217, 19, 111, 24, 216, 85, 45, 
                                        111, 184, 27, 162, 137, 114, 222, 209, 
                                        241, 24, 175, 144, 173, 53, 196, 29, 
                                        24, 26, 17, 218, 131, 236, 53, 209 };       //32bytes
        //defaults, changed with salt
        [SecuritySafeCritical]
        private readonly byte[] Vector = { 126, 64, 191, 112, 23, 3, 116, 119, 
                                            231, 121, 252, 112, 79, 32, 114, 156 }; //16bytes

        private readonly Encoding Encoder;
        public Exception LastError { get; private set; } = new Exception(null);

        #region Setup and Destroy
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key"></param>
        /// <param name="encodingType"></param>
        [SecuritySafeCritical]
        public CryptoAES(SecureString salt, CHIZL_ENCODING_TYPE encodingType = CHIZL_ENCODING_TYPE.UTF8Encoding)
        {
            //easy read, but keep it secure until split up into bytes
            NetworkCredential netCred = new NetworkCredential(string.Empty, salt);

            switch (encodingType)
            {
                case CHIZL_ENCODING_TYPE.UnicodeEncoding:
                    Encoder = new UnicodeEncoding();
                    break;
                case CHIZL_ENCODING_TYPE.ASCIIEncoding:
                    Encoder = new ASCIIEncoding();
                    break;
                case CHIZL_ENCODING_TYPE.UTF8Encoding:
                default:
                    Encoder = new UTF8Encoding();
                    break;
            }

            if (!string.IsNullOrWhiteSpace(netCred.Password))
            {
                byte[] keyBytes = Encoding.ASCII.GetBytes(netCred.Password);
                //this is to ensure salt isn't larger than the key or
                //vector, but add into encryption.
                for (int i = 0; i < keyBytes.Length; i++)
                {
                    if (i <= this.Key.Length - 1)
                        this.Key[i] = keyBytes[i];

                    if (i <= this.Vector.Length - 1)
                        this.Vector[i] = keyBytes[i];
                }
            }

            LoadEncryptor(this.Key, this.Vector);
        }
        ~CryptoAES()
        {
            Dispose(false);
        }
        /// <summary>
        /// Lets do some clean up
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// clean up.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                EncryptorTransform?.Dispose();
                DecryptorTransform?.Dispose();
            }

            _disposed = true;
        }
        #endregion

        #region Statis Public Methods
        /// <summary>
        /// Generates an encryption key.
        /// </summary>
        /// <returns></returns>
        [SecuritySafeCritical]
        static public byte[] GenerateEncryption(CHIZL_ENCRYP_RESPONSE encRespType)
        {
            byte[] retVal;
            Aes aes = Create();

            switch(encRespType)
            {
                case CHIZL_ENCRYP_RESPONSE.Vector:
                    aes.GenerateIV();
                    retVal = aes.IV;
                    break;
                case CHIZL_ENCRYP_RESPONSE.Key:
                default:
                    aes.GenerateKey();
                    retVal = aes.Key;
                    break;
            }

            return retVal;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Encrypt some text and return a string suitable for passing in a URL.
        /// </summary>
        /// <param name="textString"></param>
        /// <returns></returns>
        [SecuritySafeCritical]
        public bool EncryptString(string textString, out string textEncrypted)
        {
            bool success = false;
            textEncrypted = string.Empty;

            try
            {
                textEncrypted = ByteArrToString(Encrypt(textString));
                success = true;
            }
            catch (Exception ex)
            {
                LastError = ex;
            }

            return success;
        }
        /// <summary>
        /// Decryption methods.  If an the string is found to not be encrypted<br/>
        /// or fails to decrypt, this method will return the string<br/>
        /// that was passed in, but as a secure string.<br/>
        /// <code>
        /// Example: 
        /// if(DecryptString(ENCRYPTED_STRING, out NetworkCredential netCred))<br/>
        /// {<br/>
        ///    string clearString = netCred.Password;<br/>
        ///    SecureString securedString = netCred.SecurePassword;
        /// }<br/>
        /// </code>
        /// </summary>
        /// <param name="EncryptedString"></param>
        /// <returns></returns>
        [SecuritySafeCritical]
        public bool DecryptString(string encryptedString, out NetworkCredential netCred)
        {
            netCred = new NetworkCredential(string.Empty, string.Empty);
            
            bool success = false;
            if (encryptedString != null 
                && (encryptedString.Length % 16) == 0)
            {
                try
                {
                    byte[] encryptedBytes = StrToByteArray(encryptedString);
                    netCred = Decrypt(encryptedBytes);
                    success = true;
                }
                catch (Exception ex)
                {
                    LastError = ex;
                }
            }

            return success;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Centeral location to create Algorithm name
        /// </summary>
        /// <returns></returns>
        [SecuritySafeCritical]
        static private Aes Create()
        {
            return Aes.Create();
        }
        /// <summary>
        /// Loads Encryptor and Decryptor
        /// </summary>
        /// <param name="crypto32ByteKey"></param>
        /// <param name="crypto16ByteVector"></param>
        [SecuritySafeCritical]
        private void LoadEncryptor(byte[] crypto32ByteKey, byte[] crypto16ByteVector)
        {
            //This is our encryption method
            Aes aes = Create();

            crypto32ByteKey = crypto32ByteKey ?? this.Key;
            crypto16ByteVector = crypto16ByteVector ?? this.Vector;

            //Create an encryptor and a decryptor using our encryption method, key, and vector.
            EncryptorTransform = aes.CreateEncryptor(crypto32ByteKey, crypto16ByteVector);
            DecryptorTransform = aes.CreateDecryptor(crypto32ByteKey, crypto16ByteVector);
        }
        /// <summary>
        /// Encrypt some text and return an encrypted byte array.
        /// </summary>
        /// <param name="textString"></param>
        /// <returns></returns>
        [SecuritySafeCritical]
        private byte[] Encrypt(string textString)
        {
            byte[] bytes;
            //Translates our text value into a byte array.
            bytes = Encoder.GetBytes(textString);

            //Used to stream the data in and out of the CryptoStream.
            MemoryStream memoryStream = new MemoryStream();

            //We will have to write the unencrypted bytes to the stream,
            //then read the encrypted result back from the stream.
            CryptoStream cs = new CryptoStream(memoryStream, EncryptorTransform, CryptoStreamMode.Write);
            cs.Write(bytes, 0, bytes.Length);
            cs.FlushFinalBlock();

            // Read encrypted value back out of the stream
            memoryStream.Position = 0;
            byte[] encrypted = new byte[memoryStream.Length];
            memoryStream.Read(encrypted, 0, encrypted.Length);

            //Clean up.
            cs.Close();

            //https://msdn.microsoft.com/library/ms182334.aspx
            //memoryStream.Close();

            return encrypted;
        }
        /// <summary>
        /// Decryption when working with byte arrays.    
        /// </summary>
        /// <param name="encryptedValue"></param>
        /// <returns></returns>
        [SecuritySafeCritical]
        private NetworkCredential Decrypt(byte[] encryptedValue)
        {
            //Write the encrypted value to the decryption stream
            MemoryStream encryptedStream = new MemoryStream();
            CryptoStream decryptStream = new CryptoStream(encryptedStream, DecryptorTransform, CryptoStreamMode.Write);
            decryptStream.Write(encryptedValue, 0, encryptedValue.Length);
            decryptStream.FlushFinalBlock();

            //Read the decrypted value from the stream.
            encryptedStream.Position = 0;
            byte[] decryptedBytes = new byte[encryptedStream.Length];
            encryptedStream.Read(decryptedBytes, 0, decryptedBytes.Length);
            encryptedStream.Close();
            return new NetworkCredential(string.Empty, Encoder.GetString(decryptedBytes));
        }
        /// <summary>
        /// Convert a string to a byte array, can be used with URL's as well.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        [SecuritySafeCritical]
        private byte[] StrToByteArray(string str)
        {
            if (str.Length == 0)
                throw new Exception("Invalid string value in StrToByteArray");

            return Enumerable.Range(0, str.Length)
                    .Where(x => x % 2 == 0)
                    .Select(x => Convert.ToByte(str.Substring(x, 2), 16))
                    .ToArray();
        }
        /// <summary>
        /// Byte array back to a string
        /// </summary>
        /// <param name="byteArr"></param>
        /// <returns></returns>
        [SecuritySafeCritical]
        private string ByteArrToString(byte[] byteArr)
        {
            if (byteArr.Length == 0)
                throw new Exception("Invalid byteArr value in ByteArrToString");

            return BitConverter.ToString(byteArr).Replace("-", "");
        }
        #endregion
    }
}
