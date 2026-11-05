using GdUnit4;
using NeonWarfare.Scenes.World.Features.Saves;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.Features.Saves;

[TestSuite]
public class SaveFileNameRuleTests
{
    [TestCase]
    public void IsValid_PlainNames_AtTheLimit_NotAscii_Pass()
    {
        string[] fileNames =
        [
            "my save", new string('a', SaveFileNameRule.MaxLength), "Сейв 2.0_final-1", "a..b",
            "console", "com10", "nul1", "lpt", "a.con",
        ];

        AssertThat(fileNames.Where(fileName => !SaveFileNameRule.IsValid(fileName)).ToList()).IsEmpty();
    }

    [TestCase]
    public void IsValid_EmptyOrTooLong_Fails()
    {
        string?[] fileNames = [null, "", new string('a', SaveFileNameRule.MaxLength + 1)];

        AssertThat(fileNames.Where(fileName => SaveFileNameRule.IsValid(fileName!)).ToList()).IsEmpty();
    }

    // Separators and special names of Linux and macOS
    [TestCase]
    public void IsValid_PathsAndUnixSpecialNames_Fail()
    {
        string[] fileNames = [".", "..", "../x", "a/b", "a\\b", "a:b", ".hidden", "._meta", "a\nb", "a\0b"];

        AssertThat(fileNames.Where(SaveFileNameRule.IsValid).ToList()).IsEmpty();
    }

    // Windows reserves them in any case and with any extension, trailing spaces of the base ignored
    [TestCase]
    public void IsValid_WindowsDeviceNames_Fail()
    {
        string[] fileNames =
        [
            "CON", "con", "Prn", "aux", "NUL", "COM0", "com9", "LPT1", "lpt9", "nul.txt", "con.a.b", "aux .x", "CON ",
        ];

        AssertThat(fileNames.Where(SaveFileNameRule.IsValid).ToList()).IsEmpty();
    }
}
