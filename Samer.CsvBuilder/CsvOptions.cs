using System;
using System.Collections.Generic;
using System.Text;

namespace GoWorkPro.CsvBuilder
{
    public class Options
    {
        public int SkipInitialNumberOfRows { get; set; } = 0;
        public Func<string, int, bool>? SkipRow { get; set; }
        public char Separator { get; set; } = ',';
        public bool TrimData { get; set; }
        public HeaderMode HeaderMode { get; set; } = HeaderMode.NoPresent;
        public string NewLine { get; set; } = Environment.NewLine;
        public bool RemoveEmptyRows { get; set; } = false;
    }

    public enum HeaderMode
    {
        HeaderPresent,
        NoPresent
    }
}
