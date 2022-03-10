using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DsiSpcMq
{
    /// <summary>
    /// 두산 Message Queue Library
    /// </summary>
    public class DsiSpcMq : IDisposable
    {
        bool disposed = false;

        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);
        
        private string strTokenServerUrl = string.Empty;
        private string strNameSpace = string.Empty;
        private string strKeyName = string.Empty;
        private string strKey = string.Empty;
        private string strTopicName = string.Empty;
        private string strStandardKey = string.Empty;
        private string strMeasureKey = string.Empty;
        private string strStatKey = string.Empty;
        private string strBadKey = string.Empty;


        /// <summary>
        /// 내부에서 리소스 사용금지X, 외부 Config에서 파라미터로 받기위한 클래스 객체 선언
        /// clsRabbit 사용 안함
        /// </summary>
        /// <param name="TokenUrl">토큰발급URL</param>
        /// <param name="NameSpace">Azure네임스페이스</param>
        /// <param name="KeyName">Azure ConnectString의 KeyName</param>
        /// <param name="Key">Azure ConnectString의 Key</param>
        /// <param name="TopicName">Azure Topic명</param>
        /// <param name="StandardKey">Azure Topic으로 던질 기준정보 메세지의 필터명</param>
        /// <param name="MeasureKey">Azure Topic으로 던질 측정데이터 메세지의 필터명</param>
        /// <param name="StatKey">Azure Topic으로 던질 통계데이터 메세지의 필터명</param>
        /// <param name="BadKey">Azure Topic으로 던질 불량데이터 메세지의 필터명</param>
        public DsiSpcMq(string TokenUrl, string NameSpace, string KeyName, string Key, string TopicName, string StandardKey, string MeasureKey, string StatKey, string BadKey)
        {
            strTokenServerUrl = clsCrypt.RSADecrypt(TokenUrl);
            strNameSpace = clsCrypt.RSADecrypt(NameSpace);
            strKeyName = clsCrypt.RSADecrypt(KeyName);
            strKey = clsCrypt.RSADecrypt(Key);
            strTopicName = clsCrypt.RSADecrypt(TopicName);
            strStandardKey = clsCrypt.RSADecrypt(StandardKey);
            strMeasureKey = clsCrypt.RSADecrypt(MeasureKey);
            strStatKey = clsCrypt.RSADecrypt(StatKey);
            strBadKey = clsCrypt.RSADecrypt(BadKey);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            // Object.Finalize가 따로 구현되지 않은 경우, 아래 코드는 아무런 영향이 없다.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                handle.Dispose();
                // Free any other managed objects here.
            }

            disposed = true;
        }

        /// <summary>
        /// 측정데이터 전송<para/>
        /// lotId(string), factId(string), procId(string), lineId(string), godsId(string), inspId(string), seq(int), <para/> inspectDt(datetime), inspectData(decimal), crud(char), idxKey(int)
        /// </summary>
        /// <param name="businessNo">업체코드</param>
        /// <param name="dtData">DataTable</param>
        /// <returns>성공여부</returns>
        public bool syncInspectData(string businessNo, DataTable dtData)
        {
            if (string.IsNullOrEmpty(businessNo)) throw new Exception("businessNo has no value.");

            clsAzureMQ azureMq = new clsAzureMQ
            (
              _businessNo: businessNo
            , _TokenserverUrl: strTokenServerUrl
            , _NameSpace: strNameSpace
            , _KeyName: strKeyName
            , _Key: strKey
            , _topicName: strTopicName
            , _standardKey: strStandardKey
            , _measureKey: strMeasureKey
            , _StatKey: strStatKey
            , _BadKey: strBadKey
            );

            if (dtData == null || dtData.Rows.Count == 0)
            {
                // Scheduler발생시 데이터가없을때 지속적으로 로그메세지 발생하여 exception처리안함
                //throw new Exception("There is no data in Datatable.");
                return false;
            }

            DataTable dt = new DataTable();

            dt.Columns.Add("lotId", typeof(string));
            dt.Columns.Add("factId", typeof(string));
            dt.Columns.Add("procId", typeof(string));
            dt.Columns.Add("lineId", typeof(string));
            dt.Columns.Add("godsId", typeof(string));
            dt.Columns.Add("inspId", typeof(string));
            dt.Columns.Add("seq", typeof(int));
            // 명칭수정(MQ 데이터줄이기) 220114 kjh
            //dt.Columns.Add("inspectDt",     typeof(DateTime));
            //dt.Columns.Add("inspectData",   typeof(decimal));
            dt.Columns.Add("idt", typeof(DateTime));
            dt.Columns.Add("idata", typeof(decimal));
            // sql의 char는 c#에서 string으로 인식됨으로 인한 변경 220119 KJH
            //dt.Columns.Add("crud",          typeof(char));  
            dt.Columns.Add("crud", typeof(string));
            dt.Columns.Add("idxKey", typeof(int));

            // null 허용안함 추가 220119 KJH
            dt.Columns["lotId"].AllowDBNull = false;
            dt.Columns["factId"].AllowDBNull = false;
            dt.Columns["procId"].AllowDBNull = false;
            dt.Columns["lineId"].AllowDBNull = false;
            dt.Columns["godsId"].AllowDBNull = false;
            dt.Columns["inspId"].AllowDBNull = false;
            dt.Columns["idt"].AllowDBNull = false;
            dt.Columns["idata"].AllowDBNull = false;
            dt.Columns["idxKey"].AllowDBNull = false;

            // dt의 column Name과 일치하지않으면 무시하고 일치하는것만 병합
            dt.Merge(dtData, true, MissingSchemaAction.Ignore);

            return azureMq.SendTopicMqMeasure(dt, "tbl_spc_inspect_data");
        }


        /// <summary>
        /// 부서코드 전송 <para/>
        /// deptId(string), nameK(string), nameE(string), nameC(string), nameJ(string), <para/> insertDt(datetime), updateDt(datetime), insertUserId(string), insertUserName(string), updateUserId(string), updateUserName(string), useNChangedDt(datetime), useNChangedUserId(string), useNChangedUserName(string), <para/> useYn(char), crud(char), idxKey(int)
        /// </summary>
        /// <param name="businessNo">업체코드</param>
        /// <param name="dtData">DataTable</param>
        /// <returns>성공여부</returns>
        public bool syncDeptInfo(string businessNo, DataTable dtData)
        {
            if (string.IsNullOrEmpty(businessNo)) throw new Exception("businessNo has no value.");

            clsAzureMQ azureMq = new clsAzureMQ
            (
              _businessNo: businessNo
            , _TokenserverUrl: strTokenServerUrl
            , _NameSpace: strNameSpace
            , _KeyName: strKeyName
            , _Key: strKey
            , _topicName: strTopicName
            , _standardKey: strStandardKey
            , _measureKey: strMeasureKey
            , _StatKey: strStatKey
            , _BadKey: strBadKey
            );

            if (dtData == null || dtData.Rows.Count == 0)
            {
                // Scheduler발생시 데이터가없을때 지속적으로 로그메세지 발생하여 exception처리안함
                //throw new Exception("There is no data in Datatable.");
                return false;
            }

            DataTable dt = new DataTable();

            dt.Columns.Add("deptId", typeof(string));
            dt.Columns.Add("nameK", typeof(string));
            dt.Columns.Add("nameE", typeof(string));
            dt.Columns.Add("nameC", typeof(string));
            dt.Columns.Add("nameJ", typeof(string));
            dt.Columns.Add("insertDt", typeof(DateTime));
            dt.Columns.Add("updateDt", typeof(DateTime));
            dt.Columns.Add("insertUserId", typeof(string));
            dt.Columns.Add("insertUserName", typeof(string));
            dt.Columns.Add("updateUserId", typeof(string));
            dt.Columns.Add("updateUserName", typeof(string));
            dt.Columns.Add("useNChangedDt", typeof(DateTime));
            dt.Columns.Add("useNChangedUserId", typeof(string));
            dt.Columns.Add("useNChangedUserName", typeof(string));
            // sql의 char는 c#에서 string으로 인식됨으로 인한 변경 220119 KJH
            //dt.Columns.Add("useYn", typeof(char));
            //dt.Columns.Add("crud", typeof(char));
            dt.Columns.Add("useYn", typeof(string));
            dt.Columns.Add("crud", typeof(string));
            dt.Columns.Add("idxKey", typeof(int));

            // null 허용안함 추가 220119 KJH
            dt.Columns["deptId"].AllowDBNull = false;
            dt.Columns["idxKey"].AllowDBNull = false;

            // dt의 column Name과 일치하지않으면 무시하고 일치하는것만 병합
            dt.Merge(dtData, true, MissingSchemaAction.Ignore);

            return azureMq.SendTopicMqBasicInfo(dt, "tbl_com_dept");
        }

        /// <summary>
        /// 공장코드전송 <para/>
        /// factId(string), nameK(string), nameE(string), nameC(string), nameJ(string), <para/> insertDt(datetime), updateDt(datetime), insertUserId(string), insertUserName(string), updateUserId(string), updateUserName(string), useNChangedDt(datetime), useNChangedUserId(string), useNChangedUserName(string), <para/> useYn(char), crud(char), idxKey(int)
        /// </summary>
        /// <param name="businessNo">업체코드</param>
        /// <param name="dtData">DataTable</param>
        /// <returns>성공여부</returns>
        public bool syncFactInfo(string businessNo, DataTable dtData)
        {
            if (string.IsNullOrEmpty(businessNo)) throw new Exception("businessNo has no value.");

            clsAzureMQ azureMq = new clsAzureMQ
            (
              _businessNo: businessNo
            , _TokenserverUrl: strTokenServerUrl
            , _NameSpace: strNameSpace
            , _KeyName: strKeyName
            , _Key: strKey
            , _topicName: strTopicName
            , _standardKey: strStandardKey
            , _measureKey: strMeasureKey
            , _StatKey: strStatKey
            , _BadKey: strBadKey
            );

            if (dtData == null || dtData.Rows.Count == 0)
            {
                // Scheduler발생시 데이터가없을때 지속적으로 로그메세지 발생하여 exception처리안함
                //throw new Exception("There is no data in Datatable.");
                return false;
            }

            DataTable dt = new DataTable();

            dt.Columns.Add("factId", typeof(string));
            dt.Columns.Add("nameK", typeof(string));
            dt.Columns.Add("nameE", typeof(string));
            dt.Columns.Add("nameC", typeof(string));
            dt.Columns.Add("nameJ", typeof(string));
            dt.Columns.Add("insertDt", typeof(DateTime));
            dt.Columns.Add("updateDt", typeof(DateTime));
            dt.Columns.Add("insertUserId", typeof(string));
            dt.Columns.Add("insertUserName", typeof(string));
            dt.Columns.Add("updateUserId", typeof(string));
            dt.Columns.Add("updateUserName", typeof(string));
            dt.Columns.Add("useNChangedDt", typeof(DateTime));
            dt.Columns.Add("useNChangedUserId", typeof(string));
            dt.Columns.Add("useNChangedUserName", typeof(string));
            // sql의 char는 c#에서 string으로 인식됨으로 인한 변경 220119 KJH
            //dt.Columns.Add("useYn", typeof(char));
            //dt.Columns.Add("crud", typeof(char));
            dt.Columns.Add("useYn", typeof(string));
            dt.Columns.Add("crud", typeof(string));
            dt.Columns.Add("idxKey", typeof(int));

            // null 허용안함 추가 220119 KJH
            dt.Columns["factId"].AllowDBNull = false;
            dt.Columns["idxKey"].AllowDBNull = false;

            // dt의 column Name과 일치하지않으면 무시하고 일치하는것만 병합
            dt.Merge(dtData, true, MissingSchemaAction.Ignore);

            return azureMq.SendTopicMqBasicInfo(dt, "tbl_com_factory");
        }

        /// <summary>
        /// 공정정보 전송 <para/>
        /// procId(string), nameK(string), nameE(string), nameC(string), nameJ(string), <para/> insertDt(datetime), updateDt(datetime), insertUserId(string), insertUserName(string), updateUserId(string), updateUserName(string), useNChangedDt(datetime), useNChangedUserId(string), useNChangedUserName(string), <para/> useYn(char), crud(char), idxKey(int)
        /// </summary>
        /// <param name="businessNo">업체코드</param>
        /// <param name="dtData">DataTable</param>
        /// <returns>성공여부</returns>
        public bool syncProcInfo(string businessNo, DataTable dtData)
        {
            if (string.IsNullOrEmpty(businessNo)) throw new Exception("businessNo has no value.");

            clsAzureMQ azureMq = new clsAzureMQ
            (
              _businessNo: businessNo
            , _TokenserverUrl: strTokenServerUrl
            , _NameSpace: strNameSpace
            , _KeyName: strKeyName
            , _Key: strKey
            , _topicName: strTopicName
            , _standardKey: strStandardKey
            , _measureKey: strMeasureKey
            , _StatKey: strStatKey
            , _BadKey: strBadKey
            );

            if (dtData == null || dtData.Rows.Count == 0)
            {
                // Scheduler발생시 데이터가없을때 지속적으로 로그메세지 발생하여 exception처리안함
                //throw new Exception("There is no data in Datatable.");
                return false;
            }

            DataTable dt = new DataTable();

            dt.Columns.Add("procId", typeof(string));
            dt.Columns.Add("nameK", typeof(string));
            dt.Columns.Add("nameE", typeof(string));
            dt.Columns.Add("nameC", typeof(string));
            dt.Columns.Add("nameJ", typeof(string));
            dt.Columns.Add("insertDt", typeof(DateTime));
            dt.Columns.Add("updateDt", typeof(DateTime));
            dt.Columns.Add("insertUserId", typeof(string));
            dt.Columns.Add("insertUserName", typeof(string));
            dt.Columns.Add("updateUserId", typeof(string));
            dt.Columns.Add("updateUserName", typeof(string));
            dt.Columns.Add("useNChangedDt", typeof(DateTime));
            dt.Columns.Add("useNChangedUserId", typeof(string));
            dt.Columns.Add("useNChangedUserName", typeof(string));
            // sql의 char는 c#에서 string으로 인식됨으로 인한 변경 220119 KJH
            //dt.Columns.Add("useYn", typeof(char));
            //dt.Columns.Add("crud", typeof(char));
            dt.Columns.Add("useYn", typeof(string));
            dt.Columns.Add("crud", typeof(string));
            dt.Columns.Add("idxKey", typeof(int));

            // null 허용안함 추가 220119 KJH
            dt.Columns["procId"].AllowDBNull = false;
            dt.Columns["idxKey"].AllowDBNull = false;

            // dt의 column Name과 일치하지않으면 무시하고 일치하는것만 병합
            dt.Merge(dtData, true, MissingSchemaAction.Ignore);

            return azureMq.SendTopicMqBasicInfo(dt, "tbl_com_process");
        }

        /// <summary>
        /// 라인(설비)정보 전송 <para/>
        /// lineId(string), nameK(string), nameE(string), nameC(string), nameJ(string), <para/> insertDt(datetime), updateDt(datetime), insertUserId(string), insertUserName(string), updateUserId(string), updateUserName(string), useNChangedDt(datetime), useNChangedUserId(string), useNChangedUserName(string), <para/> useYn(char), crud(char), idxKey(int)
        /// </summary>
        /// <param name="businessNo">업체코드</param>
        /// <param name="dtData">DataTable</param>
        /// <returns>성공여부</returns>
        public bool syncLineInfo(string businessNo, DataTable dtData)
        {
            if (string.IsNullOrEmpty(businessNo)) throw new Exception("businessNo has no value.");

            clsAzureMQ azureMq = new clsAzureMQ
            (
              _businessNo: businessNo
            , _TokenserverUrl: strTokenServerUrl
            , _NameSpace: strNameSpace
            , _KeyName: strKeyName
            , _Key: strKey
            , _topicName: strTopicName
            , _standardKey: strStandardKey
            , _measureKey: strMeasureKey
            , _StatKey: strStatKey
            , _BadKey: strBadKey
            );

            if (dtData == null || dtData.Rows.Count == 0)
            {
                // Scheduler발생시 데이터가없을때 지속적으로 로그메세지 발생하여 exception처리안함
                //throw new Exception("There is no data in Datatable.");
                return false;
            }

            DataTable dt = new DataTable();

            dt.Columns.Add("lineId", typeof(string));
            dt.Columns.Add("nameK", typeof(string));
            dt.Columns.Add("nameE", typeof(string));
            dt.Columns.Add("nameC", typeof(string));
            dt.Columns.Add("nameJ", typeof(string));
            dt.Columns.Add("insertDt", typeof(DateTime));
            dt.Columns.Add("updateDt", typeof(DateTime));
            dt.Columns.Add("insertUserId", typeof(string));
            dt.Columns.Add("insertUserName", typeof(string));
            dt.Columns.Add("updateUserId", typeof(string));
            dt.Columns.Add("updateUserName", typeof(string));
            dt.Columns.Add("useNChangedDt", typeof(DateTime));
            dt.Columns.Add("useNChangedUserId", typeof(string));
            dt.Columns.Add("useNChangedUserName", typeof(string));
            // sql의 char는 c#에서 string으로 인식됨으로 인한 변경 220119 KJH
            //dt.Columns.Add("useYn", typeof(char));
            //dt.Columns.Add("crud", typeof(char));
            dt.Columns.Add("useYn", typeof(string));
            dt.Columns.Add("crud", typeof(string));
            dt.Columns.Add("idxKey", typeof(int));

            // null 허용안함 추가 220119 KJH
            dt.Columns["lineId"].AllowDBNull = false;
            dt.Columns["idxKey"].AllowDBNull = false;

            // dt의 column Name과 일치하지않으면 무시하고 일치하는것만 병합
            dt.Merge(dtData, true, MissingSchemaAction.Ignore);

            return azureMq.SendTopicMqBasicInfo(dt, "tbl_com_line");
        }

        /// <summary>
        /// 품번정보 전송 <para/>
        /// godsID(string), nameK(string), nameE(string), nameC(string), nameJ(string), division(string), <para/> insertDt(datetime), updateDt(datetime), insertUserId(string), insertUserName(string), updateUserId(string), updateUserName(string), useNChangedDt(datetime), useNChangedUserId(string), useNChangedUserName(string), <para/> useYn(char), crud(char), idxKey(int)
        /// </summary>
        /// <param name="businessNo">업체코드</param>
        /// <param name="dtData">DataTable</param>
        /// <returns>성공여부</returns>
        public bool syncGoodsInfo(string businessNo, DataTable dtData)
        {
            if (string.IsNullOrEmpty(businessNo)) throw new Exception("businessNo has no value.");

            clsAzureMQ azureMq = new clsAzureMQ
            (
              _businessNo: businessNo
            , _TokenserverUrl: strTokenServerUrl
            , _NameSpace: strNameSpace
            , _KeyName: strKeyName
            , _Key: strKey
            , _topicName: strTopicName
            , _standardKey: strStandardKey
            , _measureKey: strMeasureKey
            , _StatKey: strStatKey
            , _BadKey: strBadKey
            );

            if (dtData == null || dtData.Rows.Count == 0)
            {
                // Scheduler발생시 데이터가없을때 지속적으로 로그메세지 발생하여 exception처리안함
                //throw new Exception("There is no data in Datatable.");
                return false;
            }

            DataTable dt = new DataTable();

            dt.Columns.Add("godsId", typeof(string));
            dt.Columns.Add("nameK", typeof(string));
            dt.Columns.Add("nameE", typeof(string));
            dt.Columns.Add("nameC", typeof(string));
            dt.Columns.Add("nameJ", typeof(string));
            dt.Columns.Add("division", typeof(string));
            dt.Columns.Add("insertDt", typeof(DateTime));
            dt.Columns.Add("updateDt", typeof(DateTime));
            dt.Columns.Add("insertUserId", typeof(string));
            dt.Columns.Add("insertUserName", typeof(string));
            dt.Columns.Add("updateUserId", typeof(string));
            dt.Columns.Add("updateUserName", typeof(string));
            dt.Columns.Add("useNChangedDt", typeof(DateTime));
            dt.Columns.Add("useNChangedUserId", typeof(string));
            dt.Columns.Add("useNChangedUserName", typeof(string));
            // sql의 char는 c#에서 string으로 인식됨으로 인한 변경 220119 KJH
            //dt.Columns.Add("useYn", typeof(char));
            //dt.Columns.Add("crud", typeof(char));
            dt.Columns.Add("useYn", typeof(string));
            dt.Columns.Add("crud", typeof(string));
            dt.Columns.Add("idxKey", typeof(int));

            // null 허용안함 추가 220119 KJH
            dt.Columns["godsId"].AllowDBNull = false;
            dt.Columns["division"].AllowDBNull = false;
            dt.Columns["idxKey"].AllowDBNull = false;

            // dt의 column Name과 일치하지않으면 무시하고 일치하는것만 병합
            dt.Merge(dtData, true, MissingSchemaAction.Ignore);

            return azureMq.SendTopicMqBasicInfo(dt, "tbl_com_goods");
        }

        /// <summary>
        /// 검사항목 전송 <para/>
        /// inspId(string), nameK(string), nameE(string), nameC(string), nameJ(string), <para/> insertDt(datetime), updateDt(datetime), insertUserId(string), insertUserName(string), updateUserId(string), updateUserName(string), useNChangedDt(datetime), useNChangedUserId(string), useNChangedUserName(string), <para/> useYn(char), crud(char), idxKey(int)
        /// </summary>
        /// <param name="businessNo">업체코드</param>
        /// <param name="dtData">DataTable</param>
        /// <returns>성공여부</returns>
        public bool syncInspectInfo(string businessNo, DataTable dtData)
        {
            if (string.IsNullOrEmpty(businessNo)) throw new Exception("businessNo has no value.");

            clsAzureMQ azureMq = new clsAzureMQ
            (
              _businessNo: businessNo
            , _TokenserverUrl: strTokenServerUrl
            , _NameSpace: strNameSpace
            , _KeyName: strKeyName
            , _Key: strKey
            , _topicName: strTopicName
            , _standardKey: strStandardKey
            , _measureKey: strMeasureKey
            , _StatKey: strStatKey
            , _BadKey: strBadKey
            );

            if (dtData == null || dtData.Rows.Count == 0)
            {
                // Scheduler발생시 데이터가없을때 지속적으로 로그메세지 발생하여 exception처리안함
                //throw new Exception("There is no data in Datatable.");
                return false;
            }

            DataTable dt = new DataTable();

            dt.Columns.Add("inspId", typeof(string));
            dt.Columns.Add("nameK", typeof(string));
            dt.Columns.Add("nameE", typeof(string));
            dt.Columns.Add("nameC", typeof(string));
            dt.Columns.Add("nameJ", typeof(string));
            dt.Columns.Add("insertDt", typeof(DateTime));
            dt.Columns.Add("updateDt", typeof(DateTime));
            dt.Columns.Add("insertUserId", typeof(string));
            dt.Columns.Add("insertUserName", typeof(string));
            dt.Columns.Add("updateUserId", typeof(string));
            dt.Columns.Add("updateUserName", typeof(string));
            dt.Columns.Add("useNChangedDt", typeof(DateTime));
            dt.Columns.Add("useNChangedUserId", typeof(string));
            dt.Columns.Add("useNChangedUserName", typeof(string));
            // sql의 char는 c#에서 string으로 인식됨으로 인한 변경 220119 KJH
            //dt.Columns.Add("useYn", typeof(char));
            //dt.Columns.Add("crud", typeof(char));
            dt.Columns.Add("useYn", typeof(string));
            dt.Columns.Add("crud", typeof(string));
            dt.Columns.Add("idxKey", typeof(int));

            // null 허용안함 추가 220119 KJH
            dt.Columns["inspId"].AllowDBNull = false;
            dt.Columns["idxKey"].AllowDBNull = false;

            // dt의 column Name과 일치하지않으면 무시하고 일치하는것만 병합
            dt.Merge(dtData, true, MissingSchemaAction.Ignore);

            return azureMq.SendTopicMqBasicInfo(dt, "tbl_spc_inspect");
        }

        /// <summary>
        /// 스펙정보 전송 <para/>
        /// <para>factId(string), procId(string), lineId(string), godsId(string), inspId(string),</para> 
        /// <para>unit(string), unitNameK(string), unitNameE(string), unitNameJ(string), unitNameC(string), dataType(string), dataTypeNameK(string), dataTypeNameE(string), dataTypeNameJ(string), dataTypeNameC(string),</para> 
        /// <para> quaType(char), sampleSize(int), chartCode(string), specLower(decimal), specMid(decimal), specUpper(decimal),</para> 
        /// <para> lopl(decimal), opl(decimal), uopl(decimal), rlopl(decimal), ropl(decimal), ruopl(decimal), cpCpkUse(string), oplYn(char),</para> 
        /// <para> insertDt(datetime), updateDt(datetime), insertUserId(string), insertUserName(string), updateUserId(string), updateUserName(string), useNChangedDt(datetime), useNChangedUserId(string), useNChangedUserName(string),</para> 
        /// <para> useYn(char), crud(char), idxKey(int) </para>
        /// </summary>
        /// <param name="businessNo">업체코드</param>
        /// <param name="dtData">DataTable</param>
        /// <returns>성공여부</returns>
        public bool syncGoodsInspect(string businessNo, DataTable dtData)
        {
            if (string.IsNullOrEmpty(businessNo)) throw new Exception("businessNo has no value.");

            clsAzureMQ azureMq = new clsAzureMQ
            (
              _businessNo: businessNo
            , _TokenserverUrl: strTokenServerUrl
            , _NameSpace: strNameSpace
            , _KeyName: strKeyName
            , _Key: strKey
            , _topicName: strTopicName
            , _standardKey: strStandardKey
            , _measureKey: strMeasureKey
            , _StatKey: strStatKey
            , _BadKey: strBadKey
            );

            if (dtData == null || dtData.Rows.Count == 0)
            {
                // Scheduler발생시 데이터가없을때 지속적으로 로그메세지 발생하여 exception처리안함
                //throw new Exception("There is no data in Datatable.");
                return false;
            }

            DataTable dt = new DataTable();

            dt.Columns.Add("factId", typeof(string));
            dt.Columns.Add("procId", typeof(string));
            dt.Columns.Add("lineId", typeof(string));
            dt.Columns.Add("godsId", typeof(string));
            dt.Columns.Add("inspId", typeof(string));
            dt.Columns.Add("unit", typeof(string));
            dt.Columns.Add("unitNameK", typeof(string));
            dt.Columns.Add("unitNameE", typeof(string));
            dt.Columns.Add("unitNameJ", typeof(string));
            dt.Columns.Add("unitNameC", typeof(string));
            dt.Columns.Add("dataType", typeof(string));
            dt.Columns.Add("dataTypeNameK", typeof(string));
            dt.Columns.Add("dataTypeNameE", typeof(string));
            dt.Columns.Add("dataTypeNameJ", typeof(string));
            dt.Columns.Add("dataTypeNameC", typeof(string));
            // sql의 char는 c#에서 string으로 인식됨으로 인한 변경 220119 KJH
            //dt.Columns.Add("quaType", typeof(char));
            dt.Columns.Add("quaType", typeof(string));
            dt.Columns.Add("sampleSize", typeof(int));
            dt.Columns.Add("chartCode", typeof(string));
            dt.Columns.Add("specLower", typeof(decimal));
            dt.Columns.Add("specMid", typeof(decimal));
            dt.Columns.Add("specUpper", typeof(decimal));
            dt.Columns.Add("lopl", typeof(decimal));
            dt.Columns.Add("opl", typeof(decimal));
            dt.Columns.Add("uopl", typeof(decimal));
            dt.Columns.Add("rlopl", typeof(decimal));
            dt.Columns.Add("ropl", typeof(decimal));
            dt.Columns.Add("ruopl", typeof(decimal));
            dt.Columns.Add("cpCpkUse", typeof(string));
            // sql의 char는 c#에서 string으로 인식됨으로 인한 변경 220119 KJH
            //dt.Columns.Add("oplYn", typeof(char));
            dt.Columns.Add("oplYn", typeof(string));
            dt.Columns.Add("insertDt", typeof(DateTime));
            dt.Columns.Add("updateDt", typeof(DateTime));
            dt.Columns.Add("insertUserId", typeof(string));
            dt.Columns.Add("insertUserName", typeof(string));
            dt.Columns.Add("updateUserId", typeof(string));
            dt.Columns.Add("updateUserName", typeof(string));
            dt.Columns.Add("useNChangedDt", typeof(DateTime));
            dt.Columns.Add("useNChangedUserId", typeof(string));
            dt.Columns.Add("useNChangedUserName", typeof(string));
            // sql의 char는 c#에서 string으로 인식됨으로 인한 변경 220119 KJH
            //dt.Columns.Add("useYn", typeof(char));
            //dt.Columns.Add("crud", typeof(char));
            dt.Columns.Add("useYn", typeof(string));
            dt.Columns.Add("crud", typeof(string));
            dt.Columns.Add("idxKey", typeof(int));

            // null 허용안함 추가 220119 KJH
            dt.Columns["factId"].AllowDBNull = false;
            dt.Columns["procId"].AllowDBNull = false;
            dt.Columns["lineId"].AllowDBNull = false;
            dt.Columns["godsId"].AllowDBNull = false;
            dt.Columns["inspId"].AllowDBNull = false;
            dt.Columns["unit"].AllowDBNull = false;
            dt.Columns["dataType"].AllowDBNull = false;
            dt.Columns["quaType"].AllowDBNull = false;
            dt.Columns["sampleSize"].AllowDBNull = false;
            dt.Columns["chartCode"].AllowDBNull = false;
            dt.Columns["idxKey"].AllowDBNull = false;

            // dt의 column Name과 일치하지않으면 무시하고 일치하는것만 병합
            dt.Merge(dtData, true, MissingSchemaAction.Ignore);

            return azureMq.SendTopicMqBasicInfo(dt, "tbl_spc_goodsinspect");
        }

        /// <summary>
        /// OCAP 전송 <para/>
        /// groupCode(string), groupNameK(string), groupNameE(string), groupNameJ(string), groupNameC(string), code(string), codeNameK(string), codeNameE(string), codeNameJ(string), codeNameC(string), <para/> insertDt(datetime), updateDt(datetime), insertUserId(string), insertUserName(string), updateUserId(string), updateUserName(string), useNChangedDt(datetime), useNChangedUserId(string), useNChangedUserName(string), <para/> useYn(char), idxKey(int), crud(char)
        /// </summary>
        /// <param name="businessNo">업체코드</param>
        /// <param name="dtData">DataTable</param>
        /// <returns>성공여부</returns>
        public bool syncOCAPInfo(string businessNo, DataTable dtData)
        {
            if (string.IsNullOrEmpty(businessNo)) throw new Exception("businessNo has no value.");

            clsAzureMQ azureMq = new clsAzureMQ
            (
              _businessNo: businessNo
            , _TokenserverUrl: strTokenServerUrl
            , _NameSpace: strNameSpace
            , _KeyName: strKeyName
            , _Key: strKey
            , _topicName: strTopicName
            , _standardKey: strStandardKey
            , _measureKey: strMeasureKey
            , _StatKey: strStatKey
            , _BadKey: strBadKey
            );

            if (dtData == null || dtData.Rows.Count == 0)
            {
                // Scheduler발생시 데이터가없을때 지속적으로 로그메세지 발생하여 exception처리안함
                //throw new Exception("There is no data in Datatable.");
                return false;
            }

            DataTable dt = new DataTable();

            dt.Columns.Add("groupCode", typeof(string));
            dt.Columns.Add("groupNameK", typeof(string));
            dt.Columns.Add("groupNameE", typeof(string));
            dt.Columns.Add("groupNameJ", typeof(string));
            dt.Columns.Add("groupNameC", typeof(string));
            dt.Columns.Add("code", typeof(string));
            dt.Columns.Add("codeNameK", typeof(string));
            dt.Columns.Add("codeNameE", typeof(string));
            dt.Columns.Add("codeNameJ", typeof(string));
            dt.Columns.Add("codeNameC", typeof(string));
            dt.Columns.Add("insertDt", typeof(DateTime));
            dt.Columns.Add("updateDt", typeof(DateTime));
            dt.Columns.Add("insertUserId", typeof(string));
            dt.Columns.Add("insertUserName", typeof(string));
            dt.Columns.Add("updateUserId", typeof(string));
            dt.Columns.Add("updateUserName", typeof(string));
            dt.Columns.Add("useNChangedDt", typeof(DateTime));
            dt.Columns.Add("useNChangedUserId", typeof(string));
            dt.Columns.Add("useNChangedUserName", typeof(string));
            // sql의 char는 c#에서 string으로 인식됨으로 인한 변경 220119 KJH
            //dt.Columns.Add("useYn", typeof(char));
            dt.Columns.Add("useYn", typeof(string));
            dt.Columns.Add("idxKey", typeof(int));
            //dt.Columns.Add("crud", typeof(char));
            dt.Columns.Add("crud", typeof(string));

            // null 허용안함 추가 220119 KJH
            dt.Columns["groupCode"].AllowDBNull = false;
            dt.Columns["code"].AllowDBNull = false;
            dt.Columns["idxKey"].AllowDBNull = false;

            // dt의 column Name과 일치하지않으면 무시하고 일치하는것만 병합
            dt.Merge(dtData, true, MissingSchemaAction.Ignore);

            return azureMq.SendTopicMqBasicInfo(dt, "tbl_com_ocapinfo");
        }

        /// <summary>
        /// 불량조치유형정보 전송 <para/>
        /// groupCode(string), groupNameK(string), groupNameE(string), groupNameJ(string), groupNameC(string), code(string), codeNameK(string), codeNameE(string), codeNameJ(string), codeNameC(string), <para/> insertDt(datetime), updateDt(datetime), insertUserId(string), insertUserName(string), updateUserId(string), updateUserName(string), useNChangedDt(datetime), useNChangedUserId(string), useNChangedUserName(string), <para/> useYn(char), idxKey(int), crud(char)
        /// </summary>
        /// <param name="businessNo">업체코드</param>
        /// <param name="dtData">DataTable</param>
        /// <returns>성공여부</returns>
        public bool syncSpecOutActionInfo(string businessNo, DataTable dtData)
        {
            if (string.IsNullOrEmpty(businessNo)) throw new Exception("businessNo has no value.");

            clsAzureMQ azureMq = new clsAzureMQ
            (
              _businessNo: businessNo
            , _TokenserverUrl: strTokenServerUrl
            , _NameSpace: strNameSpace
            , _KeyName: strKeyName
            , _Key: strKey
            , _topicName: strTopicName
            , _standardKey: strStandardKey
            , _measureKey: strMeasureKey
            , _StatKey: strStatKey
            , _BadKey: strBadKey
            );

            if (dtData == null || dtData.Rows.Count == 0)
            {
                // Scheduler발생시 데이터가없을때 지속적으로 로그메세지 발생하여 exception처리안함
                //throw new Exception("There is no data in Datatable.");
                return false;
            }

            DataTable dt = new DataTable();

            dt.Columns.Add("groupCode", typeof(string));
            dt.Columns.Add("groupNameK", typeof(string));
            dt.Columns.Add("groupNameE", typeof(string));
            dt.Columns.Add("groupNameJ", typeof(string));
            dt.Columns.Add("groupNameC", typeof(string));
            dt.Columns.Add("code", typeof(string));
            dt.Columns.Add("codeNameK", typeof(string));
            dt.Columns.Add("codeNameE", typeof(string));
            dt.Columns.Add("codeNameJ", typeof(string));
            dt.Columns.Add("codeNameC", typeof(string));
            dt.Columns.Add("insertDt", typeof(DateTime));
            dt.Columns.Add("updateDt", typeof(DateTime));
            dt.Columns.Add("insertUserId", typeof(string));
            dt.Columns.Add("insertUserName", typeof(string));
            dt.Columns.Add("updateUserId", typeof(string));
            dt.Columns.Add("updateUserName", typeof(string));
            dt.Columns.Add("useNChangedDt", typeof(DateTime));
            dt.Columns.Add("useNChangedUserId", typeof(string));
            dt.Columns.Add("useNChangedUserName", typeof(string));
            // sql의 char는 c#에서 string으로 인식됨으로 인한 변경 220119 KJH
            //dt.Columns.Add("useYn", typeof(char));
            dt.Columns.Add("useYn", typeof(string));
            dt.Columns.Add("idxKey", typeof(int));
            //dt.Columns.Add("crud", typeof(char));
            dt.Columns.Add("crud", typeof(string));

            // null 허용안함 추가 220119 KJH
            dt.Columns["groupCode"].AllowDBNull = false;
            dt.Columns["code"].AllowDBNull = false;
            dt.Columns["idxKey"].AllowDBNull = false;

            // dt의 column Name과 일치하지않으면 무시하고 일치하는것만 병합
            dt.Merge(dtData, true, MissingSchemaAction.Ignore);

            return azureMq.SendTopicMqBasicInfo(dt, "tbl_com_badaction");
        }

        /// <summary>
        /// 최종현품처리정보 전송 <para/>
        /// groupCode(string), groupNameK(string), groupNameE(string), groupNameJ(string), groupNameC(string), code(string), codeNameK(string), codeNameE(string), codeNameJ(string), codeNameC(string), <para/> insertDt(datetime), updateDt(datetime), insertUserId(string), insertUserName(string), updateUserId(string), updateUserName(string), useNChangedDt(datetime), useNChangedUserId(string), useNChangedUserName(string), <para/> useYn(char), idxKey(int), crud(char)
        /// </summary>
        /// <param name="businessNo">업체코드</param>
        /// <param name="dtData">DataTable</param>
        /// <returns>성공여부</returns>
        public bool syncSpecOutFinalPointInfo(string businessNo, DataTable dtData)
        {
            if (string.IsNullOrEmpty(businessNo)) throw new Exception("businessNo has no value.");

            clsAzureMQ azureMq = new clsAzureMQ
            (
              _businessNo: businessNo
            , _TokenserverUrl: strTokenServerUrl
            , _NameSpace: strNameSpace
            , _KeyName: strKeyName
            , _Key: strKey
            , _topicName: strTopicName
            , _standardKey: strStandardKey
            , _measureKey: strMeasureKey
            , _StatKey: strStatKey
            , _BadKey: strBadKey
            );

            if (dtData == null || dtData.Rows.Count == 0)
            {
                // Scheduler발생시 데이터가없을때 지속적으로 로그메세지 발생하여 exception처리안함
                //throw new Exception("There is no data in Datatable.");
                return false;
            }

            DataTable dt = new DataTable();

            dt.Columns.Add("groupCode", typeof(string));
            dt.Columns.Add("groupNameK", typeof(string));
            dt.Columns.Add("groupNameE", typeof(string));
            dt.Columns.Add("groupNameJ", typeof(string));
            dt.Columns.Add("groupNameC", typeof(string));
            dt.Columns.Add("code", typeof(string));
            dt.Columns.Add("codeNameK", typeof(string));
            dt.Columns.Add("codeNameE", typeof(string));
            dt.Columns.Add("codeNameJ", typeof(string));
            dt.Columns.Add("codeNameC", typeof(string));
            dt.Columns.Add("insertDt", typeof(DateTime));
            dt.Columns.Add("updateDt", typeof(DateTime));
            dt.Columns.Add("insertUserId", typeof(string));
            dt.Columns.Add("insertUserName", typeof(string));
            dt.Columns.Add("updateUserId", typeof(string));
            dt.Columns.Add("updateUserName", typeof(string));
            dt.Columns.Add("useNChangedDt", typeof(DateTime));
            dt.Columns.Add("useNChangedUserId", typeof(string));
            dt.Columns.Add("useNChangedUserName", typeof(string));
            // sql의 char는 c#에서 string으로 인식됨으로 인한 변경 220119 KJH
            //dt.Columns.Add("useYn", typeof(char));
            dt.Columns.Add("useYn", typeof(string));
            dt.Columns.Add("idxKey", typeof(int));
            //dt.Columns.Add("crud", typeof(char));
            dt.Columns.Add("crud", typeof(string));

            // null 허용안함 추가 220119 KJH
            dt.Columns["groupCode"].AllowDBNull = false;
            dt.Columns["code"].AllowDBNull = false;
            dt.Columns["idxKey"].AllowDBNull = false;

            // dt의 column Name과 일치하지않으면 무시하고 일치하는것만 병합
            dt.Merge(dtData, true, MissingSchemaAction.Ignore);

            return azureMq.SendTopicMqBasicInfo(dt, "tbl_com_badfinalpoint");
        }

        /// <summary>
        /// 불량명정보 전송<para/>
        /// groupCode(string), groupNameK(string), groupNameE(string), groupNameJ(string), groupNameC(string), code(string), codeNameK(string), codeNameE(string), codeNameJ(string), codeNameC(string), <para/> insertDt(datetime), updateDt(datetime), insertUserId(string), insertUserName(string), updateUserId(string), updateUserName(string), useNChangedDt(datetime), useNChangedUserId(string), useNChangedUserName(string), <para/> useYn(char), idxKey(int), crud(char)
        /// </summary>
        /// <param name="businessNo">업체코드</param>
        /// <param name="dtData">DataTable</param>
        /// <returns>성공여부</returns>
        public bool syncSpecOutNameInfo(string businessNo, DataTable dtData)
        {
            if (string.IsNullOrEmpty(businessNo)) throw new Exception("businessNo has no value.");

            clsAzureMQ azureMq = new clsAzureMQ
            (
              _businessNo: businessNo
            , _TokenserverUrl: strTokenServerUrl
            , _NameSpace: strNameSpace
            , _KeyName: strKeyName
            , _Key: strKey
            , _topicName: strTopicName
            , _standardKey: strStandardKey
            , _measureKey: strMeasureKey
            , _StatKey: strStatKey
            , _BadKey: strBadKey
            );

            if (dtData == null || dtData.Rows.Count == 0)
            {
                // Scheduler발생시 데이터가없을때 지속적으로 로그메세지 발생하여 exception처리안함
                //throw new Exception("There is no data in Datatable.");
                return false;
            }

            DataTable dt = new DataTable();

            dt.Columns.Add("groupCode", typeof(string));
            dt.Columns.Add("groupNameK", typeof(string));
            dt.Columns.Add("groupNameE", typeof(string));
            dt.Columns.Add("groupNameJ", typeof(string));
            dt.Columns.Add("groupNameC", typeof(string));
            dt.Columns.Add("code", typeof(string));
            dt.Columns.Add("codeNameK", typeof(string));
            dt.Columns.Add("codeNameE", typeof(string));
            dt.Columns.Add("codeNameJ", typeof(string));
            dt.Columns.Add("codeNameC", typeof(string));
            dt.Columns.Add("insertDt", typeof(DateTime));
            dt.Columns.Add("updateDt", typeof(DateTime));
            dt.Columns.Add("insertUserId", typeof(string));
            dt.Columns.Add("insertUserName", typeof(string));
            dt.Columns.Add("updateUserId", typeof(string));
            dt.Columns.Add("updateUserName", typeof(string));
            dt.Columns.Add("useNChangedDt", typeof(DateTime));
            dt.Columns.Add("useNChangedUserId", typeof(string));
            dt.Columns.Add("useNChangedUserName", typeof(string));
            // sql의 char는 c#에서 string으로 인식됨으로 인한 변경 220119 KJH
            //dt.Columns.Add("useYn", typeof(char));
            dt.Columns.Add("useYn", typeof(string));
            dt.Columns.Add("idxKey", typeof(int));
            //dt.Columns.Add("crud", typeof(char));
            dt.Columns.Add("crud", typeof(string));

            // null 허용안함 추가 220119 KJH
            dt.Columns["groupCode"].AllowDBNull = false;
            dt.Columns["code"].AllowDBNull = false;
            dt.Columns["idxKey"].AllowDBNull = false;

            // dt의 column Name과 일치하지않으면 무시하고 일치하는것만 병합
            dt.Merge(dtData, true, MissingSchemaAction.Ignore);

            return azureMq.SendTopicMqBasicInfo(dt, "tbl_com_badname");
        }

        /// <summary>
        /// 이상점 발생 전송<para/>
        /// lotId(string), factId(string), procId(string), lineId(string), godsId(string), inspId(string), inspectDt(datetime), <para/>abnormalType(string), abnormalActionCode(string), abnormalActionName(string), checkPointCode(string), checkPointName(string), treatYn(char), <para/> crud(char), idxKey(int), insertDt(datetime), updateDt(datetime), insertUserId(string), insertUserName(string), updateUserId(string), updateUserName(string), <para/> treatYChangedDt(datetime), treatYChangedUserId(string), treatYChangedUserName(string)
        /// </summary>
        /// <param name="businessNo">업체코드</param>
        /// <param name="dtData">DataTable</param>
        /// <returns></returns>
        public bool syncAbnormalList(string businessNo, DataTable dtData)
        {
            if (string.IsNullOrEmpty(businessNo)) throw new Exception("businessNo has no value.");

            clsAzureMQ azureMq = new clsAzureMQ
            (
              _businessNo: businessNo
            , _TokenserverUrl: strTokenServerUrl
            , _NameSpace: strNameSpace
            , _KeyName: strKeyName
            , _Key: strKey
            , _topicName: strTopicName
            , _standardKey: strStandardKey
            , _measureKey: strMeasureKey
            , _StatKey: strStatKey
            , _BadKey: strBadKey
            );

            if (dtData == null || dtData.Rows.Count == 0)
            {
                // Scheduler발생시 데이터가없을때 지속적으로 로그메세지 발생하여 exception처리안함
                //throw new Exception("There is no data in Datatable.");
                return false;
            }

            DataTable dt = new DataTable();

            dt.Columns.Add("lotId", typeof(string));
            dt.Columns.Add("factId", typeof(string));
            dt.Columns.Add("procId", typeof(string));
            dt.Columns.Add("lineId", typeof(string));
            dt.Columns.Add("godsId", typeof(string));
            dt.Columns.Add("inspId", typeof(string));
            dt.Columns.Add("inspectDt", typeof(DateTime));
            dt.Columns.Add("abnormalType", typeof(string));
            dt.Columns.Add("abnormalActionCode", typeof(string));
            dt.Columns.Add("abnormalActionName", typeof(string));
            dt.Columns.Add("checkPointCode", typeof(string));
            dt.Columns.Add("checkPointName", typeof(string));
            // sql의 char는 c#에서 string으로 인식됨으로 인한 변경 220119 KJH
            //dt.Columns.Add("treatYn", typeof(char));
            //dt.Columns.Add("crud", typeof(char));
            dt.Columns.Add("treatYn", typeof(string));
            dt.Columns.Add("crud", typeof(string));
            dt.Columns.Add("idxKey", typeof(int));
            dt.Columns.Add("insertDt", typeof(DateTime));
            dt.Columns.Add("updateDt", typeof(DateTime));
            dt.Columns.Add("insertUserId", typeof(string));
            dt.Columns.Add("insertUserName", typeof(string));
            dt.Columns.Add("updateUserId", typeof(string));
            dt.Columns.Add("updateUserName", typeof(string));
            dt.Columns.Add("treatYChangedDt", typeof(DateTime));
            dt.Columns.Add("treatYChangedUserId", typeof(string));
            dt.Columns.Add("treatYChangedUserName", typeof(string));

            // null 허용안함 추가 220119 KJH
            dt.Columns["lotId"].AllowDBNull = false;
            dt.Columns["factId"].AllowDBNull = false;
            dt.Columns["procId"].AllowDBNull = false;
            dt.Columns["lineId"].AllowDBNull = false;
            dt.Columns["godsId"].AllowDBNull = false;
            dt.Columns["inspId"].AllowDBNull = false;
            dt.Columns["abnormalType"].AllowDBNull = false;
            dt.Columns["abnormalActionCode"].AllowDBNull = false;
            dt.Columns["checkPointCode"].AllowDBNull = false;
            dt.Columns["treatYn"].AllowDBNull = false;
            dt.Columns["idxKey"].AllowDBNull = false;

            // dt의 column Name과 일치하지않으면 무시하고 일치하는것만 병합
            dt.Merge(dtData, true, MissingSchemaAction.Ignore);

            return azureMq.SendTopicMqAbnormalSpecOut(dt, "tbl_spc_abnormal_result");
        }

        /// <summary>
        /// 스펙아웃정보 전송<para/>
        /// lotId(string), factId(string), procId(string), lineId(string), godsId(string), inspId(string), inspectDt(datetime), specLower(decimal), specMid(decimal), specUpper(decimal), <para/> specoutType(string), checkPointCode(string),<para/> treatYn(char), insertDt(datetime), updateDt(datetime), insertUserId(string), insertUserName(string), updateUserId(string), updateUserName(string), crud(char), idxKey(int), treatYChangedDt(datetime), treatYChangedUserId(string), treatYChangedUserName(string)
        /// </summary>
        /// <param name="businessNo">업체코드</param>
        /// <param name="dtData">DataTable</param>
        /// <returns></returns>
        public bool syncSpecOutList(string businessNo, DataTable dtData)
        {
            if (string.IsNullOrEmpty(businessNo)) throw new Exception("businessNo has no value.");

            clsAzureMQ azureMq = new clsAzureMQ
            (
              _businessNo: businessNo
            , _TokenserverUrl: strTokenServerUrl
            , _NameSpace: strNameSpace
            , _KeyName: strKeyName
            , _Key: strKey
            , _topicName: strTopicName
            , _standardKey: strStandardKey
            , _measureKey: strMeasureKey
            , _StatKey: strStatKey
            , _BadKey: strBadKey
            );

            if (dtData == null || dtData.Rows.Count == 0)
            {
                // Scheduler발생시 데이터가없을때 지속적으로 로그메세지 발생하여 exception처리안함
                //throw new Exception("There is no data in Datatable.");
                return false;
            }

            DataTable dt = new DataTable();

            dt.Columns.Add("lotId", typeof(string));
            dt.Columns.Add("factId", typeof(string));
            dt.Columns.Add("procId", typeof(string));
            dt.Columns.Add("lineId", typeof(string));
            dt.Columns.Add("godsId", typeof(string));
            dt.Columns.Add("inspId", typeof(string));
            dt.Columns.Add("inspectDt", typeof(DateTime));
            dt.Columns.Add("specLower", typeof(decimal));
            dt.Columns.Add("specMid", typeof(decimal));
            dt.Columns.Add("specUpper", typeof(decimal));
            dt.Columns.Add("specoutType", typeof(string));
            dt.Columns.Add("checkPointCode", typeof(string));
            // sql의 char는 c#에서 string으로 인식됨으로 인한 변경 220119 KJH
            //dt.Columns.Add("treatYn", typeof(char));
            dt.Columns.Add("treatYn", typeof(string));
            dt.Columns.Add("insertDt", typeof(DateTime));
            dt.Columns.Add("updateDt", typeof(DateTime));
            dt.Columns.Add("insertUserId", typeof(string));
            dt.Columns.Add("insertUserName", typeof(string));
            dt.Columns.Add("updateUserId", typeof(string));
            dt.Columns.Add("updateUserName", typeof(string));
            // sql의 char는 c#에서 string으로 인식됨으로 인한 변경 220119 KJH
            //dt.Columns.Add("crud", typeof(char));
            dt.Columns.Add("crud", typeof(string));
            dt.Columns.Add("idxKey", typeof(int));
            dt.Columns.Add("treatYChangedDt", typeof(DateTime));
            dt.Columns.Add("treatYChangedUserId", typeof(string));
            dt.Columns.Add("treatYChangedUserName", typeof(string));

            // null 허용안함 추가 220119 KJH
            dt.Columns["lotId"].AllowDBNull = false;
            dt.Columns["factId"].AllowDBNull = false;
            dt.Columns["procId"].AllowDBNull = false;
            dt.Columns["lineId"].AllowDBNull = false;
            dt.Columns["godsId"].AllowDBNull = false;
            dt.Columns["inspId"].AllowDBNull = false;
            dt.Columns["specoutType"].AllowDBNull = false;
            dt.Columns["checkPointCode"].AllowDBNull = false;
            dt.Columns["treatYn"].AllowDBNull = false;
            dt.Columns["idxKey"].AllowDBNull = false;

            // dt의 column Name과 일치하지않으면 무시하고 일치하는것만 병합
            dt.Merge(dtData, true, MissingSchemaAction.Ignore);

            return azureMq.SendTopicMqAbnormalSpecOut(dt, "tbl_spc_bad_result");
        }

        /// <summary>
        /// 통계치 데이터 전송<para/>
        /// processindexYear(string(4)), processindexMonth(string(2)), processindexWeek(string(2)), processindexDay(string(2)), <para/>factId(string), procId(string), lineId(string), godsId(string), inspId(string), specLower(decimal), specMid(decimal), specUpper(decimal), <para/>lcl(decimal), cl(decimal), ucl(decimal), rcl(decimal), rucl(decimal), oplYn(char), processindexCp(decimal), processindexCpk(decimal), processindexPp(decimal), processindexPpk(decimal),<para/> samplesize(int), average(decimal), stddev(decimal), variance(decimal), crud(char), idxKey(int), rawNumber(string)
        /// </summary>
        /// <param name="businessNo">업체코드</param>
        /// <param name="dtData">DataTable</param>
        /// <returns></returns>
        public bool syncStatData(string businessNo, DataTable dtData)
        {
            if (string.IsNullOrEmpty(businessNo)) throw new Exception("businessNo has no value.");

            clsAzureMQ azureMq = new clsAzureMQ
            (
              _businessNo: businessNo
            , _TokenserverUrl: strTokenServerUrl
            , _NameSpace: strNameSpace
            , _KeyName: strKeyName
            , _Key: strKey
            , _topicName: strTopicName
            , _standardKey: strStandardKey
            , _measureKey: strMeasureKey
            , _StatKey: strStatKey
            , _BadKey: strBadKey
            );

            if (dtData == null || dtData.Rows.Count == 0)
            {
                // Scheduler발생시 데이터가없을때 지속적으로 로그메세지 발생하여 exception처리안함
                //throw new Exception("There is no data in Datatable.");
                return false;
            }

            DataTable dt = new DataTable();

            // 명칭수정(mq 데이터 전송 byte줄이기) 220114 kjh
            //DataColumn pcyear = new DataColumn("processindexYear", typeof(string));
            //pcyear.MaxLength = 4;
            //DataColumn pcmon = new DataColumn("processindexMonth", typeof(string));
            //pcmon.MaxLength = 2;
            //DataColumn pcweek = new DataColumn("processindexWeek", typeof(string));
            //pcweek.MaxLength = 2;
            //DataColumn pcday = new DataColumn("processindexDay", typeof(string));
            //pcday.MaxLength = 2;

            // 글자수제한
            DataColumn pcyear = new DataColumn("piY", typeof(string));
            pcyear.MaxLength = 4;
            DataColumn pcmon = new DataColumn("piM", typeof(string));
            pcmon.MaxLength = 2;
            DataColumn pcweek = new DataColumn("piD", typeof(string));
            pcweek.MaxLength = 2;
            DataColumn pcday = new DataColumn("piW", typeof(string));
            pcday.MaxLength = 2;

            dt.Columns.Add(pcyear);
            dt.Columns.Add(pcmon);
            dt.Columns.Add(pcweek);
            dt.Columns.Add(pcday);
            dt.Columns.Add("factId", typeof(string));
            dt.Columns.Add("procId", typeof(string));
            dt.Columns.Add("lineId", typeof(string));
            dt.Columns.Add("godsId", typeof(string));
            dt.Columns.Add("inspId", typeof(string));
            dt.Columns.Add("specLower", typeof(decimal));
            dt.Columns.Add("specMid", typeof(decimal));
            dt.Columns.Add("specUpper", typeof(decimal));
            dt.Columns.Add("lcl", typeof(decimal));
            dt.Columns.Add("cl", typeof(decimal));
            dt.Columns.Add("ucl", typeof(decimal));
            dt.Columns.Add("rcl", typeof(decimal));
            dt.Columns.Add("rucl", typeof(decimal));
            // sql의 char는 c#에서 string으로 인식됨으로 인한 변경 220119 KJH
            //dt.Columns.Add("oplYn", typeof(char));
            dt.Columns.Add("oplYn", typeof(string));
            // 명칭수정(mq 데이터 전송 byte줄이기) 220114 kjh
            //dt.Columns.Add("processindexCp", typeof(decimal));
            //dt.Columns.Add("processindexCpk", typeof(decimal));
            //dt.Columns.Add("processindexPp", typeof(decimal));
            //dt.Columns.Add("processindexPpk", typeof(decimal));

            dt.Columns.Add("piCp", typeof(decimal));
            dt.Columns.Add("piCpk", typeof(decimal));
            dt.Columns.Add("piPp", typeof(decimal));
            dt.Columns.Add("piPpk", typeof(decimal));
            dt.Columns.Add("samplesize", typeof(int));
            dt.Columns.Add("average", typeof(decimal));
            dt.Columns.Add("stddev", typeof(decimal));
            dt.Columns.Add("variance", typeof(decimal));
            // sql의 char는 c#에서 string으로 인식됨으로 인한 변경 220119 KJH
            //dt.Columns.Add("crud", typeof(char));
            dt.Columns.Add("crud", typeof(string));
            dt.Columns.Add("idxKey", typeof(int));

            DataColumn rawnum = new DataColumn("rawNumber", typeof(string));
            rawnum.MaxLength = 11;
            dt.Columns.Add(rawnum);

            // null 허용안함 추가 220119 KJH
            dt.Columns["piY"].AllowDBNull = false;
            dt.Columns["piM"].AllowDBNull = false;
            dt.Columns["piD"].AllowDBNull = false;
            dt.Columns["piW"].AllowDBNull = false;
            dt.Columns["factId"].AllowDBNull = false;
            dt.Columns["procId"].AllowDBNull = false;
            dt.Columns["lineId"].AllowDBNull = false;
            dt.Columns["godsId"].AllowDBNull = false;
            dt.Columns["inspId"].AllowDBNull = false;
            dt.Columns["idxKey"].AllowDBNull = false;

            // dt의 column Name과 일치하지않으면 무시하고 일치하는것만 병합
            dt.Merge(dtData, true, MissingSchemaAction.Ignore);

            return azureMq.SendTopicMqStat(dt, "tbl_spc_date_statistics");
        }
        
    }
}
