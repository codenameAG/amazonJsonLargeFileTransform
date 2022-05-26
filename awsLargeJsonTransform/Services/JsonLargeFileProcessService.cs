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
            LogProvider.Trace($"{_logTag}.Execute", $"Started");
            try
            {
                ParseLargeJsonFile(ConfigProvider.Instance.GetSourceJsonFilePath());
            }
            catch (Exception ex)
            {
                LogProvider.Error($"{_logTag}.Execute FAILED", ex);
            }
            watch.Stop();
            LogProvider.Trace($"{_logTag}.Execute", $"Completed");
            LogProvider.Trace($"{_logTag}.Execute", $"Total Execution Time: {watch.Elapsed.ToString(@"hh\:mm\:ss")}");
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
                            LogProvider.Trace($"{_logTag}.ParseLargeJsonFile", "header parsing started.");
                            Tuple<SearchTerms, string> headerOutput = ParseHeader(line);
                            searchTerms = headerOutput.Item1;
                            jsonDocument += headerOutput.Item2;
                            LogProvider.Trace(logTag, "header parsing completed successfully.");
                        }
                        else
                        {
                            try
                            {
                                bufferRowCount++;
                                LogProvider.TraceDetailMode(logTag, $"body parsing at bufferRowCount = {bufferRowCount}");
                                jsonDocument = ParseBody(jsonDocument + line, searchTerms);
                                nextFileNumber = ExportToCsv(searchTerms, maxJsonRowCountPerFile, nextFileNumber);
                            }
                            catch (Exception ex)
                            {
                                LogProvider.Error($"{logTag}.Document.Error source file {sourceFilePath}", ex);
                            }
                        }
                    }
                }
                nextFileNumber = ExportToCsv(searchTerms, nextFileNumber);
                if (!string.IsNullOrWhiteSpace(jsonDocument))
                {
                    string outputFilePath = ConfigProvider.Instance.GetDestinationCsvFilePath(nextFileNumber++) + ".leftover.txt";
                    LogProvider.Information(logTag, $"Writing to csv file '{new FileInfo(outputFilePath).Name}' with total record count {searchTerms.dataByDepartmentAndSearchTerm.Count}");
                    File.WriteAllText(outputFilePath, jsonDocument);
                }
            }
            catch (Exception ex)
            {
                LogProvider.Error($"{logTag}.Error source file {sourceFilePath}", ex);
            }
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

        private string ParseBody(string jsonBody, SearchTerms searchTerms)
        {
            string logTag = $"{_logTag}.ParseBody";
            try
            {
                if (string.IsNullOrWhiteSpace(jsonBody) || !jsonBody.Contains("}")) { return jsonBody; }
                if (searchTerms.dataByDepartmentAndSearchTerm == null) { searchTerms.dataByDepartmentAndSearchTerm = new List<Databydepartmentandsearchterm>(); }
                int lastIndexOfRow = jsonBody.LastIndexOf("}") + 1;
                string arrayBody = jsonBody.Substring(0, lastIndexOfRow).Trim() + "]";
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
                LogProvider.Error($"{logTag} this error can be ignored as it will get fix in next iteration", ex);
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
                LogProvider.Information(logTag, $"Writing to csv file '{new FileInfo(outputFilePath).Name}' with total record count {searchTerms.dataByDepartmentAndSearchTerm.Count}");
                File.WriteAllText(outputFilePath, sbCsvContent.ToString());
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
