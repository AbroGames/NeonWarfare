using System.Text.Json;
using System.Text.Json.Serialization;
using GdUnit4;
using KludgeBox.Persistence;
using NeonWarfare.GameTests.KludgeBox.Persistence.Fixtures;
using static GdUnit4.Assertions;
using Color = Godot.Color;
using Vector2 = Godot.Vector2;
using Vector3 = Godot.Vector3;

namespace NeonWarfare.GameTests.KludgeBox.Persistence;

// The real disk, in the engine: the logging of a bad file needs it. A failure is forced by a directory where the
// file wants to be
[TestSuite]
public class JsonFileTests
{
    public record Sample(string Name, int Count);

    public record GodotSample(Color Color, Vector2 Position, Vector3 Size);

    private const string Name = "sample.json";
    private const string CorruptName = Name + JsonFile<Sample>.CorruptSuffix;
    private const string ValidJson = """{"Name":"a","Count":1}""";

    private const string GodotJson =
        """{"Color":"0a141e28","Position":{"X":1.5,"Y":-2.25},"Size":{"X":0,"Y":1,"Z":2}}""";

    private static readonly Sample Default = new("default", 0);
    private static readonly GodotSample GodotValue = new(Color.Color8(10, 20, 30, 40), new(1.5f, -2.25f), new(0, 1, 2));

    private TestUserFolder _folder = null!;
    private JsonFile<Sample> _file = null!;
    private JsonFile<GodotSample> _godotFile = null!;

    [BeforeTest]
    public void SetUp()
    {
        _folder = new TestUserFolder();
        _file = new JsonFile<Sample>(_folder.FileName(Name));
        _godotFile = new JsonFile<GodotSample>(_folder.FileName(Name));
    }

