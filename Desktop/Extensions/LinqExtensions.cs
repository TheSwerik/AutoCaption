using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Desktop.Extensions;

public static class LinqExtensions
{
    public static async Task<IEnumerable<T1>> SelectManyAsync<T, T1>(this IEnumerable<T> enumeration, Func<T, Task<IEnumerable<T1>>> func)
    {
        var select = await Task.WhenAll(enumeration.Select(func));
        return select.SelectMany(s => s);
    }
}