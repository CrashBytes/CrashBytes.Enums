using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CrashBytes.Enums.Tests;

public enum Color { Red, Green, Blue }

public enum Priority
{
    [Description("Low priority")]
    Low,
    [Description("Medium priority")]
    Medium,
    [Description("High priority")]
    High
}

public enum Status
{
    [Display(Name = "Not Started")]
    NotStarted,
    [Display(Name = "In Progress")]
    InProgress,
    [Display(Name = "Done")]
    Completed
}

[Flags]
public enum Permissions
{
    None = 0,
    Read = 1,
    Write = 2,
    Execute = 4,
    All = Read | Write | Execute
}

public class GetDescriptionTests
{
    [Fact]
    public void GetDescription_WithAttribute_ReturnsDescription()
    {
        Assert.Equal("Low priority", Priority.Low.GetDescription());
        Assert.Equal("High priority", Priority.High.GetDescription());
    }

    [Fact]
    public void GetDescription_WithoutAttribute_ReturnsName()
    {
        Assert.Equal("Red", Color.Red.GetDescription());
    }
}

public class GetDisplayNameTests
{
    [Fact]
    public void GetDisplayName_WithAttribute_ReturnsDisplayName()
    {
        Assert.Equal("In Progress", Status.InProgress.GetDisplayName());
        Assert.Equal("Not Started", Status.NotStarted.GetDisplayName());
    }

    [Fact]
    public void GetDisplayName_WithoutAttribute_ReturnsName()
    {
        Assert.Equal("Red", Color.Red.GetDisplayName());
    }
}

public class ToDictionaryTests
{
    [Fact]
    public void ToDictionary_ReturnsAllValues()
    {
        var dict = EnumExtensions.ToDictionary<Color>();
        Assert.Equal(3, dict.Count);
        Assert.Equal("Red", dict[Color.Red]);
        Assert.Equal("Green", dict[Color.Green]);
        Assert.Equal("Blue", dict[Color.Blue]);
    }
}

public class ToDescriptionDictionaryTests
{
    [Fact]
    public void ToDescriptionDictionary_ReturnsDescriptions()
    {
        var dict = EnumExtensions.ToDescriptionDictionary<Priority>();
        Assert.Equal("Low priority", dict[Priority.Low]);
        Assert.Equal("High priority", dict[Priority.High]);
    }

    [Fact]
    public void ToDescriptionDictionary_NoAttributes_ReturnsNames()
    {
        var dict = EnumExtensions.ToDescriptionDictionary<Color>();
        Assert.Equal("Red", dict[Color.Red]);
    }
}

public class GetValuesTests
{
    [Fact]
    public void GetValues_ReturnsAllValues()
    {
        var values = EnumExtensions.GetValues<Color>();
        Assert.Equal(3, values.Count);
        Assert.Contains(Color.Red, values);
        Assert.Contains(Color.Green, values);
        Assert.Contains(Color.Blue, values);
    }
}

public class GetNamesTests
{
    [Fact]
    public void GetNames_ReturnsAllNames()
    {
        var names = EnumExtensions.GetNames<Color>();
        Assert.Equal(3, names.Count);
        Assert.Contains("Red", names);
        Assert.Contains("Green", names);
        Assert.Contains("Blue", names);
    }
}

public class ParseTests
{
    [Fact]
    public void Parse_ValidValue_ReturnsEnum()
    {
        Assert.Equal(Color.Red, EnumExtensions.Parse<Color>("Red"));
    }

    [Fact]
    public void Parse_CaseInsensitive_ReturnsEnum()
    {
        Assert.Equal(Color.Red, EnumExtensions.Parse<Color>("red"));
    }

    [Fact]
    public void Parse_InvalidValue_ReturnsDefault()
    {
        Assert.Equal(Color.Red, EnumExtensions.Parse("invalid", Color.Red));
    }

    [Fact]
    public void Parse_CaseSensitive_InvalidCase_ReturnsDefault()
    {
        Assert.Equal(Color.Blue, EnumExtensions.Parse("red", Color.Blue, ignoreCase: false));
    }
}

public class TryParseTests
{
    [Fact]
    public void TryParse_ValidValue_ReturnsTrue()
    {
        Assert.True(EnumExtensions.TryParse<Color>("Green", out var result));
        Assert.Equal(Color.Green, result);
    }

    [Fact]
    public void TryParse_InvalidValue_ReturnsFalse()
    {
        Assert.False(EnumExtensions.TryParse<Color>("invalid", out _));
    }
}

public class IsDefinedTests
{
    [Fact]
    public void IsDefined_ValidValue_ReturnsTrue()
    {
        Assert.True(Color.Red.IsDefined());
    }

    [Fact]
    public void IsDefined_InvalidValue_ReturnsFalse()
    {
        Assert.False(((Color)99).IsDefined());
    }
}

public class GetFlagsTests
{
    [Fact]
    public void GetFlags_CombinedValue_ReturnsIndividualFlags()
    {
        var flags = (Permissions.Read | Permissions.Write).GetFlags();
        Assert.Equal(2, flags.Count);
        Assert.Contains(Permissions.Read, flags);
        Assert.Contains(Permissions.Write, flags);
    }

    [Fact]
    public void GetFlags_None_ReturnsEmpty()
    {
        Assert.Empty(Permissions.None.GetFlags());
    }

    [Fact]
    public void GetFlags_AllFlag_ReturnsAllIndividualPlusAll()
    {
        var flags = Permissions.All.GetFlags();
        Assert.Contains(Permissions.Read, flags);
        Assert.Contains(Permissions.Write, flags);
        Assert.Contains(Permissions.Execute, flags);
    }

    [Fact]
    public void GetFlags_SingleFlag_ReturnsSingle()
    {
        var flags = Permissions.Read.GetFlags();
        Assert.Single(flags);
        Assert.Equal(Permissions.Read, flags[0]);
    }
}

public class NextTests
{
    [Fact]
    public void Next_ReturnsNextValue()
    {
        Assert.Equal(Color.Green, Color.Red.Next());
    }

    [Fact]
    public void Next_LastValue_WrapsToFirst()
    {
        Assert.Equal(Color.Red, Color.Blue.Next());
    }
}

public class PreviousTests
{
    [Fact]
    public void Previous_ReturnsPreviousValue()
    {
        Assert.Equal(Color.Red, Color.Green.Previous());
    }

    [Fact]
    public void Previous_FirstValue_WrapsToLast()
    {
        Assert.Equal(Color.Blue, Color.Red.Previous());
    }
}
