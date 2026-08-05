namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// Reading a file as bytes and as lines, in the one way the whole suite agrees on. Every format helper
/// here splits on LF and drops a trailing CR, so a file written with CRLF is read the same as the rest.
/// The CR itself is nobody else's business: <c>FileEncodingTests</c> is the one check about the bytes,
/// and it reads them directly.
/// </summary>
public static class TextFile
{
    /// <summary>
    /// The lines of a file, split on LF, each without its trailing CR. The array has one entry per LF
    /// plus the tail, so index <c>i</c> is line <c>i + 1</c> as an editor counts them.
    /// </summary>
    public static string[] ReadLines(string path) => SplitLines(File.ReadAllText(path));

    /// <summary>The same split applied to text already in hand.</summary>
    public static string[] SplitLines(string text)
    {
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].TrimEnd('\r');
        }

        return lines;
    }

    /// <summary>
    /// True when the file starts with a UTF-8 BOM. It is invisible in an editor, and anything reading
    /// the file as text without expecting one gets a stray character glued to the front of line 1.
    /// </summary>
    public static bool HasBom(byte[] bytes) =>
        bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;

    /// <summary>The bytes without the BOM, if there was one.</summary>
    public static byte[] StripBom(byte[] bytes) => HasBom(bytes) ? bytes[3..] : bytes;

    /// <summary>
    /// Lines the way an editor shows them: a trailing newline ends the last line, it does not open a
    /// new one. Empty text is zero lines rather than one.
    /// </summary>
    public static int LineCount(string text)
    {
        if (text.Length == 0)
        {
            return 0;
        }

        int count = text.Count(character => character == '\n');
        return text.EndsWith('\n') ? count : count + 1;
    }
}
