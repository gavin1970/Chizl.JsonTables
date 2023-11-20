using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Security;
using System.Text.RegularExpressions;

namespace Chizl.JsonTables
{
    internal class Utils
    {
        #region Non-Static Public Methods
        /// <summary>
        /// This is used as a wrapper for generic types and used in json\services\JsonDataConverter.cs.<br/>
        /// Look for typeof(Utils).GetMethod("Converts");
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public T Converts<T>(object input)
        {
            if (ConvertTo<T>(input, out T outputValue))
                return outputValue;
            else
                return default;
        }
        #endregion

        #region Private Static Method
        /// <summary>
        /// Converts Int32.toString() to Color<br/>
        /// Converts string by stripping all leters except the commas and convert it to Color.<br/>
        /// If there are only 3 numbers seperated by commas, it will assume the Alpha is 255.<br/>
        /// <code>
        /// Assumptions:<br/>
        ///     - 4 numbers sperated by comma, means Alpha exist.<br/>
        ///     - 3 numbers sperated by comma, means Alpha = 255.<br/>
        ///     - The last 3 numbers are in Red, Green, Blue and all numbers range between 0-255<br/>
        ///   Examples: 
        ///     - [255, 128, 128, 128]<br/>
        ///     - Color [A, R, G, B]<br/>
        ///     - Color [R, G, B]<br/>
        /// </code>
        /// Converts string "Color [R, G, B]" to Color<br/>
        /// before passing in.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        private static Color Convert2Color(string color)
        {
            Color retVal;

            string s = Regex.Replace(color, "[^0-9,-]", "");
            if (Int32.TryParse(color, out Int32 colInt))
                retVal = Color.FromArgb(colInt);
            else if (s.IndexOf(',') > -1)
            {
                string[] colorSplit = s.Split(',');
                List<int> argb = new List<int>();
                foreach (string newS in colorSplit)
                {
                    if (int.TryParse(newS, out int c))
                    {
                        if (c > 255) c = 255;
                        else if(c < 0) c = 0;
                        argb.Add(c);
                    }
                }
                if (argb.Count.Equals(4))
                    retVal = Color.FromArgb(argb[0], argb[1], argb[2], argb[3]);
                else if (argb.Count.Equals(3))
                    retVal = Color.FromArgb(255, argb[0], argb[1], argb[2]);
                else
                    retVal = Color.Empty;    //returning no color.
            }
            else
                retVal = Color.FromName(color);

            return retVal;
        }
        /// <summary>
        /// This works for Framework 4.7/4.8, but not NET6. It also works<br/>
        /// in .NETCore 7+, but waiting for version .NET8 to fully release<br/>
        /// -----------<br/>
        /// Many formats of Font as string, these are the most popular ones.<br/>
        /// Returns an object, because Font.class isn't valid for version 6.
        /// </summary>
        /// <param name="fontString"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static object Convert2Font(string fontString)
        {
#if NET48_OR_GREATER //.NET Framework
            var fC = new FontConverter();
            try
            {
                //lets try easy way first.
                var ft = fC.ConvertFromString(fontString) as Font;
                return ft;
            }
            catch { }

            string stripFont = fontString;

            //format 1
            if (fontString.StartsWith("[Font: "))
            {
                try
                {
                    stripFont = fontString.Replace("[Font: ", "").Replace("]", "");
                    var ft = fC.ConvertFromString(stripFont) as Font;
                    return ft;
                }
                catch { /* Ingore, we will try the next format */ }
            }

            //format 2
            if (fontString.Contains("Name="))
            {
                try
                {
                    string[] fontSplit = stripFont.Split(',');

                    string name = "";
                    float size = 0F;
                    FontStyle style = FontStyle.Regular;
                    GraphicsUnit units = GraphicsUnit.Pixel;
                    byte gdiCharSet = 0;
                    bool gdiVerticalFont = false;

                    foreach (string s in fontSplit)
                    {
                        string[] col = s.Split('=');
                        switch (col[0].ToLower().Trim())
                        {
                            case "name":
                                name = col[1];
                                break;
                            case "size":
                                size = float.Parse(col[1]);
                                break;
                            case "style":
                                if (int.TryParse(col[1], out int iStyle))
                                    style = (FontStyle)Enum.ToObject(typeof(FontStyle), iStyle);
                                else
                                    style = (FontStyle)Enum.ToObject(typeof(FontStyle), col[1]);
                                break;
                            case "units":
                                if (int.TryParse(col[1], out int iUnit))
                                    units = (GraphicsUnit)Enum.ToObject(typeof(GraphicsUnit), iUnit);
                                else
                                    units = (GraphicsUnit)Enum.ToObject(typeof(GraphicsUnit), col[1]);
                                break;
                            case "gdicharset":
                                gdiCharSet = byte.Parse(col[1]);
                                break;
                            case "gdiverticalfont":
                                gdiVerticalFont = bool.Parse(col[1]);
                                break;
                        }
                    }

                    var ft = new Font(name, size, style, units, gdiCharSet, gdiVerticalFont);
                    return ft;
                }
                catch { }
            }
#endif
            throw new Exception($"Font couldn't be converted from String '{fontString}'");
        }
        #endregion
        
