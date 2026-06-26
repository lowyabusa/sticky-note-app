using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace JotTile.Core
{
    internal static class JsonFileRepositoryHelpers
    {
        internal static T Read<T>(string filePath, Func<DataContractJsonSerializer> serializerFactory)
        {
            using (FileStream stream = File.OpenRead(filePath))
            {
                DataContractJsonSerializer serializer = serializerFactory();
                object? value = serializer.ReadObject(stream);
                if (value is T typedValue)
                {
                    return typedValue;
                }

                throw new SerializationException("The JSON payload could not be deserialized into the expected type.");
            }
        }

        internal static void AtomicSave<T>(
            string directoryPath,
            string filePath,
            string backupFilePath,
            string tempFileName,
            T value,
            Func<DataContractJsonSerializer> serializerFactory)
        {
            Directory.CreateDirectory(directoryPath);
            string tempFilePath = Path.Combine(directoryPath, tempFileName);

            try
            {
                using (FileStream stream = File.Create(tempFilePath))
                {
                    DataContractJsonSerializer serializer = serializerFactory();
                    serializer.WriteObject(stream, value);
                    stream.Flush(true);
                }

                if (File.Exists(filePath))
                {
                    File.Replace(tempFilePath, filePath, backupFilePath, true);
                }
                else
                {
                    File.Move(tempFilePath, filePath);
                }
            }
            finally
            {
                TryDelete(tempFilePath);
            }
        }

        internal static string Quarantine(string directoryPath, string filePath, string prefix)
        {
            string quarantinePath = Path.Combine(
                directoryPath,
                string.Format(
                    "{0}.corrupt.{1}{2}",
                    prefix,
                    DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                    Path.GetExtension(filePath)));

            File.Move(filePath, quarantinePath);
            return quarantinePath;
        }

        internal static void TryDelete(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
            }
        }
    }
}
