namespace Riok.Mapperly.Helpers;

public class TupleEqualityComparer<T1, T2>(IEqualityComparer<T1> e1, IEqualityComparer<T2> e2) : IEqualityComparer<ValueTuple<T1, T2>>
{
    public bool Equals((T1, T2) x, (T1, T2) y) => e1.Equals(x.Item1, y.Item1) & e2.Equals(x.Item2, y.Item2);

    public int GetHashCode(ValueTuple<T1, T2> obj) => HashCode.Combine(e1.GetHashCode(obj.Item1), e2.GetHashCode(obj.Item2));
}
