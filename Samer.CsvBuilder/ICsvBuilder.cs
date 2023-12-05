using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GoWorkPro.CsvBuilder.CsvBuilder;

namespace GoWorkPro.CsvBuilder
{
    public interface ICsvBuilder : IDisposable
    {
        ICsvExtractor Build(params int[] columnsTobePresentedForTableIndex);

        event ValueParser ValueRenderEvent;
    }
}
