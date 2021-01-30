using System;
using System.Collections.Generic;
using System.IO;

namespace MultiLotrConquestServerLauncher
{
    class Logger
    {
        private static Logger _instance;
        private readonly string _logFilePath;
        private string _logLevel = "all";

        private Logger(string logFilePath) {
            _logFilePath = logFilePath;
        }

        public static Logger GetInstance(string logFilePath) {

            if (_instance == null) {
                if (string.IsNullOrEmpty(logFilePath)) {
                    throw new MultiServerException("LogFilePath must be specified");
                }

                _instance = new Logger(logFilePath);
                return _instance;
            }

            return _instance;
        }

        public static Logger GetInstance() {
            if (_instance == null) {
                throw new MultiServerException("Logger instance must be initially instantiated providing a LogFilePath");
            }

            return _instance;
        }

        public void SetLogLevel(string logLevel) {
            var validLogLevels = new List<string> {
                "debug", "info", "warn", "error", "fatal"
            };

            var logLevelLowercase = logLevel.ToLower();

            if (validLogLevels.IndexOf(logLevelLowercase) == -1) {
                // invalid log level
                return;
            }

            _logLevel = logLevelLowercase;
        }

        private void Log(string level, string message) {
            var timestamp = DateTime.UtcNow.ToString("o");
            var logMessage = $"[{level.ToUpper()}] [{timestamp}] {message}";

            try
            {
                Console.WriteLine(logMessage);
                File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception exc)
            {
                Console.WriteLine($"Failed to write to log file:\n{exc.Message}");
            }
        }

        public void Debug(string message) {
            switch (_logLevel) {
                case "all":
                case "debug":
                    Log("debug", message);
                    break;
                default:
                    // nothing
                    break;
            } 
        }

        public void Info(string message) {
            switch (_logLevel) {
                case "all":
                case "debug":
                case "info":
                    Log("info", message);
                    break;
                default:
                    // nothing
                    break;
            }
        }

        public void Warn(string message)
        {
            switch (_logLevel) {
                case "all":
                case "debug":
                case "info":
                case "warn":
                    Log("warn", message);
                    break;
                default:
                    // nothing
                    break;
            }
        }

        public void Error(string message)
        {
            switch (_logLevel) {
                case "all":
                case "debug":
                case "info":
                case "warn":
                case "error":
                    Log("error", message);
                    break;
                default:
                    // nothing
                    break;
            }
        }

        public void Fatal(string message)
        {
            Log("fatal", message);
        }
    }
}
