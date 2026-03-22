using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CrashBytes.Enums;

/// <summary>
/// Extension and utility methods for enums.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Gets the <see cref="DescriptionAttribute"/> value of an enum member, or its name if no attribute is present.
    /// </summary>
    public static string GetDescription<T>(this T value) where T : struct, Enum
    {
        var member = typeof(T).GetField(value.ToString());
        if (member is null) return value.ToString();

        var attr = member.GetCustomAttribute<DescriptionAttribute>();
        return attr?.Description ?? value.ToString();
    }

    /// <summary>
    /// Gets the <see cref="DisplayAttribute.Name"/> value of an enum member, or its name if no attribute is present.
    /// </summary>
    public static string GetDisplayName<T>(this T value) where T : struct, Enum
    {
        var member = typeof(T).GetField(value.ToString());
        if (member is null) return value.ToString();

        var attr = member.GetCustomAttribute<DisplayAttribute>();
        return attr?.Name ?? value.ToString();
    }

    /// <summary>
    /// Converts all values of enum <typeparamref name="T"/> into a dictionary of (value, name) pairs.
    /// </summary>
    public static Dictionary<T, string> ToDictionary<T>() where T : struct, Enum
    {
        var values = Enum.GetValues(typeof(T)).Cast<T>();
        return values.ToDictionary(v => v, v => v.ToString());
    }

    /// <summary>
    /// Converts all values of enum <typeparamref name="T"/> into a dictionary of (value, description) pairs,
    /// using the <see cref="DescriptionAttribute"/> if present.
    /// </summary>
    public static Dictionary<T, string> ToDescriptionDictionary<T>() where T : struct, Enum
    {
        var values = Enum.GetValues(typeof(T)).Cast<T>();
        return values.ToDictionary(v => v, v => v.GetDescription());
    }

    /// <summary>
    /// Returns all values of enum <typeparamref name="T"/>.
    /// </summary>
    public static IReadOnlyList<T> GetValues<T>() where T : struct, Enum =>
        Enum.GetValues(typeof(T)).Cast<T>().ToArray();

    /// <summary>
    /// Returns all names of enum <typeparamref name="T"/>.
    /// </summary>
    public static IReadOnlyList<string> GetNames<T>() where T : struct, Enum =>
        Enum.GetNames(typeof(T));

    /// <summary>
    /// Parses a string to enum <typeparamref name="T"/>, returning <paramref name="defaultValue"/> if parsing fails.
    /// Case-insensitive by default.
    /// </summary>
    public static T Parse<T>(string value, T defaultValue = default, bool ignoreCase = true) where T : struct, Enum =>
        Enum.TryParse<T>(value, ignoreCase, out var result) ? result : defaultValue;

    /// <summary>
    /// Tries to parse a string to enum <typeparamref name="T"/>. Case-insensitive by default.
    /// </summary>
    public static bool TryParse<T>(string value, out T result, bool ignoreCase = true) where T : struct, Enum =>
        Enum.TryParse(value, ignoreCase, out result);

    /// <summary>
    /// Returns <c>true</c> if the value is a defined member of enum <typeparamref name="T"/>.
    /// </summary>
    public static bool IsDefined<T>(this T value) where T : struct, Enum =>
        Enum.IsDefined(typeof(T), value);

    /// <summary>
    /// For a <see cref="FlagsAttribute"/> enum, returns all individual flag values that are set.
    /// </summary>
    public static IReadOnlyList<T> GetFlags<T>(this T value) where T : struct, Enum
    {
        var result = new List<T>();
        foreach (T flag in Enum.GetValues(typeof(T)))
        {
            if (Convert.ToInt64(flag) == 0)
                continue;

            if (value.HasFlag(flag))
                result.Add(flag);
        }

        return result;
    }

    /// <summary>
    /// Returns the next value of the enum, wrapping around to the first value after the last.
    /// </summary>
    public static T Next<T>(this T value) where T : struct, Enum
    {
        var values = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
        int index = Array.IndexOf(values, value);
        return values[(index + 1) % values.Length];
    }

    /// <summary>
    /// Returns the previous value of the enum, wrapping around to the last value before the first.
    /// </summary>
    public static T Previous<T>(this T value) where T : struct, Enum
    {
        var values = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
        int index = Array.IndexOf(values, value);
        return values[(index - 1 + values.Length) % values.Length];
    }
}
