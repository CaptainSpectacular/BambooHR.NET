using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using CsvHelper;
using BambooConsumer.Models;

namespace BambooConsumer
{
    class CsvUpdates
    {
        private readonly string _docPath;

        public CsvUpdates(string docPath)
        {
            _docPath = docPath;
        }

        public void WriteToFile(List<BambooEmployee> data, string docName)
        {
            using (var mem = new MemoryStream())
            using (var writer = new StreamWriter(Path.Combine(_docPath, docName)))
            using (var csvWriter = new CsvWriter(writer))
            {
                csvWriter.Configuration.HasHeaderRecord = true;
                csvWriter.Configuration.AutoMap<BambooEmployee>();
                csvWriter.WriteHeader<BambooEmployee>();
                csvWriter.NextRecord();
                csvWriter.WriteRecords(data);
                writer.Flush();

                var result = Encoding.UTF8.GetString(mem.ToArray());
                Console.WriteLine(result);
            }
        }
    }
}
