using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Net.NetworkInformation;
using System.Configuration;

namespace DsiSpcMq
{
    internal class clsRabbitMQ
    {
        #region 변수
        private string serverUrl = string.Empty;
        private string businessNo = string.Empty;
        private string serverHostNm = string.Empty;
        private int serverPort = 0;
        private string userName = string.Empty;
        private string passWord = string.Empty;
        private string companyId = string.Empty;
        private string dbInstanceNm = string.Empty;


        //기준정보
        private static readonly string RoutingPatternBasicInfo = clsCrypt.RSADecrypt(global::DsiSpcMq.Properties.Resources.RoutingPatternBasicInfo.ToString());
        private static readonly string RoutingKeyBasicInfo = clsCrypt.RSADecrypt(global::DsiSpcMq.Properties.Resources.RoutingKeyBasicInfo.ToString());
        private static readonly string QueuesBasicInfo = clsCrypt.RSADecrypt(global::DsiSpcMq.Properties.Resources.QueuesBasicInfo.ToString());


        //측정 데이터
        private static readonly string RoutingPatternMeasure = clsCrypt.RSADecrypt(global::DsiSpcMq.Properties.Resources.RoutingPatternMeasure.ToString());
        //220117 interface 파라미터 명칭 줄임으로 인한 key 변경 KJH
        //public const string RoutingKeyMeasureOne = "spc.data.partner.measure.one";
        private static readonly string RoutingKeyMeasureOne = clsCrypt.RSADecrypt(global::DsiSpcMq.Properties.Resources.RoutingKeyMeasureOne.ToString());
        private static readonly string QueuesMeasureOne = clsCrypt.RSADecrypt(global::DsiSpcMq.Properties.Resources.QueuesMeasureOne.ToString());

        private static readonly string RoutingKeyMeasureTwo = clsCrypt.RSADecrypt(global::DsiSpcMq.Properties.Resources.RoutingKeyMeasureTwo.ToString());
        private static readonly string QueuesMeasureTwo = clsCrypt.RSADecrypt(global::DsiSpcMq.Properties.Resources.QueuesMeasureTwo.ToString());

        private static readonly string RoutingKeyMeasureThree = clsCrypt.RSADecrypt(global::DsiSpcMq.Properties.Resources.RoutingKeyMeasureThree.ToString());
        private static readonly string QueuesMeasureThree = clsCrypt.RSADecrypt(global::DsiSpcMq.Properties.Resources.QueuesMeasureThree.ToString());

        //통계데이터
        private static readonly string RoutingPatternStat = clsCrypt.RSADecrypt(global::DsiSpcMq.Properties.Resources.RoutingPatternStat.ToString());
        //220117 interface 파라미터 명칭 줄임으로 인한 key 변경 KJH
        //public const string RoutingKeyStatOne = "spc.data.partner.stat.one";
        private static readonly string RoutingKeyStatOne = clsCrypt.RSADecrypt(global::DsiSpcMq.Properties.Resources.RoutingKeyStatOne.ToString());
        private static readonly string QueuesStatOne = clsCrypt.RSADecrypt(global::DsiSpcMq.Properties.Resources.QueuesStatOne.ToString());

        private static readonly string RoutingKeyStatTwo = clsCrypt.RSADecrypt(global::DsiSpcMq.Properties.Resources.RoutingKeyStatTwo.ToString());
        private static readonly string QueuesStatTwo = clsCrypt.RSADecrypt(global::DsiSpcMq.Properties.Resources.QueuesStatTwo.ToString());

        private static readonly string RoutingKeyStatThree = clsCrypt.RSADecrypt(global::DsiSpcMq.Properties.Resources.RoutingKeyStatThree.ToString());
        private static readonly string QueuesStatThree = clsCrypt.RSADecrypt(global::DsiSpcMq.Properties.Resources.QueuesStatThree.ToString());

        //기타 정보
        private static readonly string RoutingPatternLegacy = clsCrypt.RSADecrypt(global::DsiSpcMq.Properties.Resources.RoutingPatternLegacy.ToString());
        private static readonly string RoutingKeyLegacy = clsCrypt.RSADecrypt(global::DsiSpcMq.Properties.Resources.RoutingKeyLegacy.ToString());
        private static readonly string QueuesLegacy = clsCrypt.RSADecrypt(global::DsiSpcMq.Properties.Resources.QueuesLegacy.ToString());


        public const string AuthKey = "4cb8014a67f76cc5";

        public string macAddress = string.Empty;
        public string Token = string.Empty;
        public string _token = string.Empty;

        ////하루 한 번
        //private string strSectionInitial = "MQInitialCert";
        //private string strKeyDateInitial = "MQInitialDate";
        //private string strKeyTokenInitial = "MQInitialToken";
        //private string strKeyInstanceInitial = "MQInitialInstance";

        ////15분 마다
        //private string strSectionPeriod = "MQPeriodCert";
        //private string strKeyDatePeriod = "MQPeriodDate";
        //private string strKeyTokenPeriod = "MQPeriodToken";

        //private clsCryptReadWrite cryptConfig = new clsCryptReadWrite();

        //private int chkPeriodTokenTime = 14;


        #endregion

