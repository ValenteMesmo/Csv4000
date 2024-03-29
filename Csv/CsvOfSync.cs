﻿using Csv4000.Extensions;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Csv4000
{
    public partial class CsvOf<T>
    {
        public void Clear()
        {
            Lock(() =>
            {
                try
                {
                    File.Delete(FilePath);
                }
                catch { }
            });
        }

        public void Write(T item)
        {
            var properties = typeof(T).GetProperties();

            var lineValues = new List<string>();

            foreach (var prop in properties)
            {
                var value = prop.GetValue(item);
                lineValues.Add(value.ToCsvString());
            }

            OpenWriter(writer =>
                 writer.WriteLine(string.Join(";", lineValues))
            );
        }

        public IEnumerable<T> Read()
        {
            var result = new List<T>();

            Lock(() =>
              {
                  using (FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                  using (StreamReader reader = new StreamReader(fs))
                  {
                      string line;
                      var properties = typeof(T).GetProperties();

                      if (UseFirstLineAsHeader)
                          reader.ReadLine();

                      var Regex = new System.Text.RegularExpressions.Regex(@"(?:^|;)(?=[^""]|("")?)""?((?(1)[^""]*|[^;""]*))""?(?=;|$)");
                      while ((line = reader.ReadLine()) != null)
                      {
                          var values = Regex.Matches(line).OfType<System.Text.RegularExpressions.Match>().Select(f => f.Groups[2].Value).ToArray();
                          var item = Activator.CreateInstance<T>();

                          ReadProperties(properties, values, item);

                          result.Add(item);
                      }
                  }
              });

            return result;
        }

        private void OpenWriter(Action<StreamWriter> writterAction)
        {
            Lock(() =>
               {
                   var createHeaderLine = File.Exists(FilePath) == false
                    && UseFirstLineAsHeader;

                   using (FileStream fs = new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
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

                           streamWriter.WriteLine(string.Join(";", lineValues));
                       }

                       writterAction(streamWriter);
                   }
               });
        }

        private void Lock(Action Action)
        {
            semaphoreSlim.Wait();
            try
            {
                Action();
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
    }
}
