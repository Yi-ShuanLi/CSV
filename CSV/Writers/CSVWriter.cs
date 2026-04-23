using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSV.Writers
{
    internal abstract class CSVWriter
    {
        public abstract void WriteData<T>(string filePath, List<T> objs);
        public abstract Task WriteDataAsync<T>(string filePath, List<T> objs, CancellationToken token);
    }
}
