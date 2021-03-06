namespace Dixin.Linq.Parallel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

#if NETFX
    using Microsoft.ConcurrencyVisualizer.Instrumentation;
#endif

    using static Functions;

    using Stopwatch = System.Diagnostics.Stopwatch;

    internal static partial class QueryMethods
    {
        private static readonly Assembly CoreLibrary = typeof(object).GetTypeInfo().Assembly;

        internal static void OptInOutParallel()
        {
            IEnumerable<string> obsoleteTypes = CoreLibrary.GetExportedTypes() // Return IEnumerable<Type>.
                .AsParallel() // Return ParallelQuery<Type>.
                .Where(type => type.GetTypeInfo().GetCustomAttribute<ObsoleteAttribute>() != null) // ParallelEnumerable.Where.
                .Select(type => type.FullName) // ParallelEnumerable.Select.
                .AsSequential() // Return IEnumerable<Type>.
                .OrderBy(name => name); // Enumerable.OrderBy.
            obsoleteTypes.ForEach(name => name.WriteLine());
        }
    }

    internal static partial class QueryMethods
    {
        internal static void QueryExpression()
        {
            IEnumerable<string> obsoleteTypes =
                from name in
                    (from type in CoreLibrary.GetExportedTypes().AsParallel()
                     where type.GetTypeInfo().GetCustomAttribute<ObsoleteAttribute>() != null
                     select type.FullName).AsSequential()
                orderby name
                select name;
            obsoleteTypes.ForEach(name => name.WriteLine());
        }

        internal static void ForEachForAll()
        {
            Enumerable
                .Range(0, Environment.ProcessorCount * 2)
                .ForEach(value => value.WriteLine()); // 0 1 2 3 4 5 6 7

            ParallelEnumerable
                .Range(0, Environment.ProcessorCount * 2)
                .ForAll(value => value.WriteLine()); // 2 6 4 0 5 3 7 1
        }

#if NETFX
        internal static void ForEachForAllTimeSpans()
        {
            string sequentialTimeSpanName = nameof(EnumerableEx.ForEach);
            // Render a timespan for the entire sequential LINQ query execution, with text label "ForEach".
            using (Markers.EnterSpan(0, sequentialTimeSpanName))
            {
                MarkerSeries markerSeries = Markers.CreateMarkerSeries(sequentialTimeSpanName);
                Enumerable.Range(0, Environment.ProcessorCount * 2).ForEach(value =>
                {
                    // Render a sub timespan for each action execution, with each value as text label.
                    using (markerSeries.EnterSpan(Thread.CurrentThread.ManagedThreadId, value.ToString()))
                    {
                        // Add workload to extend the action execution to a more visible timespan.
                        Enumerable.Range(0, 10000000).ForEach();
                        value.WriteLine();
                    }
                });
            }

            string parallelTimeSpanName = nameof(ParallelEnumerable.ForAll);
            // Render a timespan for the entire parallel LINQ query execution, with text label "ForAll".
            using (Markers.EnterSpan(1, parallelTimeSpanName))
            {
                MarkerSeries markerSeries = Markers.CreateMarkerSeries(parallelTimeSpanName);
                ParallelEnumerable.Range(0, Environment.ProcessorCount * 2).ForAll(value =>
                {
                    // Render a sub timespan for each action execution, with each value as text label.
                    using (markerSeries.EnterSpan(Thread.CurrentThread.ManagedThreadId, value.ToString()))
                    {
                        // Add workload to extends the action execution to a more visible timespan.
                        Enumerable.Range(0, 10000000).ForEach();
                        value.WriteLine();
                    }
                });
            }
        }
#endif

        internal static void VisualizeForEachForAll()
        {
            Enumerable
                .Range(0, Environment.ProcessorCount * 2)
                .Visualize(value =>
                    {
                        Enumerable.Range(0, 10000000).ForEach();
                        value.WriteLine();
                    });

            ParallelEnumerable
                .Range(0, Environment.ProcessorCount * 2)
                .Visualize(value =>
                    {
                        Enumerable.Range(0, 10000000).ForEach();
                        value.WriteLine();
                    });
        }

        //  using static Functions;
        internal static void WhereSelect()
        {
            Enumerable
                .Range(0, 2)
                .Visualize(Enumerable.Where, _ => Compute() >= 0, value => $"{nameof(Enumerable.Where)} {value}")
                .Visualize(Enumerable.Select, _ => Compute(), value => $"{nameof(Enumerable.Select)} {value}")
                .ForEach();

            ParallelEnumerable
                .Range(0, Environment.ProcessorCount * 2)
                .Visualize(
                    ParallelEnumerable.Where,
                    _ => Compute() >= 0,
                    value => $"{nameof(ParallelEnumerable.Where)} {value}")
                .Visualize(
                    ParallelEnumerable.Select,
                    _ => Compute(),
                    value => $"{nameof(ParallelEnumerable.Select)} {value}")
                .ForAll(_ => { });
        }

        internal static void Cancel()
        {
            CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            try
            {
                ParallelEnumerable.Range(0, Environment.ProcessorCount * 10)
                    .WithCancellation(cancellation.Token)
                    .Select(value => Compute(value))
                    .ForAll(value => value.WriteLine());
            }
            catch (OperationCanceledException exception)
            {
                exception.WriteLine();
                // OperationCanceledException: The query has been canceled via the token supplied to WithCancellation.
            }
        }

        internal static void DegreeOfParallelism()
        {
            int count = Environment.ProcessorCount * 10;
            ParallelEnumerable
                .Range(0, count)
                .WithDegreeOfParallelism(count)
                .Visualize(value => Compute());
        }

#if NETFX
        public static void ExecutionMode()
        {
            int count = Environment.ProcessorCount * 10000;
            using (Markers.EnterSpan(-1, nameof(Enumerable)))
            {
                Enumerable
                    .Range(0, count)
                    .ToArray();
            }

            using (Markers.EnterSpan(0, nameof(ParallelExecutionMode.Default)))
            {
                ParallelEnumerable
                    .Range(0, count)
                    .ToArray();
            }

            using (Markers.EnterSpan(1, nameof(ParallelExecutionMode.ForceParallelism)))
            {
                ParallelEnumerable
                    .Range(0, count)
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .ToArray();
            }
        }
#endif

        internal static void Except() => 
            ParallelEnumerable
                .Range(0, Environment.ProcessorCount * 2)
                .Visualize(ParallelEnumerable.Select, value => Compute(value))
                .Except(ParallelEnumerable.Repeat(0, 1))
                .Visualize(ParallelEnumerable.Select, value => Compute(value))
                .ForAll(_ => { });

        internal static void MergeForSelect()
        {
            int count = 100000;
            Stopwatch stopwatch = Stopwatch.StartNew();
            ParallelQuery<int> notBuffered = ParallelEnumerable.Range(0, count)
                .WithMergeOptions(ParallelMergeOptions.NotBuffered)
                .Select(value => value + Compute(0, 1000));
            notBuffered.ForEach(value =>
            {
                if (value <= 5 || value >= count - 5)
                {
                    $"{value}:{stopwatch.ElapsedMilliseconds}".WriteLine();
                }
            });
            // 0:43 1:155 2:158 3:244 4:244 5:245 99995:245 99996:246 99997:246 99998:247 99999:247

            stopwatch.Restart();
            ParallelQuery<int> autoBuffered = ParallelEnumerable.Range(0, count)
                .WithMergeOptions(ParallelMergeOptions.AutoBuffered)
                .Select(value => value + Compute(0, 1000));
            autoBuffered.ForEach(value =>
            {
                if (value <= 5 || value >= count - 5)
                {
                    $"{value}:{stopwatch.ElapsedMilliseconds}".WriteLine();
                }
            });
            // 0:101 1:184 2:186 3:188 4:191 5:192 99995:199 99996:202 99997:226 99998:227 99999:227

            stopwatch.Restart();
            ParallelQuery<int> fullyBuffered = ParallelEnumerable.Range(0, count)
                .WithMergeOptions(ParallelMergeOptions.FullyBuffered)
                .Select(value => value + Compute(0, 1000));
            fullyBuffered.ForEach(value =>
            {
                if (value <= 5 || value >= count - 5)
                {
                    $"{value}:{stopwatch.ElapsedMilliseconds}".WriteLine();
                }
            });
            // 0:186 1:187 2:187 3:188 4:189 5:189 99995:190 99996:190 99997:191 99998:191 99999:192
        }

        internal static void MergeForTakeWhile()
        {
            int count = 1000;

            Stopwatch stopwatch = Stopwatch.StartNew();
            ParallelQuery<int> notBuffered = ParallelEnumerable.Range(0, count)
                .WithMergeOptions(ParallelMergeOptions.NotBuffered)
                .TakeWhile(value => value + Compute(0, 100000) >= 0);
            notBuffered.ForEach(value =>
                {
                    if (value <= 5 || value >= count - 5)
                    {
                        $"{value}:{stopwatch.ElapsedMilliseconds}".WriteLine();
                    }
                });
            // 0:237 1:239 2:240 3:241 4:241 5:242 995:242 996:243 997:243 998:244 999:244

            stopwatch.Restart();
            ParallelQuery<int> fullyBuffered = ParallelEnumerable.Range(0, count)
                .WithMergeOptions(ParallelMergeOptions.FullyBuffered)
                .TakeWhile(value => value + Compute(0, 100000) >= 0);
            fullyBuffered.ForEach(value =>
                {
                    if (value <= 5 || value >= count - 5)
                    {
                        $"{value}:{stopwatch.ElapsedMilliseconds}".WriteLine();
                    }
                });
            // 0:193 1:193 2:194 3:194 4:195 5:195 995:195 996:196 997:196 998:197 999:198
        }

        internal static void MergeForOrderBy()
        {
            int count = Environment.ProcessorCount * 2;
            Stopwatch stopwatch = Stopwatch.StartNew();
            ParallelEnumerable.Range(0, count)
                .WithMergeOptions(ParallelMergeOptions.NotBuffered)
                .Select(value => Compute(value))
                .ForEach(value => $"{value}:{stopwatch.ElapsedMilliseconds}".WriteLine());
            // 0:132 2:273 1:315 4:460 3:579 6:611 5:890 7:1103

            stopwatch.Restart();
            ParallelEnumerable.Range(0, count)
                .WithMergeOptions(ParallelMergeOptions.NotBuffered)
                .Select(value => Compute(value))
                .OrderBy(value => value)
                .ForEach(value => $"{value}:{stopwatch.ElapsedMilliseconds}".WriteLine());
            // 0:998 1:999 2:999 3:1000 4:1000 5:1000 6:1001 7:1001

            stopwatch.Restart();
            ParallelEnumerable.Range(0, count)
                .WithMergeOptions(ParallelMergeOptions.FullyBuffered)
                .Select(value => Compute(value))
                .OrderBy(value => value)
                .ForEach(value => $"{value}:{stopwatch.ElapsedMilliseconds}".WriteLine());
            // 0:984 1:985 2:985 3:986 4:987 5:987 6:988 7:989
        }

        internal static void CommutativeAssociative()
        {
            Func<int, int, int> func1 = (a, b) => a + b;
            (func1(1, 2) == func1(2, 1)).WriteLine(); // True, commutative
            (func1(func1(1, 2), 3) == func1(1, func1(2, 3))).WriteLine(); // True, associative.

            Func<int, int, int> func2 = (a, b) => a * b + 1;
            (func2(1, 2) == func2(2, 1)).WriteLine(); // True, commutative
            (func2(func2(1, 2), 3) == func2(1, func2(2, 3))).WriteLine(); // False, not associative.

            Func<int, int, int> func3 = (a, b) => a;
            (func3(1, 2) == func3(2, 1)).WriteLine(); // False, not commutative
            (func3(func3(1, 2), 3) == func3(1, func3(2, 3))).WriteLine(); // True, associative.

            Func<int, int, int> func4 = (a, b) => a - b;
            (func4(1, 2) == func4(2, 1)).WriteLine(); // False, not commutative
            (func4(func4(1, 2), 3) == func4(1, func4(2, 3))).WriteLine(); // False, not associative.
        }

        internal static void AggregateCorrectness()
        {
            int count = Environment.ProcessorCount * 2;
            int sequentialAdd = Enumerable.Range(0, count).Aggregate((a, b) => a + b);
            sequentialAdd.WriteLine(); // 28
            int parallelAdd = ParallelEnumerable.Range(0, count).Aggregate((a, b) => a + b);
            parallelAdd.WriteLine(); // 28

            int sequentialSubtract = Enumerable.Range(0, count).Aggregate((a, b) => a - b);
            sequentialSubtract.WriteLine(); // -28
            int parallelSubtract = ParallelEnumerable.Range(0, count).Aggregate((a, b) => a - b);
            parallelSubtract.WriteLine(); // 2
        }

#if NETFX
        internal static void VisualizeAggregate()
        {
            int count = Environment.ProcessorCount * 2;
            using (Markers.EnterSpan(0, "Sequential subtract"))
            {
                MarkerSeries markerSeries = Markers.CreateMarkerSeries("Sequential subtract");
                int sequentialSubtract = Enumerable.Range(0, count).Aggregate((a, b) =>
                {
                    using (markerSeries.EnterSpan(Thread.CurrentThread.ManagedThreadId, $"{a}, {b} => {a - b}"))
                    {
                        return a - b + Compute();
                    }
                });
            }

            using (Markers.EnterSpan(1, "Parallel subtract"))
            {
                MarkerSeries markerSeries = Markers.CreateMarkerSeries("Parallel subtract");
                int parallelSubtract = ParallelEnumerable.Range(0, count).Aggregate((a, b) =>
                {
                    using (markerSeries.EnterSpan(Thread.CurrentThread.ManagedThreadId, $"{a}, {b} => {a - b}"))
                    {
                        return a - b + Compute();
                    }
                });
            }
        }
#endif

        internal static void MergeForAggregate()
        {
            int count = Environment.ProcessorCount * 2;
            int sequentialSumOfSquares = Enumerable
                .Range(0, count)
                .Aggregate(seed: 0, func: (accumulate, value) => accumulate + value * value);
            sequentialSumOfSquares.WriteLine(); // 140

            int parallelSumOfSquares1 = ParallelEnumerable
                .Range(0, Environment.ProcessorCount * 2)
                .Aggregate(
                    seed: 0, // Seed for each partition.
                    updateAccumulatorFunc: (partition, value) => partition + value * value,
                    combineAccumulatorsFunc: (allPartitions, partition) => allPartitions + partition,
                    resultSelector: result => result);
            parallelSumOfSquares1.WriteLine(); // 140

            int parallelSumOfSquares2 = ParallelEnumerable
                .Range(0, Environment.ProcessorCount * 2)
                .Aggregate(
                    seedFactory: () => 0, // Seed for each partition.
                    updateAccumulatorFunc: (partition, value) => partition + value * value,
                    combineAccumulatorsFunc: (allPartitions, partition) => allPartitions + partition,
                    resultSelector: result => result);
            parallelSumOfSquares2.WriteLine(); // 140
        }
    }
}

