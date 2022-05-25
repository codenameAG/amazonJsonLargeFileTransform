namespace ca.awsLargeJsonTransform.Framework
{
    public class ConsoleWriter
    {        
        static ConsoleWriter()
        {
        }

        public static bool DoLog(LogLevel type)
        {
            try
            {
                string? config = ConfigProvider.Instance.GetConsoleLogType();
                if (string.IsNullOrWhiteSpace(config) || config == "trace") { return true; }
                if (config == "debug" && type != LogLevel.Trace) { return true; }
                if (config == "error" && type != LogLevel.Trace && type != LogLevel.Debug) { return true; }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return true;
            }
        }

        public static void Write(string message)
        {

            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine($"{message.Trim()} at {DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")}.");
        }

        public static void Write(LogLevel type, string message, string detail, Exception ex)
        {
            message = $"{message} {detail}".Trim();
            if (ex != null)
            {
                message += $"{message}\r\n\tError Stack:{ex.ToString()}";
            }
            Write(type, message);
        }
        public static void Write(LogLevel type, string message)
        {
            if (!DoLog(type)) { return; }
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            switch (type)
            {
                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogLevel.Debug:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LogLevel.Trace:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    break;
                case LogLevel.Information:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }
            Write($"{type.ToString()}: {message}");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
