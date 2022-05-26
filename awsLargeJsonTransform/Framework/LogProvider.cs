using System.Text;

namespace ca.awsLargeJsonTransform.Framework
{
    /// <summary>
    /// Class represents Log details
    /// </summary>
    public static class LogProvider
    {
        private static string _sessionId;
        private static int _logRowCount;
        private static int _maxLogRowCount;
        private static string _logFilePath;

        /// <summary>
        /// Static constructor
        /// </summary>
        static LogProvider() { _logRowCount = 0; _maxLogRowCount = ConfigProvider.Instance.GetMaxLogRowCount(); }

        ///<summary>
        /// Initialize the session & set session id
        /// </summary>
        public static void InitSession()
        {
            _sessionId = Guid.NewGuid().ToString().Replace("-", string.Empty).Trim();
        }

        ///<summary>
        /// Initialize the session if session id is blank,null or consists only white space character
        /// </summary>
        public static void CheckSession()
        {
            if (string.IsNullOrWhiteSpace(_sessionId))
            {
                InitSession();
            }
        }

        ///<summary>
        /// Get the inner exception from Exception object
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <returns>Returns the inner exception from Exception object</returns>
        public static string GetInnerErrorDetails(Exception ex)
        {
            if (ex == null) { return string.Empty; }
            string exception = $" | Inner Ex: {ex.Message}.";
            if (ex.InnerException == null)
            {
                return exception.Trim();
            }
            else
            {
                exception += GetInnerErrorDetails(ex.InnerException);
            }
            return exception;
        }

        ///<summary>
        /// Based on Debug trace detail mode write error logs to log file 
        /// </summary>
        /// <param name="tag">tag of detail to log</param>
        /// <param name="message">message to log</param>
        /// <param name="writeToConsole">Flag indicates whether to write log on console screen or not</param>

        ///<summary>
        /// Write error logs to log file
        /// </summary>
        /// <param name="prefixOrTag">tag or prefix</param>
        /// <param name="ex">Exception object</param>
        /// <param name="writeToConsole">Flag indicates whether to write log on console screen or not</param>
        public static void Error(string prefixOrTag, Exception ex)
        {
            string error = ConfigProvider.Instance.GetErrorDetailMode() ? ex.ToString() : $"Error: {ex.Message} {GetInnerErrorDetails(ex.InnerException)}".Trim();
            Write(LogLevel.Error, $"{prefixOrTag}.Error: {ex.Message}.", error, null);
        }
        public static void ErrorAndEmail(string prefixOrTag, Exception ex)
        {
            Write(LogLevel.Error, $"{prefixOrTag}.Error: {ex.Message}.", ex.ToString(), null);
        }

        ///<summary>
        /// Write Trace logs to log file
        /// </summary>
        /// <param name="tag">tag of detail to log</param>
        /// <param name="message">message to log</param>
        /// <param name="writeToConsole">Flag indicates whether to write log on console screen or not</param>
        public static void Trace(string tag, string message)
        {
            Write(LogLevel.Trace, tag, message);
        }

        ///<summary>
        /// Based on Debug trace detail mode write trace logs to log file 
        /// </summary>
        /// <param name="tag">tag of detail to log</param>
        /// <param name="message">message to log</param>
        /// <param name="writeToConsole">Flag indicates whether to write log on console screen or not</param>

        public static void TraceDetailMode(string tag, string message)
        {
            if (!ConfigProvider.Instance.GetDebugTraceDetailMode()) { return; }
            Write(LogLevel.Trace, tag, message);
        }

        ///<summary>
        /// Write Information logs to log file
        /// </summary>
        /// <param name="tag">tag of detail to log</param>
        /// <param name="message">message to log</param>
        /// <param name="writeToConsole">Flag indicates whether to write log on console screen or not</param>
        public static void Information(string tag, string message)
        {
            Write(LogLevel.Information, tag, message);
        }

        ///<summary>
        /// Write warning logs to log file
        /// </summary>
        /// <param name="tag">tag of detail to log</param>
        /// <param name="message">message to log</param>
        /// <param name="writeToConsole">Flag indicates whether to write log on console screen or not</param>
        public static void Warning(string tag, string message)
        {
            Write(LogLevel.Warning, tag, message);
        }

        ///<summary>
        /// Write logs to log file
        /// </summary>
        /// <param name="type">Log level</param>
        /// <param name="tag">tag of detail to log</param>
        /// <param name="message">message to log</param>
        /// <param name="writeToConsole">Flag indicates whether to write log on console screen or not</param>
        public static void Write(LogLevel type, string tag, string message)
        {
            Write(type, tag, message, null);
        }

