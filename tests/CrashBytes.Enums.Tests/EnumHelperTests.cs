using System.Diagnostics;
using System.Runtime.Serialization;
using CrashBytes.Enums;

namespace CrashBytes.Enums.Tests;

public enum HttpStatus
{
    [EnumMember(Value = "ok")]
    Ok = 200,
    [EnumMember(Value = "not-found")]
    NotFound = 404,
    InternalServerError = 500
}

public enum WithZero
{
    Zero = 0,
    One = 1,
    Two = 2
}

public enum WithDuplicates
{
    Alpha = 1,
    AliasOfAlpha = 1,
    Beta = 2
}

public enum ByteEnum : byte { A = 1, B = 2, C = 4 }
public enum ShortEnum : short { A = 1, B = 2, C = 4 }
public enum LongEnum : long { A = 1L, B = 2L, C = 4L }

[Flags]
public enum LongFlags : long
{
    None = 0L,
    Low = 1L,
    High = 1L << 33,
    All = Low | High
}

public class EnumHelperTests
{
    [Fact]
    public void GetValues_ReturnsCachedInstance()
    {
        var first = EnumHelper.GetValues<Color>();
        var second = EnumHelper.GetValues<Color>();
        Assert.Same(first, second);
        Assert.Equal(3, first.Count);
    }

    [Fact]
    public void GetNames_ReturnsCachedInstance()
    {
        var first = EnumHelper.GetNames<Color>();
        var second = EnumHelper.GetNames<Color>();
        Assert.Same(first, second);
        Assert.Equal(new[] { "Red", "Green", "Blue" }, first);
    }

    [Fact]
    public void TryParse_KnownName_Succeeds()
    {
        Assert.True(EnumHelper.TryParse<Color>("Green", out var v));
        Assert.Equal(Color.Green, v);
    }

    [Fact]
    public void TryParse_CaseInsensitive_Succeeds()
    {
        Assert.True(EnumHelper.TryParse<Color>("green", out var v));
        Assert.Equal(Color.Green, v);
    }

    [Fact]
    public void TryParse_CaseSensitive_RejectsWrongCase()
    {
        Assert.False(EnumHelper.TryParse<Color>("green", out _, ignoreCase: false));
    }

    [Fact]
    public void TryParse_NumericString_FallsThroughToBcl()
    {
        Assert.True(EnumHelper.TryParse<Color>("1", out var v));
        Assert.Equal(Color.Green, v);
    }

    [Fact]
    public void TryParse_Null_ReturnsFalse()
    {
        Assert.False(EnumHelper.TryParse<Color>(null, out _));
    }

    [Fact]
    public void TryParse_Invalid_ReturnsFalse()
    {
        Assert.False(EnumHelper.TryParse<Color>("Yellow", out _));
    }
}

public class GetEnumMemberValueTests
{
    [Fact]
    public void GetEnumMemberValue_WithAttribute_ReturnsValue()
    {
        Assert.Equal("ok", HttpStatus.Ok.GetEnumMemberValue());
        Assert.Equal("not-found", HttpStatus.NotFound.GetEnumMemberValue());
    }

    [Fact]
    public void GetEnumMemberValue_WithoutAttribute_ReturnsName()
    {
        Assert.Equal("InternalServerError", HttpStatus.InternalServerError.GetEnumMemberValue());
    }

    [Fact]
    public void GetEnumMemberValue_PlainEnum_ReturnsName()
    {
        Assert.Equal("Red", Color.Red.GetEnumMemberValue());
    }
}

public class HasFlagFastTests
{
    [Fact]
    public void HasFlagFast_FlagSet_ReturnsTrue()
    {
        var combined = Permissions.Read | Permissions.Write;
        Assert.True(combined.HasFlagFast(Permissions.Read));
        Assert.True(combined.HasFlagFast(Permissions.Write));
    }

    [Fact]
    public void HasFlagFast_FlagNotSet_ReturnsFalse()
    {
        var combined = Permissions.Read | Permissions.Write;
        Assert.False(combined.HasFlagFast(Permissions.Execute));
    }

    [Fact]
    public void HasFlagFast_AgreesWithBuiltinHasFlag()
    {
        // Smoke test across a few combinations to check semantic parity.
        Permissions[] sets =
        {
            Permissions.None,
            Permissions.Read,
            Permissions.Read | Permissions.Write,
            Permissions.All,
        };
        Permissions[] flags = { Permissions.Read, Permissions.Write, Permissions.Execute, Permissions.None };

        foreach (var s in sets)
        foreach (var f in flags)
        {
            Assert.Equal(s.HasFlag(f), s.HasFlagFast(f));
        }
    }

