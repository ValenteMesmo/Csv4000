﻿using Csv4000.Extensions;
using System;
using System.Collections.Generic;
using System.IO;

namespace Csv4000
{
    public partial class CsvOf<T>
    {
        public void Clear()
        {
            Lock(() =>
            {
                File.Delete(path);
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
                  using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                  using (StreamReader reader = new StreamReader(fs))
                  {
                      string line;
                      var properties = typeof(T).GetProperties();

                      if (useFirstLineAsHeader)
                          reader.ReadLine();

                      while ((line = reader.ReadLine()) != null)
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

        private void OpenWriter(Action<StreamWriter> writterAction)
        {
            Lock(() =>
               {
                   var createHeaderLine = File.Exists(path) == false 
                    && useFirstLineAsHeader;

                   using (FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                   using (StreamWriter streamWriter = new StreamWriter(fs))
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
