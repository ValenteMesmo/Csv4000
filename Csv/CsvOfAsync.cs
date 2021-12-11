using Csv4000.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Csv4000
{
    public partial class CsvOf<T>
    {
        public bool UseFirstLineAsHeader = true;
        private readonly static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly string FilePath;

        /// <summary>
        /// Directory used: System.AppDomain.CurrentDomain.BaseDirectory
        /// </summary>
        /// <param name="csvFileName">Example: test.csv</param>
        public CsvOf(string csvFileName) : this(AppDomain.CurrentDomain.BaseDirectory, csvFileName) { }

        /// <summary>
        /// </summary>
        /// <param name="directoryPath">Example: c:\temp</param>
        /// <param name="csvFileName">Example: test.csv</param>
        public CsvOf(string directoryPath, string csvFileName)
        {
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            FilePath = Path.Combine(directoryPath, csvFileName);
        }

        public async Task WriteAsync(T item)
        {
            var properties = typeof(T).GetProperties();

            var lineValues = new List<string>();

            foreach (var prop in properties)
            {
                var value = prop.GetValue(item);
                lineValues.Add(value.ToCsvString());
            }

            await OpenWriterAsync(async writer =>
                 await writer.WriteLineAsync(string.Join(";", lineValues))
            );
        }

        public async Task<IEnumerable<T>> ReadAsync()
        {
            var result = new List<T>();

            await LockAsync(async () =>
            {
                using (FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader reader = new StreamReader(fs))
                {
                    string line;
                    var properties = typeof(T).GetProperties();

                    if (UseFirstLineAsHeader)
                        await reader.ReadLineAsync();

                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        var values = line.Split(';');
                        var item = Activator.CreateInstance<T>();

                        ReadProperties(properties, values, item);

                        result.Add(item);
                    }
                }
            });

            return result;
        }

        private static void ReadProperties(System.Reflection.PropertyInfo[] properties, string[] values, T item)
        {
            var i = 0;
            foreach (var prop in properties)
            {
                if (i >= values.Length)
                    break;

                if (prop.PropertyType == typeof(string))
                {
                    prop.SetValue(item, values[i]);
                }

                if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                {
                    if (int.TryParse(values[i], out int parsedValue))
                        prop.SetValue(item, parsedValue);
                }

                if (prop.PropertyType == typeof(long) || prop.PropertyType == typeof(long?))
                {
                    if (long.TryParse(values[i], out long parsedValue))
                        prop.SetValue(item, parsedValue);
                }

                if (prop.PropertyType == typeof(float) || prop.PropertyType == typeof(float?))
                {
                    if (float.TryParse(values[i], out float parsedValue))
                        prop.SetValue(item, parsedValue);
                }

                if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?))
                {
                    if (bool.TryParse(values[i], out bool parsedValue))
                        prop.SetValue(item, parsedValue);
                }

                if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
                {
                    if (DateTime.TryParse(values[i], out DateTime parsedValue))
                        prop.SetValue(item, parsedValue);
                }

                i++;
            }
        }

        private async Task OpenWriterAsync(Func<StreamWriter, Task> writterAction)
        {
            await LockAsync(async () =>
            {
                var createHeaderLine = File.Exists(FilePath) == false
                    && UseFirstLineAsHeader;

                using (FileStream fs = new FileStream(
                        FilePath
                        , FileMode.Append
                        , FileAccess.Write
                        , FileShare.ReadWrite
                    )
                )
                using (StreamWriter streamWriter = new StreamWriter(fs, Encoding.UTF8))
                {
                    fs.Lock(0, fs.Length);

                    if (createHeaderLine)
                    {
                        var properties = typeof(T).GetProperties();

                        var lineValues = new List<string>();

                        foreach (var prop in properties)
                        {
                            lineValues.Add(prop.Name.ToCsvString());
                        }

                        await streamWriter.WriteLineAsync(string.Join(";", lineValues));
                    }

                    await writterAction(streamWriter);
                }
            });
        }

        private async Task LockAsync(Func<Task> Action)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                await Action();
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
    }
}
