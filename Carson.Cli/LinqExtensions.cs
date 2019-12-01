using System;
using System.Collections.Generic;
using System.Linq;

namespace Experiment1
{
	public static class LinqExtensions
	{
		public static IEnumerable<T> Flatten<T, R>(this IEnumerable<T> source, Func<T, R> recursion) where R : IEnumerable<T>
		{
			return source.SelectMany(x => (recursion(x) != null && recursion(x).Any()) ? recursion(x).Flatten(recursion) : null)
						 .Where(x => x != null);
		}
	}
}
