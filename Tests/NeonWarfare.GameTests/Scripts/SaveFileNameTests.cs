using GdUnit4;
using NeonWarfare.Scripts;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Scripts;

[TestSuite]
public class SaveFileNameTests
{
    [TestCase]
    public void IsValid_PlainNames_AtTheLimit_NotAscii_Pass()
    {
        string[] fileNames =
        [
            "my save", new string('a', SaveFileName.MaxLength), "Сейв 2.0_final-1", "a..b",
            "console", "com10", "nul1", "lpt", "a.con",
        ];

        AssertThat(fileNames.Where(fileName => !new SaveFileName(fileName).IsValid).ToList()).IsEmpty();
    }

    [TestCase]
    public void IsValid_EmptyOrTooLong_Fails()
    {
        string?[] fileNames = [null, "", new string('a', SaveFileName.MaxLength + 1)];

        AssertThat(fileNames.Where(fileName => new SaveFileName(fileName!).IsValid).ToList()).IsEmpty();
    }

    // Separators and special names of Linux and macOS
    [TestCase]
    public void IsValid_PathsAndUnixSpecialNames_Fail()
    {
        string[] fileNames = [".", "..", "../x", "a/b", "a\\b", "a:b", ".hidden", "._meta", "a\nb", "a\0b"];

        AssertThat(fileNames.Where(fileName => new SaveFileName(fileName).IsValid).ToList()).IsEmpty();
    }

    // Windows reserves them in any case and with any extension, trailing spaces of the base ignored
    [TestCase]
    public void IsValid_WindowsDeviceNames_Fail()
    {
        string[] fileNames =
        [
            "CON", "con", "Prn", "aux", "NUL", "COM0", "com9", "LPT1", "lpt9", "nul.txt", "con.a.b", "aux .x", "CON ",
        ];

        AssertThat(fileNames.Where(fileName => new SaveFileName(fileName).IsValid).ToList()).IsEmpty();
    }

    [TestCase]
    public void TryParse_Valid_IsTheName()
    {
        AssertThat(SaveFileName.TryParse("my save", out SaveFileName fileName)).IsTrue();
        AssertThat(fileName).IsEqual(new SaveFileName("my save"));
    }

    [TestCase]
    public void TryParse_Invalid_IsDefault()
    {
        string?[] values = [null, "", "a/b", "CON"];

        foreach (string? value in values)
        {
            AssertThat(SaveFileName.TryParse(value!, out SaveFileName fileName))
                .OverrideFailureMessage($"'{value}' parsed").IsFalse();
            AssertThat(fileName).IsEqual(default(SaveFileName));
        }
    }
}
