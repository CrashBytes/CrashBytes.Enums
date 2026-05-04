using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace CrashBytes.Enums;

/// <summary>
/// Extension and utility methods for enums. Attribute lookups (Description, Display, EnumMember)
/// and value/name enumerations are cached per enum type to avoid reflection cost on subsequent calls.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Gets the <see cref="DescriptionAttribute"/> value of an enum member, or its name if no
    /// attribute is present. Result is served from a per-type cache.
    /// </summary>
    public static string GetDescription<T>(this T value) where T : struct, Enum
    {
        return EnumCache<T>.Descriptions.TryGetValue(value, out var v) ? v : value.ToString();
    }

    /// <summary>
    /// Gets the <see cref="DisplayAttribute.Name"/> value of an enum member, or its name if no
    /// attribute is present. Result is served from a per-type cache.
    /// </summary>
    public static string GetDisplayName<T>(this T value) where T : struct, Enum
    {
        return EnumCache<T>.DisplayNames.TryGetValue(value, out var v) ? v : value.ToString();
    }

    /// <summary>
    /// Gets the <see cref="EnumMemberAttribute.Value"/> of an enum member, or its name if no
    /// attribute is present. Result is served from a per-type cache.
    /// </summary>
    public static string GetEnumMemberValue<T>(this T value) where T : struct, Enum
    {
        return EnumCache<T>.EnumMemberValues.TryGetValue(value, out var v) ? v : value.ToString();
    }

    /// <summary>
    /// Converts all values of enum <typeparamref name="T"/> into a dictionary of (value, name) pairs.
    /// </summary>
    public static Dictionary<T, string> ToDictionary<T>() where T : struct, Enum
    {
        var values = EnumCache<T>.Values;
        var names = EnumCache<T>.Names;
        var dict = new Dictionary<T, string>(values.Length);
        for (int i = 0; i < values.Length; i++)
        {
            // First-write-wins for duplicate underlying values, matching original semantics.
            if (!dict.ContainsKey(values[i]))
                dict[values[i]] = names[i];
        }
        return dict;
    }

    /// <summary>
    /// Converts all values of enum <typeparamref name="T"/> into a dictionary of (value, description) pairs,
    /// using the <see cref="DescriptionAttribute"/> if present.
    /// </summary>
    public static Dictionary<T, string> ToDescriptionDictionary<T>() where T : struct, Enum
    {
        var values = EnumCache<T>.Values;
        var dict = new Dictionary<T, string>(values.Length);
        foreach (var v in values)
        {
            if (!dict.ContainsKey(v))
                dict[v] = v.GetDescription();
        }
        return dict;
    }

    /// <summary>
    /// Returns all values of enum <typeparamref name="T"/>. Backed by a per-type cache;
    /// the returned array is shared across calls — do not mutate.
    /// </summary>
    public static IReadOnlyList<T> GetValues<T>() where T : struct, Enum =>
        EnumCache<T>.Values;

    /// <summary>
    /// Returns all names of enum <typeparamref name="T"/>. Backed by a per-type cache;
    /// the returned array is shared across calls — do not mutate.
    /// </summary>
    public static IReadOnlyList<string> GetNames<T>() where T : struct, Enum =>
        EnumCache<T>.Names;

    /// <summary>
    /// Parses a string to enum <typeparamref name="T"/>, returning <paramref name="defaultValue"/> if parsing fails.
    /// Case-insensitive by default. Uses <see cref="EnumHelper.TryParse{T}"/>'s cached name lookup.
    /// </summary>
    public static T Parse<T>(string value, T defaultValue = default, bool ignoreCase = true) where T : struct, Enum =>
        EnumHelper.TryParse<T>(value, out var result, ignoreCase) ? result : defaultValue;

    /// <summary>
    /// Tries to parse a string to enum <typeparamref name="T"/>. Case-insensitive by default.
    /// Uses cached name-to-value maps; falls back to <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/>
    /// for numeric or flag-combination inputs.
    /// </summary>
    public static bool TryParse<T>(string value, out T result, bool ignoreCase = true) where T : struct, Enum =>
        EnumHelper.TryParse(value, out result, ignoreCase);

    /// <summary>
    /// Returns <c>true</c> if the value is a defined member of enum <typeparamref name="T"/>.
    /// </summary>
    public static bool IsDefined<T>(this T value) where T : struct, Enum =>
        Enum.IsDefined(typeof(T), value);

    /// <summary>
    /// Tests whether <paramref name="flag"/> is set on <paramref name="value"/> without boxing
    /// either operand. The standard <see cref="Enum.HasFlag(Enum)"/> boxes both operands on every
    /// call; this overload reads the underlying integer directly via <see cref="Unsafe"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasFlagFast<T>(this T value, T flag) where T : struct, Enum
    {
        // Dispatch on the underlying type's size. The JIT eliminates the unreachable branches
        // for any concrete T because Unsafe.SizeOf<T>() is a constant per generic instantiation.
        var size = Unsafe.SizeOf<T>();
        switch (size)
        {
            case 1:
            {
                var f = Unsafe.As<T, byte>(ref flag);
                return (Unsafe.As<T, byte>(ref value) & f) == f;
            }
            case 2:
            {
                var f = Unsafe.As<T, ushort>(ref flag);
                return (Unsafe.As<T, ushort>(ref value) & f) == f;
            }
            case 4:
            {
                var f = Unsafe.As<T, uint>(ref flag);
                return (Unsafe.As<T, uint>(ref value) & f) == f;
            }
            case 8:
            {
                var f = Unsafe.As<T, ulong>(ref flag);
                return (Unsafe.As<T, ulong>(ref value) & f) == f;
            }
            default:
                // Should not be reachable for any standard underlying type.
                return value.HasFlag(flag);
        }
    }

    /// <summary>
    /// For a <see cref="FlagsAttribute"/> enum, returns all individual flag values that are set.
    /// Iterates the cached values array. Uses <see cref="HasFlagFast{T}"/> for each comparison.
    /// </summary>
    public static IReadOnlyList<T> GetFlags<T>(this T value) where T : struct, Enum
    {
        var values = EnumCache<T>.Values;
        var result = new List<T>();
        foreach (var flag in values)
        {
            if (IsZero(flag))
                continue;

            if (value.HasFlagFast(flag))
                result.Add(flag);
        }

        return result;
    }

    /// <summary>
    /// Returns the next value of the enum, wrapping around to the first value after the last.
    /// </summary>
    public static T Next<T>(this T value) where T : struct, Enum
    {
        var values = EnumCache<T>.Values;
        int index = Array.IndexOf(values, value);
        return values[(index + 1) % values.Length];
    }

    /// <summary>
    /// Returns the previous value of the enum, wrapping around to the last value before the first.
    /// </summary>
    public static T Previous<T>(this T value) where T : struct, Enum
    {
        var values = EnumCache<T>.Values;
        int index = Array.IndexOf(values, value);
        return values[(index - 1 + values.Length) % values.Length];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsZero<T>(T value) where T : struct, Enum
    {
        var size = Unsafe.SizeOf<T>();
        return size switch
        {
            1 => Unsafe.As<T, byte>(ref value) == 0,
            2 => Unsafe.As<T, ushort>(ref value) == 0,
            4 => Unsafe.As<T, uint>(ref value) == 0,
            8 => Unsafe.As<T, ulong>(ref value) == 0,
            _ => Convert.ToInt64(value) == 0,
        };
    }
}
