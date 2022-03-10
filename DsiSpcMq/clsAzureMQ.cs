using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Net.NetworkInformation;
using System.Configuration;
using Azure.Messaging.ServiceBus;

namespace DsiSpcMq
{
    internal class clsAzureMQ
    {
        #region 변수
        private string TokenserverUrl = string.Empty;
        private string businessNo = string.Empty;
        private string companyId = string.Empty;
        private string dbInstanceNm = string.Empty;

        private string connectionString = string.Empty;
        private string topicName = string.Empty;

        private string standardKey = string.Empty;
        private string measureKey = string.Empty;
        private string StatKey = string.Empty;
        private string BadKey = string.Empty;
        
        public const string AuthKey = "4cb8014a67f76cc5";

        public string macAddress = string.Empty;
        public string Token = string.Empty;
        public string _token = string.Empty;
        
        #endregion

        public clsAzureMQ(string _businessNo, string _TokenserverUrl, string _NameSpace, string _KeyName, string _Key, string _topicName, string _standardKey, string _measureKey, string _StatKey, string _BadKey)
        {
            businessNo = _businessNo;
            TokenserverUrl = _TokenserverUrl;
            connectionString = string.Format("Endpoint=sb://{0}.servicebus.windows.net/;SharedAccessKeyName={1};SharedAccessKey={2};EntityPath={3}"
                                            , _NameSpace
                                            , _KeyName
                                            , _Key
                                            , _topicName
                                            );
            topicName = _topicName;

            standardKey = _standardKey;
            measureKey = _measureKey;
            StatKey = _StatKey;
            BadKey = _BadKey;
            
            macAddress = GetMacAddress();
            
            string strrCode = "";
            string strrMsg = "";
            if (GetMethodAgentMqBasicInfo(ref strrCode, ref strrMsg))
            {

            }
        }
        #region Json 파싱

        public string DataRowToJson(DataRow drRow)
        {
            if (drRow == null) return "";
            return new JObject(drRow.Table.Columns.Cast<DataColumn>().Select(c => new JProperty(c.ColumnName, JToken.FromObject(drRow[c])))).ToString(Formatting.None);
        }

        public string DataTableToJson(DataTable dtData)
        {
            return JsonConvert.SerializeObject(dtData);
        }

        public DataTable JsonToDataTable(string jsonSting)
        {
            DataTable dtData = (DataTable)JsonConvert.DeserializeObject(jsonSting, (typeof(DataTable)));
            return dtData;
        }

        #endregion