#if DEMO
namespace System.Linq
{
    using System.Collections.Generic;

    public static class Enumerable
    {
        public static IEnumerable<TSource> Where<TSource>(
            this IEnumerable<TSource> source, Func<TSource, bool> predicate);

        public static IEnumerable<TResult> Select<TSource, TResult>(
            this IEnumerable<TSource> source, Func<TSource, TResult> selector);

        public static IEnumerable<TSource> Concat<TSource>(
            this IEnumerable<TSource> first, IEnumerable<TSource> second);

        // More query methods...
    }

    public static class ParallelEnumerable
    {
        public static ParallelQuery<TSource> Where<TSource>(
            this ParallelQuery<TSource> source, Func<TSource, bool> predicate);

        public static ParallelQuery<TResult> Select<TSource, TResult>(
            this ParallelQuery<TSource> source, Func<TSource, TResult> selector);

        public static ParallelQuery<TSource> Concat<TSource>(
            this ParallelQuery<TSource> first, ParallelQuery<TSource> second);

        // More query methods...
    }
}

namespace System.Linq
{
    using System.Collections.Generic;

    public static class Enumerable
    {
        public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(
            this IEnumerable<TSource> source, Func<TSource, TKey> keySelector);

        public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(
            this IEnumerable<TSource> source, Func<TSource, TKey> keySelector);

