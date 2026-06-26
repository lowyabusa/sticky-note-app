using System;
using System.IO;
using System.Text;

namespace JotTile.Core
{
    internal sealed class AppLogger
    {
        private const long MaxLogSizeBytes = 1024 * 1024;
        private readonly object _syncRoot;
        private readonly string _logDirectoryPath;
        private readonly string _logFilePath;

        internal AppLogger()
            : this(AppIdentity.GetLocalLogDirectoryPath())
        {
        }

        internal AppLogger(string logDirectoryPath)
        {
            _syncRoot = new object();
            _logDirectoryPath = logDirectoryPath;
            _logFilePath = Path.Combine(logDirectoryPath, "app.log");
        }

        internal void Info(string operation, string message)
        {
            Write(LogSeverity.Info, operation, message, null);
        }

        internal void Warning(string operation, string message, Exception? exception = null)
        {
            Write(LogSeverity.Warning, operation, message, exception);
        }

        internal void Error(string operation, string message, Exception? exception = null)
        {
            Write(LogSeverity.Error, operation, message, exception);
        }

        private void Write(LogSeverity severity, string operation, string message, Exception? exception)
        {
            try
            {
                lock (_syncRoot)
                {
                    Directory.CreateDirectory(_logDirectoryPath);
                    RotateIfNeeded();

                    using (StreamWriter writer = new StreamWriter(_logFilePath, true, new UTF8Encoding(false)))
                    {
                        writer.WriteLine(
                            "{0:u} [{1}] {2} - {3}",
                            DateTime.UtcNow,
                            severity,
                            operation,
                            message);

                        if (exception != null)
                        {
                            writer.WriteLine("Exception: {0}", exception.GetType().FullName);
                            writer.WriteLine("Message: {0}", exception.Message);
                            if (!string.IsNullOrWhiteSpace(exception.StackTrace))
                            {
                                writer.WriteLine(exception.StackTrace);
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void RotateIfNeeded()
        {
            try
            {
                if (!File.Exists(_logFilePath))
                {
                    return;
                }

                FileInfo info = new FileInfo(_logFilePath);
                if (info.Length < MaxLogSizeBytes)
                {
                    return;
                }

                string rotatedPath = _logFilePath + ".1";
                if (File.Exists(rotatedPath))
                {
                    File.Delete(rotatedPath);
                }

                File.Move(_logFilePath, rotatedPath);
            }
            catch
            {
            }
        }

        private enum LogSeverity
        {
            Info,
            Warning,
            Error
        }
    }
}