        public clsRabbitMQ(string _businessNo, string _serverUrl, string _serverHostNm, string _serverPort, string _userName, string _password)
        {
            serverUrl = _serverUrl;
            businessNo = _businessNo;
            serverHostNm = _serverHostNm;
            int.TryParse(_serverPort, out serverPort);
            userName = _userName;
            passWord = _password;

            macAddress = GetMacAddress();
            // 인증토큰 파일로 저장 안하고 매번체크하도록 수정 220127
            //bool result = chkDayInitialToken();
            //if (!result)
            //{
            //    string strrCode = "";
            //    string strrMsg = "";
            //    if (GetMethodAgentMqBasicInfo(ref strrCode, ref strrMsg))
            //    {

            //    }
            //}

            string strrCode = "";
            string strrMsg = "";
            if (GetMethodAgentMqBasicInfo(ref strrCode, ref strrMsg))
            {

            }
        }
        ////2020-06-30 
        ////김성남 부장님 지시
        ////사용자 처리 인증 실패시 메시지 표출
        ////서버프로그램 3개 모두 수정해야해서 보류 됨
        //public bool Init(ref string rCode, ref string rMsg)
        //{
        //    rCode = "";
        //    rMsg = "";
        //    bool result = chkDayInitialToken();
        //    if (!result)
        //    {
        //        string strrCode = "";
        //        string strrMsg = "";
        //        result = GetMethodAgentMqBasicInfo(ref strrCode, ref strrMsg);
        //        rCode = strrCode;
        //        rMsg = strrMsg;
        //    }
        //    return result;
        //}


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

        #region rabbit MQ 관련
        //public void GetMethodAgentMqBasicInfoOld()
        //{
        //    try
        //    {
        //        StringBuilder dataParams = new StringBuilder();

        //        dataParams.Append("businessNo=" + businessNo);
        //        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serverUrl + dataParams);

        //        request.Method = "GET";
        //        HttpWebResponse response = (HttpWebResponse)request.GetResponse();

        //        Stream stReadData = response.GetResponseStream();
        //        StreamReader srReadData = new StreamReader(stReadData, Encoding.GetEncoding("utf-8"), true);

        //        string strResult;

        //        using (var sr = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8"), true))
        //        {
        //            strResult = sr.ReadToEnd();
        //        }

        //        if (!string.IsNullOrEmpty(strResult))
        //        {
        //            JObject obj = JObject.Parse(strResult);

        //            if (!string.IsNullOrEmpty(obj["data"].ToString()))
        //            {
        //                companyId = obj["data"]["company_id"].ToString();
        //                dbInstanceNm = obj["data"]["db_instance_nm"].ToString();
        //            }
        //        }
        //    }
        //    catch (WebException webEx)
        //    {
        //        throw new Exception(webEx.Message);
        //    }
        //    catch (JsonReaderException jsonEx)
        //    {
        //        throw new Exception(jsonEx.Message);
        //    }
        //}

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

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serverUrl);
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

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serverUrl);
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

        private bool sendTopicMq(string routingPattern, string routingKey, string queueName, string strMessage)
        {
            //2020-06-30 협력사SPC 시스템 화면정의서에 의한 수정/개발
            //Data 전송 시도 후
            //send_YN 이 Y 로 변경이 되는데
            //전송이 실패되면   send_YN 을 N 로 유지 
            // bool return 되게 수정
            try
            {
                bool result = sendToken();
                if (!result) return false;

                var factory = new ConnectionFactory() { HostName = serverHostNm, Port = serverPort, UserName = userName, Password = passWord };
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    string exchangeName = "spc-topic";

                    channel.ExchangeDeclare(exchange: exchangeName, type: "topic");
                    channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                    channel.QueueBind(queueName, exchangeName, routingPattern);

                    var body = Encoding.UTF8.GetBytes(strMessage);
                    channel.BasicPublish(exchange: exchangeName, routingKey: routingKey, basicProperties: null, body: body);
                    //Console.WriteLine(" [x] Sent Topic'{0}':'{1}'", routingKey, strMessage);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public bool SendTopicMq(string routingPattern, string routingKey, string queueName, DataTable dtData, string strTableNm)
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
            
            return sendTopicMq(routingPattern, routingKey, queueName, strMessage);
        }

        public bool SendTopicMqBasicInfo(DataTable dtData, string strTableNm)
        {
            //2020-06-30 협력사SPC 시스템 화면정의서에 의한 수정/개발
            // bool return 되게 수정
            return SendTopicMq(RoutingPatternBasicInfo, RoutingKeyBasicInfo, QueuesBasicInfo, dtData, strTableNm);
        }

        public bool SendTopicMqMeasure(DataTable dtData, string strTableNm)
        {
            return SendTopicMq(RoutingPatternMeasure, RoutingKeyMeasureOne, QueuesMeasureOne, dtData, strTableNm);
        }

        public bool SendTopicMqStat(DataTable dtData, string strTableNm)
        {
            return SendTopicMq(RoutingPatternStat, RoutingKeyStatOne, QueuesStatOne, dtData, strTableNm);
        }

        public bool SendTopicMqAbnormalSpecOut(DataTable dtData, string strTableNm)
        {
            return SendTopicMq(RoutingPatternStat, RoutingKeyStatTwo, QueuesStatTwo, dtData, strTableNm);
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
