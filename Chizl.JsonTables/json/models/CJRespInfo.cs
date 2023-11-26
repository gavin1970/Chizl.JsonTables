using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;

namespace Chizl.JsonTables.json
{
    public class CJRespInfo
    {
        public CJRespInfo()
        {
            List<string> clsMethod = GetMethodName(METHOD_HISTORY.PREVIOUS_METHOD).Split('.').ToList();

            List<string> remove = clsMethod.FindAll(f => f.Trim().Equals(""));
            foreach (string s in remove)
                clsMethod.Remove(s);

            remove = clsMethod.FindAll(f => f.Equals("ctor"));

            foreach (string s in remove)
                clsMethod.Remove(s);

            int classStarts = remove.Count > 0 ? clsMethod.Count - 1 : clsMethod.Count - 2;

            if (clsMethod.Count > 1)
                ClassName = clsMethod[classStarts];
            if (!clsMethod[clsMethod.Count - 1].Equals(ClassName))
                MethodName = clsMethod[clsMethod.Count - 1];
            else
                MethodName = "Constructor";
        }
        public string ClassName { get; }
        public string MethodName { get; }
        public bool HasErrors { get { return Errors.Count > 0; } }
        public bool HasWarnings { get { return Warnings.Count > 0; } }
        public bool HasErrorOrWarnings { get { return HasErrors || HasWarnings; } }
        public List<string> Errors { set; get; } = new List<string>();
        public List<string> Warnings { set; get; } = new List<string>();
        public CJ_RESP_STATUS Status
        {
            get
            {
                return !HasErrors && !HasWarnings ? 
                    CJ_RESP_STATUS.Success :
                    Errors.Count != 0 ?
                        CJ_RESP_STATUS.Error : 
                        CJ_RESP_STATUS.Warning;
            }
        }
        public string LastErrorMessage
        {
            get
            {
                if (Errors.Count == 0)
                    return string.Empty;
                else
                {
                    return Errors[Errors.Count - 1];
                }
            }
        }
        public string LastWarningMessage
        {
            get
            {
                if (Warnings.Count == 0)
                    return string.Empty;
                else
                {
                    return Warnings[Warnings.Count - 1];
                }
            }
        }
        public string AllErrorMessages
        {
            get
            {
                if (Errors.Count == 0)
                    return string.Empty;
                else
                {
                    return string.Join(Environment.NewLine, Errors);
                }
            }
        }
        public string AllWarningMessages
        {
            get
            {
                if (Warnings.Count == 0)
                    return string.Empty;
                else
                {
                    return string.Join(Environment.NewLine, Warnings);
                }
            }
        }

        #region private Methods
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetMethodName(METHOD_HISTORY lastMethod = METHOD_HISTORY.THIS_METHOD)
        {
            string retVal;

            try
            {
                StackTrace st = new StackTrace(new StackFrame(lastMethod.ToInt()));
                retVal = String.Format("{0}.{1}", st.GetFrame(0).GetMethod().ReflectedType.FullName, st.GetFrame(0).GetMethod().Name);
            }
            catch
            {
                retVal = "";
            }

            return retVal;
        }
        #endregion
    }
}
