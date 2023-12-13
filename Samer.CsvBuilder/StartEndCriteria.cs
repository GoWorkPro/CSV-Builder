using System;
using System.Collections.Generic;
using System.Text;

namespace GoWorkPro.CsvBuilder
{
    public class StartEndCriteria
    {
        public StartEndCriteria(Func<ReadCriteria, bool> startCriteria, Func<ReadCriteria, bool> endCriteria) : this()
        {
            StartCriteria = startCriteria;
            EndCriteria = endCriteria;
        }

        internal StartEndCriteria()
        {
        }

        public StartEndCriteria(Func<ReadCriteria, bool> startCriteria) : this(startCriteria, null)
        {
        }

        public Func<ReadCriteria, bool>? StartCriteria { get; }
        public Func<ReadCriteria, bool>? EndCriteria { get; }
    }
}
