using System;
using System.IO;
using System.Threading;
using JotTile.Core;

namespace JotTile.Tests
{
    internal sealed class TestWorkspace : IDisposable
    {
        private readonly string _rootPath;

        internal TestWorkspace()
        {
            _rootPath = Path.Combine(Path.GetTempPath(), "JotTile.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_rootPath);
        }

        internal string RootPath
        {
            get { return _rootPath; }
        }

        internal string CreateSubdirectory(string name)
        {
            string path = Path.Combine(_rootPath, name);
            Directory.CreateDirectory(path);
            return path;
        }

        internal AppLogger CreateLogger()
        {
            return new AppLogger(CreateSubdirectory("logs"));
        }

        internal string ReadLog()
        {
            string logPath = Path.Combine(_rootPath, "logs", "app.log");
            for (int attempt = 0; attempt < 5; attempt++)
            {
                if (File.Exists(logPath))
                {
                    return File.ReadAllText(logPath);
                }

                Thread.Sleep(25);
            }

            return string.Empty;
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_rootPath))
                {
                    Directory.Delete(_rootPath, true);
                }
            }
            catch
            {
            }
        }
    }
}