    [AfterTest]
    public void TearDown()
    {
        _folder.Dispose();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Read_Missing_IsMissing()
    {
        AssertThat(_file.Read().Outcome).IsEqual(JsonFileOutcome.Missing);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Read_Valid_IsLoaded()
    {
        _folder.Write(Name, ValidJson);

        AssertThat(_file.Read(IsValid)).IsEqual(new JsonFileRead<Sample>(JsonFileOutcome.Loaded, new Sample("a", 1)));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Read_Empty_IsCorrupt_MovedAside() => AssertCorrupt("");

    [TestCase]
    [RequireGodotRuntime]
    public void Read_Null_IsCorrupt_MovedAside() => AssertCorrupt("null");

    [TestCase]
    [RequireGodotRuntime]
    public void Read_BrokenJson_IsCorrupt_MovedAside() => AssertCorrupt("""{"Name":""");

    [TestCase]
    [RequireGodotRuntime]
    public void Read_MissingRecordField_IsCorrupt_MovedAside() => AssertCorrupt("""{"Name":"a"}""");

    [TestCase]
    [RequireGodotRuntime]
    public void Read_WrongFieldType_IsCorrupt_MovedAside() => AssertCorrupt("""{"Name":"a","Count":"one"}""");

    [TestCase]
    [RequireGodotRuntime]
    public void Read_RejectedByValidation_IsCorrupt_MovedAside() => AssertCorrupt("""{"Name":"","Count":1}""");

    [TestCase]
    [RequireGodotRuntime]
    public void Read_Corrupt_ReplacesAnOlderCorruptCopy()
    {
        _folder.Write(CorruptName, "older");

        AssertCorrupt("null");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Read_Unreadable_IsUnreadable_NothingMoved()
    {
        Directory.CreateDirectory(_folder.FullPath(Name));

        AssertThat(_file.Read(IsValid).Outcome).IsEqual(JsonFileOutcome.Unreadable);

        AssertThat(_folder.Entries()).ContainsExactly(Name);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ReadOrWriteDefault_Loaded_ReturnsTheValue_WritesNothing()
    {
        _folder.Write(Name, ValidJson);

        AssertThat(_file.ReadOrWriteDefault(IsValid, () => Default)).IsEqual(new Sample("a", 1));

        AssertThat(_folder.Read(Name)).IsEqual(ValidJson);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ReadOrWriteDefault_Missing_WritesTheDefault()
    {
        AssertThat(_file.ReadOrWriteDefault(IsValid, () => Default)).IsEqual(Default);

        AssertThat(_file.Read(IsValid).Value).IsEqual(Default);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ReadOrWriteDefault_Corrupt_WritesTheDefault_KeepsTheCorruptCopy()
    {
        _folder.Write(Name, "null");

        AssertThat(_file.ReadOrWriteDefault(IsValid, () => Default)).IsEqual(Default);

        AssertThat(_file.Read(IsValid).Value).IsEqual(Default);
        AssertThat(_folder.Read(CorruptName)).IsEqual("null");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ReadOrWriteDefault_Unreadable_ReturnsTheDefault_WritesNothing()
    {
        _folder.Write(Name, ValidJson);

        // A file opened exclusively cannot be read, but on Unix a rename would still replace it
        using (new FileStream(_folder.FullPath(Name), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            AssertThat(_file.ReadOrWriteDefault(IsValid, () => Default)).IsEqual(Default);
        }

        AssertThat(_folder.Read(Name)).IsEqual(ValidJson);
        AssertThat(_folder.Entries()).ContainsExactly(Name);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Write_RoundTrips()
    {
        AssertThat(_file.Write(new Sample("b", 2))).IsTrue();

        AssertThat(_file.Read(IsValid).Value).IsEqual(new Sample("b", 2));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Write_GodotTypes_InTheirFormat_RoundTrip()
    {
        AssertThat(_godotFile.Write(GodotValue)).IsTrue();

        AssertThat(_folder.Read(Name)).IsEqual(GodotJson);
        AssertThat(_godotFile.Read().Value).IsEqual(GodotValue);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Write_TheCallersConverter_WinsOverTheBuiltInOne()
    {
        var file = new JsonFile<GodotSample>(_folder.FileName(Name),
            new JsonSerializerOptions { Converters = { new FixedColorConverter() } });

        file.Write(GodotValue);

        AssertThat(_folder.Read(Name)).StartsWith("""{"Color":"fixed",""");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Read_BadColor_IsCorrupt_MovedAside() => AssertGodotCorrupt(GodotJson.Replace("0a141e28", "zz"));

    [TestCase]
    [RequireGodotRuntime]
    public void Read_VectorMissingComponent_IsCorrupt_MovedAside() =>
        AssertGodotCorrupt(GodotJson.Replace(""","Z":2""", ""));

    [TestCase]
    [RequireGodotRuntime]
    public void Read_VectorUnknownComponent_IsCorrupt_MovedAside() =>
        AssertGodotCorrupt(GodotJson.Replace(""","Z":2""", ""","Z":2,"W":3"""));

    [TestCase]
    [RequireGodotRuntime]
    public void Read_VectorDuplicateComponent_IsCorrupt_MovedAside() =>
        AssertGodotCorrupt(GodotJson.Replace(""","Z":2""", ""","Z":2,"Z":3"""));

    [TestCase]
    [RequireGodotRuntime]
    public void Write_Fails_ReturnsFalse_KeepsTheFile()
    {
        _folder.Write(Name, ValidJson);
        Directory.CreateDirectory(_folder.FullPath($"{Name}.{Environment.ProcessId}.tmp"));

        AssertThat(_file.Write(new Sample("b", 2))).IsFalse();

        AssertThat(_folder.Read(Name)).IsEqual(ValidJson);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ReadOverDefault_Missing_WritesTheDefault()
    {
        AssertThat(_file.ReadOverDefault(() => Default)).IsEqual(Default);

        AssertThat(_file.Read().Value).IsEqual(Default);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ReadOverDefault_Complete_ReturnsTheValue_WritesNothing()
    {
        _folder.Write(Name, ValidJson);

        AssertThat(_file.ReadOverDefault(() => Default)).IsEqual(new Sample("a", 1));

        AssertThat(_folder.Read(Name)).IsEqual(ValidJson);
        AssertThat(_folder.Entries()).ContainsExactly(Name);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ReadOverDefault_UnknownField_Ignored_WritesNothing()
    {
        const string json = """{"Name":"a","Count":1,"Gone":true}""";
        _folder.Write(Name, json);

        AssertThat(_file.ReadOverDefault(() => Default)).IsEqual(new Sample("a", 1));

        AssertThat(_folder.Read(Name)).IsEqual(json);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ReadOverDefault_MissingField_ItsDefault_WrittenBack_NoCorruptCopy()
    {
        _folder.Write(Name, """{"Name":"a"}""");

        AssertThat(_file.ReadOverDefault(() => Default)).IsEqual(new Sample("a", Default.Count));

        AssertThat(_file.Read().Value).IsEqual(new Sample("a", Default.Count));
        AssertThat(_folder.Entries()).ContainsExactly(Name);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ReadOverDefault_WrongFieldType_ItsDefault_WrittenBack_TheFileCopied() =>
        AssertFieldReplaced("""{"Name":"a","Count":"one"}""", new Sample("a", Default.Count));

    [TestCase]
    [RequireGodotRuntime]
    public void ReadOverDefault_NullWhereTheDefaultIsNot_ItsDefault_WrittenBack_TheFileCopied() =>
        AssertFieldReplaced("""{"Name":null,"Count":1}""", new Sample(Default.Name, 1));

    [TestCase]
    [RequireGodotRuntime]
    public void ReadOverDefault_NullWhereTheDefaultIsNull_Kept()
    {
        const string json = """{"Name":null,"Count":1}""";
        _folder.Write(Name, json);

        AssertThat(_file.ReadOverDefault(() => new Sample(null!, 0))).IsEqual(new Sample(null!, 1));

        AssertThat(_folder.Read(Name)).IsEqual(json);
        AssertThat(_folder.Entries()).ContainsExactly(Name);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ReadOverDefault_DuplicateField_TheFileIsCorrupt() =>
        AssertNotAnObject("""{"Name":"a","Name":"b","Count":1}""");

    [TestCase]
    [RequireGodotRuntime]
    public void ReadOverDefault_Null_TheFileIsCorrupt() => AssertNotAnObject("null");

    [TestCase]
    [RequireGodotRuntime]
    public void ReadOverDefault_Array_TheFileIsCorrupt() => AssertNotAnObject("[1]");

    [TestCase]
    [RequireGodotRuntime]
    public void ReadOverDefault_BrokenJson_TheFileIsCorrupt() => AssertNotAnObject("""{"Name":""");

    [TestCase]
    [RequireGodotRuntime]
    public void ReadOverDefault_Unreadable_ReturnsTheDefault_WritesNothing()
    {
        _folder.Write(Name, ValidJson);

        // A file opened exclusively cannot be read, but on Unix a rename would still replace it
        using (new FileStream(_folder.FullPath(Name), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            AssertThat(_file.ReadOverDefault(() => Default)).IsEqual(Default);
        }

        AssertThat(_folder.Read(Name)).IsEqual(ValidJson);
        AssertThat(_folder.Entries()).ContainsExactly(Name);
    }

    private static bool IsValid(Sample sample) => sample.Name != "";

    private void AssertFieldReplaced(string json, Sample expected)
    {
        _folder.Write(Name, json);

        AssertThat(_file.ReadOverDefault(() => Default)).IsEqual(expected);

        AssertThat(_file.Read().Value).IsEqual(expected);
        AssertThat(_folder.Read(CorruptName)).IsEqual(json);
    }

    private void AssertNotAnObject(string json)
    {
        _folder.Write(Name, json);

        AssertThat(_file.ReadOverDefault(() => Default)).IsEqual(Default);

        AssertThat(_file.Read().Value).IsEqual(Default);
        AssertThat(_folder.Read(CorruptName)).IsEqual(json);
    }

    private void AssertGodotCorrupt(string json)
    {
        _folder.Write(Name, json);

        AssertThat(_godotFile.Read().Outcome).IsEqual(JsonFileOutcome.Corrupt);

        AssertThat(_folder.Read(CorruptName)).IsEqual(json);
    }

    private void AssertCorrupt(string json)
    {
        _folder.Write(Name, json);

        AssertThat(_file.Read(IsValid)).IsEqual(new JsonFileRead<Sample>(JsonFileOutcome.Corrupt, null!));

        AssertThat(_folder.Entries()).ContainsExactly(CorruptName);
        AssertThat(_folder.Read(CorruptName)).IsEqual(json);
    }

    private class FixedColorConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotSupportedException();

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) =>
            writer.WriteStringValue("fixed");
    }
}
