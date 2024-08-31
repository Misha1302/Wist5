namespace Wist;

public static class ListObjectExtensions
{
    public static T To<T>(this List<object> obj)
    {
        return (T)obj[0];
    }
}