using System.Security.Cryptography;

namespace GodotTemplate.Scripts.Service.Settings;

public class UidGenerator
{
    private const string DefaultAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private readonly string _alphabet;

    public UidGenerator(string alphabet = DefaultAlphabet)
    {
        _alphabet = alphabet;
    }

    public string Generate()
    {
        return RandomNumberGenerator.GetString(_alphabet, 10) +
               "-" + 
               RandomNumberGenerator.GetString(_alphabet, 10);
    }
}