    [Fact]
    public void HasFlagFast_LongUnderlyingType_ReadsHighBitsCorrectly()
    {
        var combined = LongFlags.Low | LongFlags.High;
        Assert.True(combined.HasFlagFast(LongFlags.High));
        Assert.True(combined.HasFlagFast(LongFlags.Low));
        Assert.False(LongFlags.Low.HasFlagFast(LongFlags.High));
    }

    [Fact]
    public void HasFlagFast_NoBoxingSmoke_ManyIterationsRunsFast()
    {
        // Smoke benchmark: not a strict perf assertion (CI variance is real),
        // just ensure HasFlagFast completes a million iterations in well under a second.
        const int iterations = 1_000_000;
        var v = Permissions.Read | Permissions.Write;
        var sw = Stopwatch.StartNew();
        var count = 0;
        for (int i = 0; i < iterations; i++)
        {
            if (v.HasFlagFast(Permissions.Read)) count++;
        }
        sw.Stop();
        Assert.Equal(iterations, count);
        Assert.True(sw.ElapsedMilliseconds < 2000, $"HasFlagFast took {sw.ElapsedMilliseconds}ms for {iterations} iters");
    }
}

public class CachedExtensionsTests
{
    [Fact]
    public void GetValues_ReturnsCachedInstance()
    {
        var a = EnumExtensions.GetValues<Color>();
        var b = EnumExtensions.GetValues<Color>();
        Assert.Same(a, b);
    }

    [Fact]
    public void GetNames_ReturnsCachedInstance()
    {
        var a = EnumExtensions.GetNames<Color>();
        var b = EnumExtensions.GetNames<Color>();
        Assert.Same(a, b);
    }

    [Fact]
    public void GetDescription_RepeatedCalls_ReturnSameString()
    {
        var first = Priority.Low.GetDescription();
        var second = Priority.Low.GetDescription();
        Assert.Same(first, second);
    }

    [Fact]
    public void GetDisplayName_RepeatedCalls_ReturnSameString()
    {
        var first = Status.InProgress.GetDisplayName();
        var second = Status.InProgress.GetDisplayName();
        Assert.Same(first, second);
    }
}

public class EdgeCaseTests
{
    [Fact]
    public void GetValues_EnumWithExplicitZero_IncludesZero()
    {
        var values = EnumHelper.GetValues<WithZero>();
        Assert.Contains(WithZero.Zero, values);
        Assert.Equal(3, values.Count);
    }

    [Fact]
    public void GetFlags_EnumWithExplicitZero_SkipsZero()
    {
        // GetFlags should not yield the zero member even when it is part of a non-Flags enum.
        var flags = WithZero.One.GetFlags();
        Assert.DoesNotContain(WithZero.Zero, flags);
    }

    [Fact]
    public void GetValues_DuplicateValues_AreReturnedOncePerName()
    {
        // BCL Enum.GetValues returns one entry per declared name (3 names, two share an underlying value).
        var values = EnumHelper.GetValues<WithDuplicates>();
        Assert.Equal(3, values.Count);
        // The two aliases compare equal via the enum equality semantics.
        Assert.Equal(WithDuplicates.Alpha, WithDuplicates.AliasOfAlpha);
    }

    [Fact]
    public void TryParse_DuplicateAlias_ResolvesToCanonicalName()
    {
        Assert.True(EnumHelper.TryParse<WithDuplicates>("Alpha", out var alpha));
        Assert.True(EnumHelper.TryParse<WithDuplicates>("AliasOfAlpha", out var alias));
        Assert.Equal(alpha, alias);
    }

    [Fact]
    public void HasFlagFast_ByteEnum_Works()
    {
        var v = ByteEnum.A | ByteEnum.B;
        Assert.True(v.HasFlagFast(ByteEnum.A));
        Assert.False(v.HasFlagFast(ByteEnum.C));
    }

    [Fact]
    public void HasFlagFast_ShortEnum_Works()
    {
        var v = ShortEnum.A | ShortEnum.B;
        Assert.True(v.HasFlagFast(ShortEnum.A));
        Assert.False(v.HasFlagFast(ShortEnum.C));
    }

    [Fact]
    public void HasFlagFast_LongEnum_Works()
    {
        var v = LongEnum.A | LongEnum.B;
        Assert.True(v.HasFlagFast(LongEnum.A));
        Assert.False(v.HasFlagFast(LongEnum.C));
    }
}
