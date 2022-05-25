using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ca.awsLargeJsonTransform.Framework
{
    /// <summary>
    /// Represents a log level
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Trace
        /// </summary>
        Trace = 5,
        /// <summary>
        /// Debug
        /// </summary>
        Debug = 10,
        /// <summary>
        /// Information
        /// </summary>
        Information = 20,
        /// <summary>
        /// Warning
        /// </summary>
        Warning = 30,
        /// <summary>
        /// Error
        /// </summary>
        Error = 40,
        /// <summary>
        /// Fatal
        /// </summary>
        Fatal = 50
    }
}
