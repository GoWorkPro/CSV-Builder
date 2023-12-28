using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GoWorkPro.CsvBuilder
{
    public static class ListExtensions
    {
        public static void Resize<T>(this List<T> list, int newSize, T defaultValue)
        {
            int currentSize = list.Count;
            if (newSize > currentSize)
            {
                list.AddRange(Enumerable.Repeat(defaultValue, newSize - currentSize));
            }
            else if (newSize < currentSize)
            {
                list.RemoveRange(newSize, currentSize - newSize);
            }
        }
    }
}
