using HugsLib.Utils;

namespace Law_and_Order.Source.Utils
{
    /// <summary>
    /// Log levels for tiered logging system
    /// </summary>
    public enum LogLevel
    {
        Trace = 0,   // Very detailed logs (method enter/exit, etc.)
        Debug = 1,   // Debug information useful for development
        Info = 2,    // General informational messages
        Warning = 3, // Warning messages for potential issues
        Error = 4,   // Error messages for failures
        None = 5     // No logging
    }

    /// <summary>
    /// Tiered logging system that wraps HugsLib's ModLogger
    /// Provides runtime-configurable log levels to reduce log spam
    /// </summary>
    public static class ModLog
    {
        // Current log level - can be changed at runtime via settings
        private static LogLevel currentLogLevel = LogLevel.Info;

        /// <summary>
        /// Get or set the current log level
        /// </summary>
        public static LogLevel CurrentLogLevel
        {
            get => currentLogLevel;
            set => currentLogLevel = value;
        }

        /// <summary>
        /// Log a trace message (most detailed)
        /// Only logged if log level is Trace
        /// </summary>
        public static void Trace(string message)
        {
            if (currentLogLevel <= LogLevel.Trace && Mod.Log != null)
            {
                Mod.Log.Message($"[TRACE] {message}");
            }
        }

        /// <summary>
        /// Log a debug message
        /// Only logged if log level is Debug or lower
        /// </summary>
        public static void Debug(string message)
        {
            if (currentLogLevel <= LogLevel.Debug && Mod.Log != null)
            {
                Mod.Log.Message($"[DEBUG] {message}");
            }
        }

        /// <summary>
        /// Log an informational message
        /// Only logged if log level is Info or lower
        /// </summary>
        public static void Info(string message)
        {
            if (currentLogLevel <= LogLevel.Info && Mod.Log != null)
            {
                Mod.Log.Message(message);
            }
        }

        /// <summary>
        /// Log a warning message
        /// Only logged if log level is Warning or lower
        /// </summary>
        public static void Warning(string message)
        {
            if (currentLogLevel <= LogLevel.Warning && Mod.Log != null)
            {
                Mod.Log.Warning(message);
            }
        }

        /// <summary>
        /// Log an error message
        /// Only logged if log level is Error or lower (not None)
        /// </summary>
        public static void Error(string message)
        {
            if (currentLogLevel <= LogLevel.Error && Mod.Log != null)
            {
                Mod.Log.Error(message);
            }
        }

        /// <summary>
        /// Check if a specific log level is enabled
        /// Useful for avoiding expensive string operations if logging is disabled
        /// </summary>
        public static bool IsEnabled(LogLevel level)
        {
            return currentLogLevel <= level;
        }

        /// <summary>
        /// Get log level name for display purposes
        /// </summary>
        public static string GetLogLevelName(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    return "Trace (Very Verbose)";
                case LogLevel.Debug:
                    return "Debug (Verbose)";
                case LogLevel.Info:
                    return "Info (Normal)";
                case LogLevel.Warning:
                    return "Warning (Quiet)";
                case LogLevel.Error:
                    return "Error (Very Quiet)";
                case LogLevel.None:
                    return "None (Silent)";
                default:
                    return level.ToString();
            }
        }
    }
}
