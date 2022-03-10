using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DsiSpcMq
{
    internal class clsCryptReadWrite
    {
        private string IniFileName = @".\cryptConfig.ini";

        [DllImport("kernel32")]
        public static extern bool WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);

        [DllImport("kernel32")]
        public static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);

        public clsCryptReadWrite() { }

        public clsCryptReadWrite(string filename) { if (!string.IsNullOrEmpty(filename)) IniFileName = filename; }

        public string GetValue(string strSection, string strKey, string strDefault = "")
        {
            StringBuilder strResult = new StringBuilder(65536);
            string result = string.Empty;

            try
            {
                if (GetPrivateProfileString(strSection, strKey, strDefault, strResult, 65536, IniFileName) <= 0)
                {
                    return "";
                }
                string strTest = "12345";
                string stEndata = clsCrypt.DESEncrypt(strTest);
                string stDedata = clsCrypt.DESDecrypt(stEndata);


                //result = clsCrypt.DESDecrypt(strResult.ToString() + "=");
                result = strResult.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return result.ToString();
        }

        public void SetValue(string strSection, string strKey, string strValue)
        {
            try
            {
                WritePrivateProfileString(strSection, strKey, strValue, IniFileName);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public string GetCryptValue(string strSection, string strKey, string strDefault = "")
        {
            StringBuilder cryptResult = new StringBuilder();
            string result = string.Empty;

            try
            {
                string cryptSection = clsCrypt.DESEncrypt(strSection);
                string cryptKey = clsCrypt.DESEncrypt(strKey);

                if (GetPrivateProfileString(cryptSection, cryptKey, strDefault, cryptResult, 255, IniFileName) <= 0)
                {
                    return "";
                }

                //result = clsCrypt.DESDecrypt(cryptResult.ToString());
                result = cryptResult.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return result.ToString();
        }

        public void SetCryptValue(string strSection, string strKey, string strValue)
        {
            try
            {
                string cryptSection = clsCrypt.DESEncrypt(strSection);
                string cryptKey = clsCrypt.DESEncrypt(strKey);
                string cryptValue = clsCrypt.DESEncrypt(strValue);

                WritePrivateProfileString(cryptSection, cryptKey, cryptValue, IniFileName);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
