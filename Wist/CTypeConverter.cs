namespace Wist;

public static class CTypeConverter
{
    public static bool Convert(string text, out CType value)
    {
        return Enum.TryParse(text, true, out value);
    }
}