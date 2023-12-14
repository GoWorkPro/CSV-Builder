using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GoWorkPro.CsvBuilder.CsvBuilder;

namespace GoWorkPro.CsvBuilder
{
    /// <summary>
    /// Represents a builder for constructing CSV data and extracting specific columns.
    /// </summary>
    public interface ICsvBuilder : IDisposable
    {
        /// <summary>
        /// Builds a CSV extractor with the specified columns to be presented for each table index.
        /// </summary>
        /// <param name="columnsToBePresentedForTableIndex">An array of integers representing the columns to be presented for each table index.</param>
        /// <returns>An instance of <see cref="ICsvExtractor"/> configured with the specified columns to be presented.</returns>
        ICsvExtractor Build(params int[] columnsToBePresentedForTableIndex);

        /// <summary>
        /// Occurs when a value needs to be rendered during the CSV building process.
        /// Subscribers can provide custom logic to parse and render values.
        /// </summary>
        event ValueParser ValueRenderEvent;
    }

}