        #region Azure MQ 관련
        public bool GetMethodAgentMqBasicInfo(ref string rCode, ref string rMsg)
        {
            string _json = string.Empty;

            string code = string.Empty;
            string message = string.Empty;

            bool result = false;

            try
            {
                StringBuilder dataParams = new StringBuilder();

                string encryptCompanyId = Encrypt(businessNo, AuthKey);
                string encryptMacAdresss = Encrypt(macAddress, AuthKey);

                _json = $@"

{{
    ""company_id"": ""{encryptCompanyId}"",
    ""mac_address"": ""{encryptMacAdresss}"",
    ""token"": ""N/A""
}}";

                dataParams.Append(_json);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(TokenserverUrl);
                request.ContentType = "application/json;charset=UTF-8";
                request.Method = "POST";

                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(_json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                Stream stReadData = response.GetResponseStream();
                StreamReader srReadData = new StreamReader(stReadData, Encoding.GetEncoding("utf-8"), true);

                string strResult = "";

                using (var sr = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8"), true))
                {
                    strResult = sr.ReadToEnd();
                }

                Token = "";

                if (!string.IsNullOrEmpty(strResult))
                {
                    JObject obj = JObject.Parse(strResult);

                    if (!string.IsNullOrEmpty(obj["code"].ToString()))
                    {
                        code = obj["code"].ToString();
                        message = obj["message"].ToString();
                        rCode = code;
                        rMsg = message;

                        if (code == "201")
                        {
                            Token = obj["token"].ToString();
                            dbInstanceNm = obj["dbInstanceNm"].ToString();
                            // 인증토큰 파일로 저장 안하고 매번체크하도록 수정 220127
                            //saveDayInitialToken(Token, dbInstanceNm);
                            return true;
                        }
                        else
                        {
                            Token = "";
                            return result;
                        }
                    }
                }
            }
            catch (WebException webEx)
            {
                throw new Exception(webEx.Message + " 업체코드와 MacAdrress로 토큰인증을 받는데 실패하였습니다.");
            }
            catch (JsonReaderException jsonEx)
            {
                throw new Exception(jsonEx.Message);
            }
            return result;
        }

        public bool sendToken()
        {
            if (string.IsNullOrEmpty(Token))
            {
                return false;
            }

            StringBuilder dataParams = new StringBuilder();
            string _json = string.Empty;

            string code = string.Empty;
            string message = string.Empty;

            bool result = false;

            try
            {
                _json = $@"
{{
    ""token"": ""{Token}""
}}";

                dataParams.Append(_json);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(TokenserverUrl);
                request.ContentType = "application/json;charset=UTF-8";
                request.Method = "POST";

                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(_json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                Stream stReadData = response.GetResponseStream();
                StreamReader srReadData = new StreamReader(stReadData, Encoding.GetEncoding("utf-8"), true);

                string strResult;

                using (var sr = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8"), true))
                {
                    strResult = sr.ReadToEnd();
                }

                if (!string.IsNullOrEmpty(strResult))
                {
                    JObject obj = JObject.Parse(strResult);

                    if (!string.IsNullOrEmpty(obj["code"].ToString()))
                    {
                        code = obj["code"].ToString();
                        message = obj["message"].ToString();

                        if (code == "200")
                        {
                            _token = obj["token"].ToString();
                            // 인증토큰 파일로 저장 안하고 매번체크하도록 수정 220127
                            //savePeriodToken(_token);
                            result = true;
                        }
                        else
                        {
                            _token = "";
                            result = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return result;
        }
        // 인증토큰 파일로 저장 안하고 매번체크하도록 수정 220127
        //public bool chkDayInitialToken()
        //{
        //    try
        //    {
        //        DateTime dtCurrent = DateTime.Now;

        //        Token = cryptConfig.GetValue(strSectionInitial, strKeyTokenInitial);

        //        if (string.IsNullOrEmpty(Token)) return false;

        //        string strValueDate = cryptConfig.GetValue(strSectionInitial, strKeyDateInitial);

        //        //2020-06-30 협력사SPC 시스템 화면정의서에 의한 수정/개발
        //        //1회 / 1일 인증을 하는데
        //        //두번재는 인증을 하지 않아 instance 명이 누락이 되는 현상 발생
        //        //--> config 파일에 instance name 관리 추가
        //        dbInstanceNm = cryptConfig.GetValue(strSectionInitial, strKeyInstanceInitial);

        //        if (dtCurrent.ToString("yyyy-MM-dd").Equals(strValueDate) && !dbInstanceNm.Trim().Equals(""))
        //        {
        //            return true;
        //        }
        //        else return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(ex.Message);
        //    }
        //}

        // 인증토큰 파일로 저장 안하고 매번체크하도록 수정 220127
        //public bool chkPeriodToken()
        //{
        //    try
        //    {
        //        DateTime dtCurrent = DateTime.Now;

        //        Token = cryptConfig.GetValue(strSectionPeriod, strKeyTokenPeriod);

        //        if (string.IsNullOrEmpty(Token)) return false;

        //        string strValueDate = cryptConfig.GetValue(strSectionPeriod, strKeyDatePeriod);

        //        DateTime chkTime = Convert.ToDateTime(strValueDate);

        //        TimeSpan dateDiff = dtCurrent - chkTime;

        //        if (dateDiff.Minutes < chkPeriodTokenTime) return true;
        //        else return false;

        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(ex.Message);
        //    }
        //}

        // 인증토큰 파일로 저장 안하고 매번체크하도록 수정 220127
        //private void saveDayInitialToken(string dayInitialToken, string dbInstanceNm)
        //{
        //    DateTime dtCurrent = DateTime.Now;
        //    string strInitialDate = dtCurrent.ToString("yyyy-MM-dd");

        //    cryptConfig.SetValue(strSectionInitial, strKeyDateInitial, strInitialDate);
        //    cryptConfig.SetValue(strSectionInitial, strKeyTokenInitial, dayInitialToken);
        //    //2020-06-30 협력사SPC 시스템 화면정의서에 의한 수정/개발
        //    //1회 / 1일 인증을 하는데
        //    //두번재는 인증을 하지 않아 instance 명이 누락이 되는 현상 발생
        //    //--> config 파일에 instance name 관리 추가
        //    cryptConfig.SetValue(strSectionInitial, strKeyInstanceInitial, dbInstanceNm);
        //}

        // 인증토큰 파일로 저장 안하고 매번체크하도록 수정 220127
        //private void savePeriodToken(string periodToken)
        //{
        //    DateTime dtCurrent = DateTime.Now;
        //    string strPeriodDate = dtCurrent.ToString("yyyy-MM-dd HH:mm:ss");

        //    cryptConfig.SetValue(strSectionPeriod, strKeyDatePeriod, strPeriodDate);
        //    cryptConfig.SetValue(strSectionPeriod, strKeyTokenPeriod, periodToken);
        //}

        public async Task<bool> _sendTopicMq(string _strMessage, string _CorrelationId)
        {
            // 토큰을 파일형태로 export시키지않고 내부저장하기때문에 1일 토큰기간 체크를 하지않아 주석처리함
            //bool result = sendToken();
            //if (!result) return false;

            ServiceBusClient client = null;

            // create a sender for the queue 
            ServiceBusSender sender = null;

            // create a message that we can send
            ServiceBusMessage message = null;

            try
            {
                client = new ServiceBusClient(connectionString);
                sender = client.CreateSender(topicName);
                
                message = new ServiceBusMessage(Encoding.UTF8.GetBytes(_strMessage));
                //message.ContentType = "application/json";
                //message.MessageId = "sub2";
                message.CorrelationId = _CorrelationId;

                // send the message
                await sender.SendMessageAsync(message);
                
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                await sender.DisposeAsync();
                await client.DisposeAsync();
            }
        }

        public bool SendTopicMq(DataTable dtData, string strTableNm, string CorrelationId)
        {
            //2020-06-30 협력사SPC 시스템 화면정의서에 의한 수정/개발
            // bool return 되게 수정
            if (dtData == null || dtData.Rows.Count == 0) return false;

            string strData = DataTableToJson(dtData);

            int mqRecvCount = dtData.Rows.Count;

            string strMessage = $@"
{{
    ""companyId"": ""{businessNo}"",
    ""dbInstanceNm"": ""{dbInstanceNm}"",
    ""tblName"": ""{strTableNm}"",
    ""data"": {strData},
    ""mqRecvCount"": ""{mqRecvCount}""
}}";

            return _sendTopicMq(strMessage, CorrelationId).Result;
        }

        public bool SendTopicMqBasicInfo(DataTable dtData, string strTableNm)
        {
            return SendTopicMq(dtData, strTableNm, standardKey);
        }

        public bool SendTopicMqMeasure(DataTable dtData, string strTableNm)
        {
            return SendTopicMq(dtData, strTableNm, measureKey);
        }

        public bool SendTopicMqStat(DataTable dtData, string strTableNm)
        {
            return SendTopicMq(dtData, strTableNm, StatKey);
        }

        public bool SendTopicMqAbnormalSpecOut(DataTable dtData, string strTableNm)
        {
            return SendTopicMq(dtData, strTableNm, BadKey);
        }


        #endregion

        private string Encrypt(string s, string key)
        {
            StringBuilder sbResult = new StringBuilder();

            byte[] KeyArray = UTF8Encoding.UTF8.GetBytes(key);
            byte[] EncryptArray = UTF8Encoding.UTF8.GetBytes(s);

            RijndaelManaged Rdel = new RijndaelManaged();
            Rdel.Mode = CipherMode.ECB;
            Rdel.Padding = PaddingMode.Zeros;
            Rdel.Key = KeyArray;

            ICryptoTransform CtransForm = Rdel.CreateEncryptor();
            byte[] ResultArray = CtransForm.TransformFinalBlock(EncryptArray, 0, EncryptArray.Length);

            foreach (byte b in ResultArray)
            {
                sbResult.AppendFormat("{0:x2}", b);
            }

            return sbResult.ToString();
        }

        private string GetMacAddress()
        {
            return NetworkInterface.GetAllNetworkInterfaces()[0].GetPhysicalAddress().ToString();
        }
    }
}
