using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace CrashBytes.Enums;

/// <summary>
/// Internal reflection cache for enum metadata. Resolves attribute values once per enum type
/// and serves all subsequent lookups from a <see cref="ConcurrentDictionary{TKey, TValue}"/>.
/// </summary>
internal static class EnumCache<T> where T : struct, Enum
{
    public static readonly T[] Values = (T[])Enum.GetValues(typeof(T));
    public static readonly string[] Names = Enum.GetNames(typeof(T));

    public static readonly IReadOnlyDictionary<T, string> Descriptions = BuildAttributeMap(field =>
        field.GetCustomAttribute<DescriptionAttribute>()?.Description);

    public static readonly IReadOnlyDictionary<T, string> DisplayNames = BuildAttributeMap(field =>
        field.GetCustomAttribute<DisplayAttribute>()?.Name);

    public static readonly IReadOnlyDictionary<T, string> EnumMemberValues = BuildAttributeMap(field =>
        field.GetCustomAttribute<EnumMemberAttribute>()?.Value);

    public static readonly Dictionary<string, T> NameToValueIgnoreCase = BuildNameMap(ignoreCase: true);
    public static readonly Dictionary<string, T> NameToValueCaseSensitive = BuildNameMap(ignoreCase: false);

    private static Dictionary<T, string> BuildAttributeMap(Func<FieldInfo, string?> resolver)
    {
        var map = new Dictionary<T, string>(Values.Length);
        var type = typeof(T);
        foreach (var value in Values)
        {
            var name = value.ToString();
            var field = type.GetField(name);
            string? attrValue = null;
            if (field is not null)
            {
                attrValue = resolver(field);
            }
            // Last-write-wins for duplicate underlying values; first occurrence wins via TryAdd.
            if (!map.ContainsKey(value))
            {
                map[value] = attrValue ?? name;
            }
        }
        return map;
    }

    private static Dictionary<string, T> BuildNameMap(bool ignoreCase)
    {
        var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var map = new Dictionary<string, T>(Names.Length, comparer);
        for (int i = 0; i < Names.Length; i++)
        {
            // Duplicate names are not possible in C# enums; safe to use indexer.
            map[Names[i]] = Values[i];
        }
        return map;
    }
}

/// <summary>
/// Process-wide cache index of underlying integral sizes for enum types, used by <see cref="EnumExtensions.HasFlagFast{T}"/>.
/// </summary>
internal static class UnderlyingTypeCache
{
    public static readonly ConcurrentDictionary<Type, TypeCode> Codes = new();

    public static TypeCode GetCode(Type enumType)
    {
        return Codes.GetOrAdd(enumType, static t => Type.GetTypeCode(Enum.GetUnderlyingType(t)));
    }
}
