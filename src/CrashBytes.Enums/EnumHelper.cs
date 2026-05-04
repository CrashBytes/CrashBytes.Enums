namespace CrashBytes.Enums;

/// <summary>
/// Static helpers that operate on enum types without an instance value.
/// All results are precomputed and cached per enum type via <see cref="EnumCache{T}"/>.
/// </summary>
public static class EnumHelper
{
    /// <summary>
    /// Returns all values of enum <typeparamref name="T"/>. The returned array is the
    /// cached instance shared across calls; do not mutate.
    /// </summary>
    public static IReadOnlyList<T> GetValues<T>() where T : struct, Enum
        => EnumCache<T>.Values;

    /// <summary>
    /// Returns all names of enum <typeparamref name="T"/>. The returned array is the
    /// cached instance shared across calls; do not mutate.
    /// </summary>
    public static IReadOnlyList<string> GetNames<T>() where T : struct, Enum
        => EnumCache<T>.Names;

    /// <summary>
    /// Tries to parse <paramref name="value"/> to enum <typeparamref name="T"/> using the cached
    /// name-to-value map. Avoids the per-call reflection cost of <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/>.
    /// </summary>
    /// <param name="value">The string to parse. May be a member name or its underlying numeric representation.</param>
    /// <param name="result">The parsed enum value, or <c>default</c> if parsing fails.</param>
    /// <param name="ignoreCase">If <c>true</c>, member name comparison is case-insensitive.</param>
    public static bool TryParse<T>(string? value, out T result, bool ignoreCase = true) where T : struct, Enum
    {
        if (value is null)
        {
            result = default;
            return false;
        }

        var map = ignoreCase ? EnumCache<T>.NameToValueIgnoreCase : EnumCache<T>.NameToValueCaseSensitive;
        if (map.TryGetValue(value, out result))
            return true;

        // Fall through to BCL parser to honor numeric strings ("1", "2") and comma-separated [Flags] inputs.
        return Enum.TryParse(value, ignoreCase, out result);
    }
}
