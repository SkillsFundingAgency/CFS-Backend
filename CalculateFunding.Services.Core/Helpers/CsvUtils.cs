using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CalculateFunding.Services.Core.Interfaces;
using CsvHelper;
using Microsoft.Extensions.ObjectPool;

namespace CalculateFunding.Services.Core.Helpers
{
    public class CsvUtils : ICsvUtils
    {
        private readonly StreamWriterObjectPool _streamWriters = new StreamWriterObjectPool(); 
    
        /// <summary>
        /// Returns a pooled stream writer for the supplied documents transformed into CVS rows
        /// NB the calling code is responsible for returning stream writer after use to the
        /// csv utils
        /// </summary>
        public StreamWriter AsCsvStream(IEnumerable<dynamic> documents, bool outputHeaders)
        {
            if (!documents.Any()) return null;

            StreamWriter streamWriter = _streamWriters.Get();
            
            using (CsvWriter csvWriter = new CsvWriter(streamWriter))
            {
                csvWriter.Configuration.ShouldQuote = (value, context) => true;
                csvWriter.Configuration.Quote = '\"';
                csvWriter.Configuration.HasHeaderRecord = outputHeaders;

                csvWriter.WriteRecords(documents);

                streamWriter.Flush();
                streamWriter.BaseStream.Position = 0;
                
                return streamWriter;
            }
        }

        public void ReturnStreamWriter(StreamWriter streamWriter)
        {
            _streamWriters.Return(streamWriter);
        }

        private class StreamWriterObjectPool : ObjectPool<StreamWriter>
        {
            private const int BufferSize = 4096;
            private const int InitialPoolSize = 16;

            private readonly Queue<StreamWriter> _streamWriters;

            public StreamWriterObjectPool()
            {
                _streamWriters = new Queue<StreamWriter>();

                for (int count = 0; count < InitialPoolSize; count++)
                {
                    _streamWriters.Enqueue(NewStreamWriter());
                }
            }

            public override StreamWriter Get()
            {
                lock (_streamWriters)
                {
                    if (!_streamWriters.Any())
                    {
                        _streamWriters.Enqueue(NewStreamWriter());
                    }

                    return _streamWriters.Dequeue();
                }
            }

            public override void Return(StreamWriter obj)
            {
                lock (_streamWriters)
                {
                    obj.BaseStream.SetLength(0);
               
                    _streamWriters.Enqueue(obj);
                }
            }

            private StreamWriter NewStreamWriter()
            {
                return new StreamWriter(new MemoryStream(BufferSize), Encoding.UTF8, BufferSize, true);
            }
        }
    }
}