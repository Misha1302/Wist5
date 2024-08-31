namespace Wist;

public static class CTypeExtensions
{
    public static Type ToSharpType(this CType type)
    {
        return type switch
        {
            CType.I8 => typeof(sbyte),
            CType.I16 => typeof(short),
            CType.I32 => typeof(int),
            CType.I64 => typeof(long),
            CType.F32 => typeof(float),
            CType.F64 => typeof(double),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }
}