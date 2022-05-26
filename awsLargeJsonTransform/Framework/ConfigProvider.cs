using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ca.awsLargeJsonTransform.Framework
{
    public class ConfigProvider
    {
        private static ConfigProvider _instance;
        private readonly IConfigurationRoot _configuration;

        private ConfigProvider() {
            var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", true, true);
            _configuration = builder.Build();
        }
        public static ConfigProvider Instance
        {
            get
            {
                if (_instance == null) { _instance = new ConfigProvider(); }
                return _instance;
            }
        }

        private string GetSetting(string key, string defaultValue = "")
        {
            if (string.IsNullOrEmpty(key)) { return defaultValue; }
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            string returnValue = _configuration[key];
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            if (string.IsNullOrEmpty(returnValue)) { return defaultValue; }
            return returnValue.ToString();

        }

        public string GetAppSettings(string key, string defaultValue = "")
        {
            return GetSetting($"appSettings:{key}", defaultValue);
        }
        public string GetLogSettings(string key, string defaultValue = "")
        {
            return GetSetting($"logSettings:{key}", defaultValue);
        }

        public string GetSourceJsonFilePath()
        {
            return GetAppSettings("inputFilePath");// @"C:\01\prh\gitFinal\oi.prh.JsonTransform\files\search_terms.json";
        }
        public string GetDestinationCsvFilePath(int? nextFileNumber)
        {
            string outputFolderPath = GetAppSettings("outputFolderPath"); //@"C:\01\prh\gitFinal\oi.prh.JsonTransform\files\output";
            string outputFilePrefix = GetAppSettings("outputFilePrefix");            
            Directory.CreateDirectory(outputFolderPath);
            string filePathBase = $@"{outputFolderPath}\search_terms_{DateTime.Now.ToString("yyyyMMddhhmmss")}";
            if (nextFileNumber.HasValue)
            {
                return $@"{filePathBase}_{nextFileNumber.Value.ToString("00000")}.csv";
            }
            else
            {
                return $@"{filePathBase}.csv";
            }
        }
        
        public string GetLogPath()
        {
            return GetLogSettings("filePath", $@"{AppDomain.CurrentDomain.BaseDirectory}\logs\");            
        }

        public bool GetDebugTraceDetailMode()
        {
            return bool.Parse(GetLogSettings("debugTraceDetailMode", "false"));
        }
        public bool GetErrorDetailMode()
        {
            return bool.Parse(GetLogSettings("errorDetailMode", "true"));
        }
        public bool GetDoLogToFile()
        {
            return bool.Parse(GetLogSettings("doLogToFile", "true"));
        }
        public string GetConsoleLogType()
        {
#pragma warning disable CS8603 // Possible null reference return.
            return GetLogSettings("consoleLogType", $@"{AppDomain.CurrentDomain.BaseDirectory}\logs\")?.ToString().ToLower().Trim();
#pragma warning restore CS8603 // Possible null reference return.
        }
        public int GetMaxLogRowCount(int defaluteRowCount = 10000)
        {
            string strMaxRowCount = GetAppSettings("maxRowCount", defaluteRowCount.ToString());
            int maxRowCount = 0;
            if (!int.TryParse(strMaxRowCount, out maxRowCount)) { maxRowCount = defaluteRowCount; }
            return maxRowCount;
        }

        public string GetDataSearchTerCSVHeaderLine()
        {
            return GetAppSettings("dataSearchTerCSVHeaderLine");
        }
        public int GetMaxRowCountPerCsvFile(int defaluteRowCount = 100)
        {
            string maxRowCountPerCsvFile = GetAppSettings("maxRowCountPerCsvFile", defaluteRowCount.ToString());
            int rowCount = 0;
            if(!int.TryParse(maxRowCountPerCsvFile, out rowCount)) { rowCount = defaluteRowCount; }
            return rowCount;
        }

    }
}