        #region Internal Static Properties
        /// <summary>
        /// Shows the last Exception that was caused within this class.
        /// </summary>
        internal static Exception LastError { get; private set; }
        #endregion
        
        #region Internal Static Method
        /// <summary>
        /// Generic method setup to convert one object type into another.
        /// Starting a convertion process for most popular objects.  
        /// When working with Json, everything is string, so we will 
        /// need to convert for formatting purposes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="outputValue"></param>
        /// <returns></returns>
        internal static bool ConvertTo<T>(object input, out T outputValue)
        {
            bool retVal = true;
            outputValue = default;              //set default value, just in case.

            //set a default if null was passed in.
            var inputValue = input ?? default(T);

            //Conversions where looking like >> outputValue = (T)(object)Enum.ToOjbect(....)
            //so to fix this, I create an object with the default value of that type.  I then cast
            //the object at the end and it works no matter what type and wihtout all (object) cast every time.
            object outputObj = default(T);

            Type inputType = inputValue.GetType();
            string inputTypeName = inputType.Name;

            Type outputType = typeof(T);
            string outputTypeName = outputType.Name;

            try
            {
                if (inputTypeName.Equals("DBNull"))
                    inputValue = outputValue;
                
                //if not null then convert it to what the generic is..
                if (inputValue != null)
                {
                    if (outputType.IsEnum)
                    {
                        if (int.TryParse(inputValue.ToString(), out int iVal))
                            outputObj = Enum.ToObject(outputType, iVal);
                        else
                            outputObj = inputValue;
                    }
                    else if (outputTypeName.Equals("Font"))
                    {
                        //I didn't want to comment "Convert2Font" out if not the correct version of .NET.  Reason being, it
                        //chould show the method itself as not used on someone's machine and they might delete it not
                        //realizing on their next upgrade this code wouldn't compile, because of a missing method.
                        outputObj = Utils.Convert2Font(inputValue?.ToString());
#if! NET48_OR_GREATER   //.NET 4.8 Framework
                        throw new Exception("Font is only a support class in .NET v4.8+ Framework");
#endif
                    }
                    else if (inputTypeName.Equals("SecureString"))
                    {
                        if (outputTypeName.Equals("SecureString"))
                            outputObj = inputValue;
                        else 
                        {
                            NetworkCredential nc = new NetworkCredential(string.Empty, (SecureString)inputValue);
                            var outPutStr = nc.Password;

                            if (outputTypeName.Equals("String"))
                                outputObj = outPutStr;
                            else if (ConvertTo<T>(outPutStr, out T newOutput))  //convert data into requested output
                                outputObj = newOutput;
                        }
                    }
                    else if (outputTypeName.Equals("String") 
                        && inputTypeName.Equals("Guid"))
                    {
                        outputObj = ((Guid)inputValue).ToString(); 
                    }
                    else if (outputTypeName.Equals("Guid"))
                    {
                        if (!inputTypeName.Equals("Guid"))
                        {
                            if (Guid.TryParse(inputValue.ToString(), out Guid newG))
                                outputObj = newG;
                            else
                                outputObj = inputValue;
                        }
                        else
                            outputObj = inputValue;
                    }
                    else if (outputTypeName.Equals("Color")
                        && (inputTypeName.Equals("String") 
                        || inputTypeName.StartsWith("Int")))
                    {
                        outputObj = Utils.Convert2Color(inputValue.ToString());
                    }
                    else if (inputTypeName.Equals("String[]") 
                        && outputTypeName.StartsWith("List") 
                        && outputTypeName.Contains("String"))
                    {
                        outputObj = ((string[])inputValue).ToList();
                    }
                    else if (inputTypeName.Equals("String[]") 
                        && outputTypeName.Equals("String"))
                    {
                        outputObj = string.Join(",", ((string[])inputValue));
                    }
                    else if (inputTypeName.Equals("String")
                        && outputTypeName.Equals("Point"))
                    {
                        string s = Regex.Replace(inputValue.ToString(), "[^0-9,]", "");
                        string[] pointSplit = s.Split(',');
                        List<int> pInt = new List<int>();
                        foreach (string newS in pointSplit)
                        {
                            if (int.TryParse(newS, out int p))
                                pInt.Add(p);
                        }
                        if (pInt.Count > 1)
                            outputObj = new Point(pInt[0], pInt[1]);
                    }
                    else
                        outputObj = Convert.ChangeType(inputValue, outputType);

                    //for anything left over.
                    outputObj = outputObj ?? default(T);
                }
                else
                    outputObj = default(T);

                outputValue = (T)outputObj;
            }
            catch (Exception ex)
            {
                string inputText = inputTypeName.Equals("SecureString") ? 
                    "to" : 
                    $"with value of '{input}' to";
                throw new Exception($"While converting typeof({inputTypeName}) {inputText} typeof('{outputTypeName}') the following exception occured:\n'{ex.Message}");
            }

            return retVal;
        }
        /// <summary>
        /// Converts SecureString into NetworkCredential
        /// </summary>
        /// <param name="ss"></param>
        /// <returns></returns>
        internal static NetworkCredential GetCredObj(SecureString ss)
        {
            return new NetworkCredential(string.Empty, ss);
        }
        /// <summary>
        /// Converts string into NetworkCredential
        /// </summary>
        /// <param name="ss"></param>
        /// <returns></returns>
        internal static NetworkCredential GetCredObj(string ss)
        {
            return new NetworkCredential(string.Empty, ss);
        }
        /// <summary>
        /// Clean up where and sorts.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        internal static string CleanQuery(string query)
        {
            string retVal = "";
            string trimQuery = query?.Trim();

            if (!string.IsNullOrWhiteSpace(trimQuery))
            {
                if (trimQuery.StartsWith("WHERE "))
                    retVal = trimQuery.Replace("WHERE ", "").Trim();
                else if (trimQuery.StartsWith("ORDER BY "))
                    retVal = trimQuery.Replace("ORDER BY ", "").Trim();
                else
                    retVal = trimQuery;
            }

            //if(retVal.Length> 0)    //prevent SQL injections
            //    retVal = retVal.Replace("'", "\'");

            return retVal;
        }
        /// <summary>
        /// Converts JSON special characters to unicode and back during restore.
        /// </summary>
        /// <param name="json"></param>
        /// <param name="restore"></param>
        /// <returns></returns>
        internal static string JsonClean(string jsonValue, bool restore = false)
        {
            jsonValue = string.IsNullOrWhiteSpace(jsonValue) ? "" : jsonValue.Trim();
            //return jsonValue;
            List<char> lookfor = new List<char> { '/', '\"', '\'', '\b', '\f', '\t', '\r', '\n' };

            foreach (char ch in lookfor)
            {
                string find = string.Empty;
                string replace = string.Empty;

                if (restore)
                {
                    find = $"$\\{ch}$";
                    replace = ch.ToString();
                }
                else
                {
                    find = ch.ToString();
                    replace = $"$\\{ch}$";
                }

                if (jsonValue.IndexOf(find) > -1)
                    jsonValue = jsonValue.Replace(find, replace);
            }

            return jsonValue;
        }
        #endregion
    }
}
