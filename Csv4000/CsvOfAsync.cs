using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Csv4000
{
    public partial class CsvOf<T>
    {
        private readonly static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly bool useFirstLineAsHeader;
        private string path;

        public CsvOf(string path, bool useFirstLineAsHeader = true)
        {
            this.path = path;
            this.useFirstLineAsHeader = useFirstLineAsHeader;
        }

        public async Task ClearAsync()
        {
            Lock(() =>
            {
                File.Create(path).Close();
            });

            if (useFirstLineAsHeader)
            {
                var properties = typeof(T).GetProperties();

                var lineValues = new List<string>();

                foreach (var prop in properties)
                {
                    lineValues.Add(prop.Name.ToCsvString());
                }

                await OpenWriterAsync(async writer =>
                    await writer.WriteLineAsync(string.Join(";", lineValues))
                );
            }
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
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader reader = new StreamReader(fs))
                {
                    string line;
                    var properties = typeof(T).GetProperties();

                    if (useFirstLineAsHeader)
                        await reader.ReadLineAsync();

                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        var values = line.Split(';');
                        var item = Activator.CreateInstance<T>();

                        var i = 0;
                        foreach (var prop in properties)
                        {
                            if (prop.PropertyType == typeof(string))
                            {
                                prop.SetValue(item, values[i]);
                            }

                            if (prop.PropertyType == typeof(int))
                            {
                                if (int.TryParse(values[i], out int parsedValue))
                                    prop.SetValue(item, parsedValue);
                            }

                            if (prop.PropertyType == typeof(float))
                            {
                                if (float.TryParse(values[i], out float parsedValue))
                                    prop.SetValue(item, parsedValue);
                            }

                            if (prop.PropertyType == typeof(DateTime))
                            {
                                if (DateTime.TryParse(values[i], out DateTime parsedValue))
                                    prop.SetValue(item, parsedValue);
                            }

                            i++;
                        }

                        result.Add(item);
                    }
                }
            });

            return result;
        }

        private async Task OpenWriterAsync(Func<StreamWriter, Task> writterAction)
        {
            await LockAsync(async () =>
            {
                using (FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (StreamWriter streamWriter = new StreamWriter(fs))
                {
                    fs.Lock(0, fs.Length);
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
