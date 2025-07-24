

namespace KeysAndValues;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1510 // use ThrowIfNull

/// <summary>
/// Provides methods for difference calculation
/// </summary>
public static class DifferenceCalculation
{
    /// <summary>
    /// Calculates the difference betweeh two store versions
    /// </summary>
    /// <param name="from">The from side of the diff.</param>
    /// <param name="to">The to side of the diff.</param>
    /// <returns>A <see cref="ChangeBatch{TKey, TValue}"/> representing the difference.</returns>
    /// <exception cref="ArgumentNullException">One of the arguments was null.</exception>
    public static ChangeBatch<Mem, Mem> CalculateDifference(StoreVersion from, StoreVersion to)
    {
        if (from is null)
        {
            throw new ArgumentNullException(nameof(from));
        }

        if (to is null)
        {
            throw new ArgumentNullException(nameof(to));
        }

        var diff = CalculateDifference(from.Data, to.Data);

        return new(to.Sequence, diff);
    }

    /// <summary>
    /// Calculates the difference betweeh two <see cref="ImmutableAvlTree"/> instances.
    /// </summary>
    /// <param name="from">The from side of the diff.</param>
    /// <param name="to">The to side of the diff.</param>
    /// <returns>A <see cref="ChangeOperation{TKey, TValue}"/> array representing the difference.</returns>
    /// <exception cref="ArgumentNullException">One of the arguments was null.</exception>
    public static ChangeOperation<TKey, TValue>[] CalculateDifference<TKey, TValue>(ImmutableAvlTree<TKey, TValue> from, ImmutableAvlTree<TKey, TValue> to)
        where TValue : IComparable<TValue>
        where TKey : IComparable<TKey>
    {
        if (from is null)
        {
            throw new ArgumentNullException(nameof(from));
        }

        if (to is null)
        {
            throw new ArgumentNullException(nameof(to));
        }

        List<ChangeOperation<TKey, TValue>> ops = [];

        using var fenum = from.GetEnumerator();
        using var tenum = to.GetEnumerator();

        bool mustAdvancef = true;
        bool mustAdvancet = true;
        bool hasf = false;
        bool hast = false;

        for (; ; )
        {
            if (mustAdvancef)
            {
                hasf = fenum.MoveNext();
            }
            if (mustAdvancet)
            {
                hast = tenum.MoveNext();
            }

            if (!hasf && !hast)
            {
                break;
            }

            if (!hast)
            {
                ops.Add(new ChangeOperation<TKey, TValue>
                {
                    Type = ChangeOperationType.Delete,
                    Key = fenum.Current.Key
                });
                mustAdvancef = true;
                mustAdvancet = false;
                continue;
            }

            if (!hasf)
            {
                ops.Add(new ChangeOperation<TKey, TValue>
                {
                    Type = ChangeOperationType.Add,
                    Key = tenum.Current.Key,
                    Value = tenum.Current.Value
                });
                mustAdvancef = false;
                mustAdvancet = true;
                continue;
            }

            var keyComparison = fenum.Current.Key.CompareTo(tenum.Current.Key);
            if (keyComparison == 0)
            {
                mustAdvancef = true;
                mustAdvancet = true;

                var valueComparison = fenum.Current.Value.CompareTo(tenum.Current.Value);
                if (valueComparison == 0)
                {
                    continue;
                }

                ops.Add(new ChangeOperation<TKey, TValue>
                {
                    Type = ChangeOperationType.Set,
                    Key = tenum.Current.Key,
                    Value = tenum.Current.Value
                });
                continue;
            }

            if (keyComparison < 0)
            {
                // from precedes to
                ops.Add(new ChangeOperation<TKey, TValue>
                {
                    Type = ChangeOperationType.Delete,
                    Key = fenum.Current.Key
                });

                mustAdvancef = true;
                mustAdvancet = false;
                continue;
            }

            if (keyComparison > 0)
            {
                // from comes after to
                ops.Add(new ChangeOperation<TKey, TValue>
                {
                    Type = ChangeOperationType.Add,
                    Key = tenum.Current.Key,
                    Value = tenum.Current.Value
                });

                mustAdvancef = false;
                mustAdvancet = true;
                continue;
            }

            Debug.Assert(false, "This should not be reachable");
        }

        return [.. ops];
    }
}
