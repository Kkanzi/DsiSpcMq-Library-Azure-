using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DsiSpcMq
{
    internal class clsCrypt
    {
        #region 암호화키 (절대 변경 및 공개하지 말 것)

        // DES암복호화키 : 절대 수정하지 말 것(복호화 못 함) -> 수정할 경우, 기존 데이터 다시 다 만들어야 함.
        private const string desKey = "x1F9dWXQ";

        //RSA암복호화키 : 절대 수정하지 말 것(복호화 못 함) -> 수정할 경우, 기존 데이터 다시 다 만들어야 함.
        private const string privateKey = "<RSAKeyValue><Modulus>68uAL2MX7FRcagVd+Xdgt/E1EodXZUfIBdnn+galtXNnqMNuqaxUkuic8yET12OtiD2ghtWF8Bi7wTfffCcCylefqaUjDPTulwCCNIQxOLa42iodrQLIRdcnV4LZXirpFmbRsc3zbMH54pO00NGgf0o7drJDAWxzq/1iM8F3a3s=</Modulus><Exponent>AQAB</Exponent><P>+uChZhNU5Cqd5YbmfTfjqfaMjLt8C161nGH3fED8KPIV/2xO+J4gUQRoGXJbcNSzYgf5v6rRO47U4Q1TWWDJEw==</P><Q>8JwIJxoGFlP/aLI58idkAk2Fe8nsfg8qaLAtIaHKrYrPEWZ7EVyUO8VP8RytXzIC850nhXXocxw3v32jom/I+Q==</Q><DP>62x4iQ2DEEpdudKJ4N/dqNVQt5AIq7LIwmO8lsF04AetVPASe4QH139HIPoLjSpM26WYXKCzkCxM4JRcrvcAOQ==</DP><DQ>We/o+Dy5A8WYFdlw4Xwp3NZ/S7s5pBElKAaaiBTC/sWBCx8EZ4P0gLcLX7P5djjqc4dNy4w8PDLS/8gFz2T7eQ==</DQ><InverseQ>MTGRmvsB1VP1rd+IRiZzXvXq2FtKqoBpC0B36qwAtkjPhy1ojg40Xt5mlSoB5bSwYv4uj/n9BAAyl5u9dEEtbw==</InverseQ><D>6GYFLC9dt9cZ0oEBs0u+ruz0oxNzxuXtth6kLeB5WJKq+0HLgz3PiY/siRDz7llXAq3C1sICpbaq7vAzu7jzXDiAcqoqxcUQsijPMBSpKwpkOZAq9Goq96oMrMHQbSyPCj045JNZQ61gkLvl2fAB83dl6EKNA+qAntkxZrtkDHE=</D></RSAKeyValue>";
        private const string publicKey = "<RSAKeyValue><Modulus>68uAL2MX7FRcagVd+Xdgt/E1EodXZUfIBdnn+galtXNnqMNuqaxUkuic8yET12OtiD2ghtWF8Bi7wTfffCcCylefqaUjDPTulwCCNIQxOLa42iodrQLIRdcnV4LZXirpFmbRsc3zbMH54pO00NGgf0o7drJDAWxzq/1iM8F3a3s=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        #endregion

        public clsCrypt()
        {
            //
            // TODO: 여기에 생성자 논리를 추가합니다.
            //
        }

        //------------------------------------------------------------------------
        #region MD5 Hash

        public static string MD5HashCrypt(string val)
        {
            byte[] data = Convert.FromBase64String(val);
            // This is one implementation of the abstract class MD5.
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(data);

            return Convert.ToBase64String(result);
        }

        #endregion //MD5 Hash

        //------------------------------------------------------------------------
        #region DES암복호화

        #region private 함수
        //문자열->유니코드 바이트 배열
        private static Byte[] ConvertStringToByteArray(String s)
        {
            return (new UnicodeEncoding()).GetBytes(s);
        }

        //유니코드 바이트 배열->문자열
        private static string ConvertByteArrayToString(byte[] b)
        {
            return (new UnicodeEncoding()).GetString(b, 0, b.Length);
        }

        //문자열->안시 바이트 배열
        private static Byte[] ConvertStringToByteArrayA(String s)
        {
            return (new ASCIIEncoding()).GetBytes(s);
        }

        //안시 바이트 배열->문자열
        private static string ConvertByteArrayToStringA(byte[] b)
        {
            return (new ASCIIEncoding()).GetString(b, 0, b.Length);
        }

        //문자열->Base64 바이트 배열
        private static Byte[] ConvertStringToByteArrayB(String s)
        {
            return Convert.FromBase64String(s);
        }

        //Base64 바이트 배열->문자열
        private static string ConvertByteArrayToStringB(byte[] b)
        {
            return Convert.ToBase64String(b);
        }

        //문자열 암호화
        private static string DesEncrypt(string str, string key)
        {
            //키 유효성 검사
            byte[] btKey = ConvertStringToByteArrayA(key);

            //키가 8Byte가 아니면 예외발생
            if (btKey.Length != 8)
            {
                throw (new Exception("Invalid key. Key length must be 8 byte."));
            }

            //소스 문자열
            byte[] btSrc = ConvertStringToByteArray(str);
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            des.Key = btKey;
            des.IV = btKey;

            ICryptoTransform desencrypt = des.CreateEncryptor();

            MemoryStream ms = new MemoryStream();

            CryptoStream cs = new CryptoStream(ms, desencrypt,
             CryptoStreamMode.Write);

            cs.Write(btSrc, 0, btSrc.Length);
            cs.FlushFinalBlock();


            byte[] btEncData = ms.ToArray();

            return (ConvertByteArrayToStringB(btEncData));
        }//end of func DesEncrypt

        //문자열 복호화
        private static string DesDecrypt(string str, string key)
        {
            //키 유효성 검사
            byte[] btKey = ConvertStringToByteArrayA(key);

            //키가 8Byte가 아니면 예외발생
            if (btKey.Length != 8)
            {
                throw (new Exception("Invalid key. Key length must be 8 byte."));
            }


            byte[] btEncData = ConvertStringToByteArrayB(str);
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            des.Key = btKey;
            des.IV = btKey;

            ICryptoTransform desdecrypt = des.CreateDecryptor();

            MemoryStream ms = new MemoryStream();

            CryptoStream cs = new CryptoStream(ms, desdecrypt,
             CryptoStreamMode.Write);

            cs.Write(btEncData, 0, btEncData.Length);

            cs.FlushFinalBlock();

            byte[] btSrc = ms.ToArray();


            return (ConvertByteArrayToString(btSrc));

        }//end of func DesDecrypt

        #endregion

        #region public 함수
        public static string DESEncrypt(string inStr)
        {
            return DesEncrypt(inStr, desKey);
        }

        // Public Function
        public static string DESDecrypt(string inStr) // 복호화
        {
            return DesDecrypt(inStr, desKey);
        }

        #endregion

        #endregion //DES암복호화

        //------------------------------------------------------------------------
        #region RSA암복호화
        //RSA 암호화
        public static string RSAEncrypt(string strValue, string sPubKey)
        {
            System.Security.Cryptography.RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(); //암호화
            rsa.FromXmlString(sPubKey);

            //암호화할 문자열을 UFT8인코딩
            byte[] inbuf = (new UTF8Encoding()).GetBytes(strValue);

            //암호화
            byte[] encbuf = rsa.Encrypt(inbuf, false);

            //암호화된 문자열 Base64인코딩
            return Convert.ToBase64String(encbuf);
        }
        //RSA 복호화
        public static string RSADecrypt(string strValue, string sPrvKey)
        {
            //RSA객체생성
            System.Security.Cryptography.RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(); //복호화
            rsa.FromXmlString(sPrvKey);

            //sValue문자열을 바이트배열로 변환
            byte[] srcbuf = Convert.FromBase64String(strValue);

            //바이트배열 복호화
            byte[] decbuf = rsa.Decrypt(srcbuf, false);

            //복호화 바이트배열을 문자열로 변환
            string sDec = (new UTF8Encoding()).GetString(decbuf, 0, decbuf.Length);
            return sDec;
        }

        public static string RSAEncrypt(string sValue)
        {
            return RSAEncrypt(sValue, publicKey);
        }

        public static string RSADecrypt(string sValue)
        {
            return RSADecrypt(sValue, privateKey);
        }

        #endregion
    }
}
