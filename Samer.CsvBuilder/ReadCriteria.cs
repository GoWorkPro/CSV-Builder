using System;
using System.Collections.Generic;
using System.Text;

namespace GoWorkPro.CsvBuilder
{
    public class ReadCriteria
    {
        public int? ColumnNumber { get; set; }
        public int? RowNumber { get; set; }
        public object? Value { get; set; }
    }
}
