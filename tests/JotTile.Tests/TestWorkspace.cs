using System;
using System.IO;
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
