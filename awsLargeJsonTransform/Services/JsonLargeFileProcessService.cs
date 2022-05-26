using ca.awsLargeJsonTransform.Entity;
using ca.awsLargeJsonTransform.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ca.awsLargeJsonTransform.Services
{
    public struct FileParseKeyTerms
    {
        public const string dataByDepartmentAndSearchTerm = "\"dataByDepartmentAndSearchTerm\"";
    }

    public interface IJsonLargeFileProcessService : IDisposable
    {
        void Execute();
    }
    /// <summary>
    /// Class to process large search_terms JSON file to CSV.
    /// class name is JsonLargeFileProcessService 
    /// log tag name is JsonLFPService
    /// </summary>
    public class JsonLargeFileProcessService : IJsonLargeFileProcessService
    {
        private string _logTag = "JsonLFPService";
        private long _totalRecordCount = 0;
        private int errorFileNumber = 0;
        public JsonLargeFileProcessService()
        {

        }

        public void Dispose()
        {
            GC.Collect();
        }

        public void Execute()
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            LogProvider.Information($"{_logTag}.Execute", $"Started");
            try
            {
                ParseLargeJsonFile(ConfigProvider.Instance.GetSourceJsonFilePath());
            }
            catch (Exception ex)
            {
                LogProvider.Error($"{_logTag}.Execute FAILED", ex);
            }
            watch.Stop();
            LogProvider.Information($"{_logTag}.Execute", $"Completed with total record count {_totalRecordCount}");
            LogProvider.Information($"{_logTag}.Execute", $"Total Execution Time: {watch.Elapsed.ToString(@"hh\:mm\:ss")}");
        }
        private string ValidateDecompressSource(string sourceFilePath)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath)) { throw new Exception("Provide valid source file path!"); }
            if (!sourceFilePath.EndsWith(".json"))
            {
                return CommonHelper.Decompress(sourceFilePath);
            }
            return sourceFilePath;
        }
        private void ParseLargeJsonFile(string sourceFilePath)
        {
            string logTag = $"{_logTag}.ParseLargeJsonFile";
            try
            {
                sourceFilePath = ValidateDecompressSource(sourceFilePath);
                int bufferSize = 1024;
                var buffer = new Char[bufferSize];
                String jsonDocument = "";

                var length = 0L;
                var totalRead = 0L;
                var count = bufferSize;
                int bufferRowCount = 0;
                int maxJsonRowCountPerFile = ConfigProvider.Instance.GetMaxRowCountPerCsvFile();
                int nextFileNumber = 1;

                SearchTerms searchTerms = new SearchTerms();

                using (var sr = new StreamReader(sourceFilePath))
                {
                    length = sr.BaseStream.Length;
                    while (count > 0)
                    {
                        count = sr.Read(buffer, 0, bufferSize);
                        List<char> newArray = new List<char>();
                        totalRead += count;

                        String line = new String(buffer);

                        if (line.Contains(FileParseKeyTerms.dataByDepartmentAndSearchTerm, StringComparison.OrdinalIgnoreCase))
                        {
                            LogProvider.Information($"{_logTag}.ParseLargeJsonFile", "header parsing started.");
                            Tuple<SearchTerms, string> headerOutput = ParseHeader(line);
                            searchTerms = headerOutput.Item1;
                            jsonDocument += headerOutput.Item2;
                            LogProvider.Information(logTag, "header parsing completed successfully.");
                        }
                        else
                        {
                            try
                            {
                                bufferRowCount++;
                                LogProvider.TraceDetailMode(logTag, $"body parsing at bufferRowCount = {bufferRowCount}");
                                jsonDocument = ParseBody(jsonDocument + line, searchTerms);
                                nextFileNumber = ExportToCsv(searchTerms, maxJsonRowCountPerFile, nextFileNumber);

                                if(count > 0 && (jsonDocument.Contains("]") || jsonDocument.Contains("[")))
                                {
                                    LogProvider.Error(logTag, new Exception("jsonDocument has [ and ]"));
                                    string errorFilePath = GetErroredFilePath(errorFileNumber);
                                    WriteToFile(errorFilePath, jsonDocument);
                                }

                            }
                            catch (Exception ex)
                            {
                                LogProvider.Error($"{logTag}.Document.Error source file {sourceFilePath}", ex);
                            }
                        }
                    }
                }
                ParsingEndOfFileErroredContent(jsonDocument, searchTerms, nextFileNumber++);
            }
            catch (Exception ex)
            {
                LogProvider.Error($"{logTag}.Error source file {sourceFilePath}", ex);
            }
        }

        private void ParsingEndOfFileErroredContent(string jsonDocument, SearchTerms searchTerms, int nextFileNumber)
        {
            string logTag = $"{_logTag}.ParsingEndOfFileErroredContent";
            try
            {
                if (string.IsNullOrWhiteSpace(jsonDocument))
                {
                    LogProvider.Information(logTag, $"The file had no errored content all data parsed.");
                    return;
                }
                if (searchTerms.dataByDepartmentAndSearchTerm == null) { searchTerms.dataByDepartmentAndSearchTerm = new List<Databydepartmentandsearchterm>(); }
                string leftOverContent = "";
                int startIndex = jsonDocument.IndexOf("{");
                string finalJsonDocument = jsonDocument.Substring(startIndex);
                leftOverContent = jsonDocument.Substring(0, startIndex);
                string strEnd = "}";
                if (finalJsonDocument.Contains("]"))
                {
                    strEnd = "]";
                }
                int endIndex = finalJsonDocument.LastIndexOf(strEnd) + 1;
                finalJsonDocument = $"[{finalJsonDocument.Substring(0, endIndex)}".Trim();
                if (!finalJsonDocument.EndsWith("]"))
                {
                    finalJsonDocument += "]";
                }

                leftOverContent += jsonDocument.Substring(endIndex);
                searchTerms.dataByDepartmentAndSearchTerm.AddRange(finalJsonDocument.FromJson<List<Databydepartmentandsearchterm>>());
                ExportToCsv(searchTerms, nextFileNumber);
                string outputFilePath = GetErroredFilePath(errorFileNumber);
                WriteToFile(outputFilePath, leftOverContent);
            }
            catch(Exception ex)
            {
                LogProvider.Error(logTag, ex);
                string outputFilePath = GetErroredFilePath(errorFileNumber);
                WriteToFile(outputFilePath, jsonDocument);
            }
        }
        private string GetErroredFilePath(int errorFileNumber)
        {
            return ConfigProvider.Instance.GetDestinationCsvFilePath(errorFileNumber++) + ".errcnt";
        }
        private void WriteToFile(string outputFilePath, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) { return; }
            string logTag = $"{_logTag}.WriteOverlookedContent";
            LogProvider.Information(logTag, $"Writing to incorrect leftover content completed to file {new FileInfo(outputFilePath).Name}.");
            File.WriteAllText(outputFilePath, content);
        }

        private Tuple<SearchTerms, string> ParseHeader(string line)
        {
            int headerEndIndex = line.IndexOf(FileParseKeyTerms.dataByDepartmentAndSearchTerm) - 1;
            int bodyStartIndex = line.IndexOf(FileParseKeyTerms.dataByDepartmentAndSearchTerm) + FileParseKeyTerms.dataByDepartmentAndSearchTerm.Length;
            string header = line.Substring(0, headerEndIndex).Trim();
            string body = line.Substring(bodyStartIndex).Trim();
            if (header.EndsWith(","))
            {
                header = header.Substring(0, header.Length - 1);
            }
            header += "}";

            if (body.StartsWith(":"))
            {
                body = body.Substring(1).Trim();
            }

            SearchTerms searchTerms = Newtonsoft.Json.JsonConvert.DeserializeObject<SearchTerms>(header);
            if (searchTerms == null)
            {
                throw new Exception("Header information is missgin!");
            }
            searchTerms.dataByDepartmentAndSearchTerm = new List<Databydepartmentandsearchterm>();

            return Tuple.Create<SearchTerms, string>(searchTerms, body);
        }

        private string ParseBody(string jsonBody, SearchTerms searchTerms, bool checkForArrayEnd = false)
        {
            string logTag = $"{_logTag}.ParseBody";
            try
            {
                if (string.IsNullOrWhiteSpace(jsonBody) || !jsonBody.Contains("}")) { return jsonBody; }
                if (searchTerms.dataByDepartmentAndSearchTerm == null) { searchTerms.dataByDepartmentAndSearchTerm = new List<Databydepartmentandsearchterm>(); }
                int lastIndexOfRow = jsonBody.LastIndexOf("}") + 1;
                string strEnd = "]";
                if (checkForArrayEnd && jsonBody.Contains("]"))
                {
                    lastIndexOfRow = jsonBody.IndexOf("]") + 1;
                    strEnd = string.Empty;
                }
                string arrayBody = jsonBody.Substring(0, lastIndexOfRow).Trim() + strEnd;
                searchTerms.dataByDepartmentAndSearchTerm.AddRange(arrayBody.FromJson<List<Databydepartmentandsearchterm>>());
                string leftoverBody = jsonBody.Substring(lastIndexOfRow).Trim();
                if (leftoverBody.StartsWith(","))
                {
                    leftoverBody = leftoverBody.Substring(1);
                }
                if (!leftoverBody.StartsWith("["))
                {
                    leftoverBody = "[" + leftoverBody;
                }
                LogProvider.Trace(logTag, $"dataByDepartmentAndSearchTerm added current file row counter {searchTerms.dataByDepartmentAndSearchTerm.Count}");
                return leftoverBody;
            }
            catch (Exception ex)
            {
                //LogProvider.Error($"{logTag} this error can be ignored as it will get fix in next iteration", ex);
                ConsoleWriter.Write(LogLevel.Error, $"{logTag} this error can be ignored as it will get fix in next iteration. Error: {ex.Message}");
                return jsonBody;
            }
        }

        private int ExportToCsv(SearchTerms searchTerms, int maxJsonRowCountPerFile, int nextFileNumber)
        {
            if (searchTerms == null || searchTerms.dataByDepartmentAndSearchTerm?.Count == 0) { return nextFileNumber; }
            if (searchTerms?.dataByDepartmentAndSearchTerm?.Count < maxJsonRowCountPerFile) { return nextFileNumber; }
            return ExportToCsv(searchTerms, nextFileNumber);
        }

        private int ExportToCsv(SearchTerms searchTerms, int nextFileNumber)
        {
            string logTag = $"{_logTag}.ExportToCsv";
            try
            {
                StringBuilder sbCsvContent = new StringBuilder();
                sbCsvContent.AppendLine(ConfigProvider.Instance.GetDataSearchTerCSVHeaderLine());
                searchTerms.dataByDepartmentAndSearchTerm.ForEach(term =>
                {
                    try
                    {
                        if (term != null)
                        {
                            string csvLine = $"{term.departmentName.GetCsvColumnValue()}"
                                            + $",{term.searchTerm.GetCsvColumnValue()}"
                                            + $",{term.searchFrequencyRank.GetCsvColumnValue()}"
                                            + $",{term.clickedAsin.GetCsvColumnValue()}"
                                            + $",{term.clickedItemName.GetCsvColumnValue()}"
                                            + $",{term.clickShareRank.GetCsvColumnValue()}"
                                            + $",{term.clickShare.GetCsvColumnValue()}"
                                            + $",{term.conversionShare.GetCsvColumnValue()}";
                            sbCsvContent.AppendLine(csvLine);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogProvider.Error($"{logTag}.forEachTerm term={term.ToJson()}", ex);
                    }
                });
                string outputFilePath = ConfigProvider.Instance.GetDestinationCsvFilePath(nextFileNumber);
                LogProvider.Information(logTag, $"Writing to csv file '{new FileInfo(outputFilePath).Name}' with {searchTerms.dataByDepartmentAndSearchTerm.Count} records");
                File.WriteAllText(outputFilePath, sbCsvContent.ToString());
                _totalRecordCount += searchTerms.dataByDepartmentAndSearchTerm.Count;
                LogProvider.Information(logTag, $"{_totalRecordCount} successfully transformed");
                nextFileNumber++;
                searchTerms.dataByDepartmentAndSearchTerm.Clear();

            }
            catch (Exception ex)
            {
                LogProvider.Error(logTag, ex);
            }
            return nextFileNumber;
        }
        private string WriteToFile(int nextFileNumber, string content, string overrideExt = "")
        {
            if (string.IsNullOrWhiteSpace(content)) { return string.Empty; }
            string logTag = $"{_logTag}.WriteToCsv";
            string outputFilePath = ConfigProvider.Instance.GetDestinationCsvFilePath(nextFileNumber);
            if (!string.IsNullOrWhiteSpace(overrideExt)) { outputFilePath += overrideExt; }
            File.WriteAllText(outputFilePath, content);
            return outputFilePath;
        }
    }
}