        ///<summary>
        /// Write logs to log file
        /// </summary>
        /// <param name="type">Log level</param>
        /// <param name="tag">tag of detail to log</param>
        /// <param name="ex">Exception</param>
        /// <param name="writeToConsole">Flag indicates whether to write log on console screen or not</param>
        public static void Write(LogLevel type, string tag, Exception ex)
        {
            Write(type, tag, null, ex);
        }
        private static bool DoLog(LogLevel type)
        {
            if (type != LogLevel.Trace && type != LogLevel.Debug)
            {
                return true;
            }
            else if (type == LogLevel.Trace
                && ConfigProvider.Instance.GetConsoleLogType().Equals("trace", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else if (type == LogLevel.Debug
                && (ConfigProvider.Instance.GetConsoleLogType().Equals("trace", StringComparison.OrdinalIgnoreCase)
                || ConfigProvider.Instance.GetConsoleLogType().Equals("debug", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            return false;
        }

        ///<summary>
        /// Write logs to log file
        /// </summary>
        /// <param name="type">Log level</param>
        /// <param name="tag">tag of detail to log</param>
        /// <param name="detail">Details to log</param>
        /// <param name="ex">Exception</param>
        /// <param name="writeToConsole">Flag indicates whether to write log on console screen or not</param>
        public static void Write(LogLevel type, string tag, string detail, Exception ex)
        {
            if (!DoLog(type)) { return; }
            CheckSession();
            ConsoleWriter.Write(type, tag, detail, ex);
            SetLogFilePath();
            Directory.CreateDirectory(new FileInfo(_logFilePath).Directory.FullName);
            StringBuilder sb = new StringBuilder();
            if (!File.Exists(_logFilePath))
            {
                sb.AppendLine("Type\tDate Time\tTag\tDetails");
            }
            sb.AppendLine($"{type.ToString()}\t{DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff")}\t{tag}\t{detail}");
            string logdberror = ex?.Message;
            if (!string.IsNullOrWhiteSpace(logdberror))
            {
                sb.AppendLine($"DBLoggingError\t{DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff")}\t{logdberror}\t{GetExceptionClean(ex)}");
            }
            File.AppendAllText(_logFilePath, sb.ToString());
            _logRowCount++;
            sb = null;
        }

        private static void SetLogFilePath()
        {
            if (string.IsNullOrWhiteSpace(_logFilePath))
            {
                _logFilePath = $@"{GetLogPath()}\{GetLogSessionPrefix()}\log-{DateTime.Now.ToString("HHmmss")}.log";
                return;
            }
            if (_logRowCount > _maxLogRowCount)
            {
                _logFilePath = $@"{GetLogPath()}\{GetLogSessionPrefix()}\log-{DateTime.Now.ToString("HHmmss")}.log";
            }
        }

        ///<summary>
        /// Get log session prefix
        /// </summary>
        /// <returns>returns log session prefix</returns>
        private static string GetLogSessionPrefix()
        {
            return $"{DateTime.Now.ToString("yyyyMMdd")}-{_sessionId}";
        }

        ///<summary>
        /// Get log file path
        /// </summary>
        /// <returns>returns log file path</returns>
        private static string GetLogPath()
        {
            string path = ConfigProvider.Instance.GetLogPath();
            Directory.CreateDirectory(path);
            return path;
        }

        ///<summary>
        /// Get clean exception by replacing newline, tab with space
        /// </summary>
        /// <param name="ex">Exception object</param>
        /// <returns>returns clean exception</returns>
        private static string GetExceptionClean(Exception ex)
        {
            if (ex == null) { return string.Empty; }
            return System.Text.RegularExpressions.Regex.Replace(ex.ToString(), @"\t|\n|\r", " ");
        }

        /////<summary>
        ///// Email log file
        ///// </summary>
        ///// <param name="subject">Subject of email</param>
        ///// <param name="message">Body of email</param>
        //public static void EmailLog(string subject, string message)
        //{
        //    try
        //    {
        //        SMTPSettings settings = ConfigReader.Config.BrandSetting.SMTP;
        //        string body = $"Hello,<br/><p>{message}</p>";
        //        string[] files = Directory.GetFiles(GetLogPath(), $"{GetLogSessionPrefix()}*.log");
        //        if (files != null && files.Length > 0)
        //        {
        //            body += $"<p><b>Following are the log content</b></p>";
        //            foreach (string file in files)
        //            {
        //                string content = File.ReadAllText(file);
        //                if (!string.IsNullOrWhiteSpace(content))
        //                {
        //                    body += $"<p><pre>{content}</pre></p>";
        //                }
        //            }
        //        }

        //        using (MailMessage msg = new MailMessage())
        //        {
        //            msg.Subject = $"{settings.SubjectPrefix} {subject}";
        //            msg.From = new MailAddress(settings.FromEmail, settings.FromName);
        //            msg.Body = body;
        //            msg.IsBodyHtml = true;
        //            foreach (string to in settings.ToEmail.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
        //            {
        //                if (string.IsNullOrWhiteSpace(to)) { continue; }
        //                msg.To.Add(new MailAddress(to));
        //            }
        //            if (!string.IsNullOrWhiteSpace(settings.CcEmail))
        //            {
        //                foreach (string cc in settings.CcEmail.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
        //                {
        //                    if (string.IsNullOrWhiteSpace(cc)) { continue; }
        //                    msg.CC.Add(new MailAddress(cc));
        //                }
        //            }
        //            if (!string.IsNullOrWhiteSpace(settings.BccEmail))
        //            {
        //                foreach (string bcc in settings.BccEmail.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
        //                {
        //                    if (string.IsNullOrWhiteSpace(bcc)) { continue; }
        //                    msg.Bcc.Add(new MailAddress(bcc));
        //                }
        //            }
        //            using (SmtpClient client = new SmtpClient(settings.Host, settings.Port))
        //            {
        //                client.UseDefaultCredentials = false;
        //                client.Credentials = new NetworkCredential(settings.Username, settings.Password);
        //                client.EnableSsl = settings.UseSSL;
        //                client.Send(msg);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Error("LogProvider.EmailLog", ex);
        //    }
        //}
    }
}
