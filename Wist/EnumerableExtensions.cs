namespace Wist;

public static class EnumerableExtensions
{
    public static IEnumerable<T> Reversed<T>(this IEnumerable<T> input)
    {
        return new Stack<T>(input);
    }
}