        public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(
            this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector);

        public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(
            this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector);
    }

    public static class ParallelEnumerable
    {
        public static OrderedParallelQuery<TSource> OrderBy<TSource, TKey>(
            this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector);

        public static OrderedParallelQuery<TSource> OrderByDescending<TSource, TKey>(
            this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector);

        public static OrderedParallelQuery<TSource> ThenBy<TSource, TKey>(
            this OrderedParallelQuery<TSource> source, Func<TSource, TKey> keySelector);

        public static OrderedParallelQuery<TSource> ThenByDescending<TSource, TKey>(
            this OrderedParallelQuery<TSource> source, Func<TSource, TKey> keySelector);
    }
}

namespace System.Linq
{
    using System.Collections.Generic;

    public static class EnumerableEx
    {
        public static void ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource> onNext);
    }

    public static class ParallelEnumerable
    {
        public static void ForAll<TSource>(this ParallelQuery<TSource> source, Action<TSource> action);
    }
}

namespace Microsoft.ConcurrencyVisualizer.Instrumentation
{
    public static class Markers
    {
        public static Span EnterSpan(int category, string text);
    }

    public class MarkerSeries
    {
        public static Span EnterSpan(int category, string text);
    }
}

namespace System.Linq.Parallel
{
    internal static class Scheduling
    {
        internal static int DefaultDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 512);

        internal static int GetDefaultDegreeOfParallelism() => DefaultDegreeOfParallelism;
    }
}

namespace System.Linq
{
    using System.Collections.Generic;

    public static class Enumerable
    {
        public static IEnumerable<TSource> Concat<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second);
    }

    public static class ParallelEnumerable
    {
        public static ParallelQuery<TSource> Concat<TSource>(this ParallelQuery<TSource> first, ParallelQuery<TSource> second);

        public static TResult Aggregate<TSource, TAccumulate, TResult>(
            this ParallelQuery<TSource> source, 
            TAccumulate seed, 
            Func<TAccumulate, TSource, TAccumulate> updateAccumulatorFunc, 
            Func<TAccumulate, TAccumulate, TAccumulate> combineAccumulatorsFunc, 
            Func<TAccumulate, TResult> resultSelector);

        public static TResult Aggregate<TSource, TAccumulate, TResult>(
            this ParallelQuery<TSource> source, 
            Func<TAccumulate> seedFactory, 
            Func<TAccumulate, TSource, TAccumulate> updateAccumulatorFunc, 
            Func<TAccumulate, TAccumulate, TAccumulate> combineAccumulatorsFunc, 
            Func<TAccumulate, TResult> resultSelector);
    }
}
#endif
