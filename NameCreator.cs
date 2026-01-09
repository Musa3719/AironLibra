using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class NameCreator
{
    public enum CultureTypeForName
    {
        EastSteppe, WestSteppe, Desert, Empire, Merchant, Artist, Snow
    }

    [System.Serializable]
    public class NameSyllableProfile
    {
        public string[] Start;
        public string[] Middle;
        public string[] End;
    }
    public static Dictionary<(CultureTypeForName, bool), NameSyllableProfile> _Profiles;

    public static string GetName(CultureTypeForName cultureType, bool isMale)
    {
        if (_Profiles == null) InitProfiles();
        string newName = "";
        newName += _Profiles[(cultureType, isMale)].Start.Pick();
        int random = Random.Range(0, 100);
        int middleCount = random > 96 ? 3 : (random > 85 ? 2 : (random > 30 ? 1 : 0));
        for (int i = 0; i < middleCount; i++)
        {
            newName += _Profiles[(cultureType, isMale)].Middle.Pick();
        }
        newName += _Profiles[(cultureType, isMale)].End.Pick();
        return newName;
    }
    private static string Pick(this string[] array)
    {
        return array[Random.Range(0, array.Length)];
    }
    private static void InitProfiles()
    {
        _Profiles = new Dictionary<(CultureTypeForName, bool), NameSyllableProfile>();

        _Profiles.Add((CultureTypeForName.EastSteppe, true), new NameSyllableProfile
        {
            Start = new[] { "Al", "Er", "Ka", "Tu", "Sa", "Me", "Ha", "Mu", "Ce", "Ya", "Or", "Il", "Bar", "Kor", "Tan", "Dor", "Ul", "Bor", "Tar" },
            Middle = new[] { "an", "ar", "en", "in", "ur", "at", "em", "il", "ay", "ka", "ka", "tu", "or", "ar", "ar", "or", "un", "el", "ir" },
            End = new[] { "n", "r", "k", "han", "tur", "tun", "kut", "dem", "mar", "dar", "gan" }
        });
        _Profiles.Add((CultureTypeForName.EastSteppe, false), new NameSyllableProfile
        {
            Start = new[] { "Ay", "El", "Su", "Na", "La", "Be", "Ne", "Ya", "Ze", "Ýl", "Al", "Ka", "Mi", "Ta", "Lu", "Sa", "Re" },
            Middle = new[] { "la", "ne", "ra", "se", "li", "da", "ya", "su", "ce", "mi", "ka", "na", "ra", "li", "ta", "sa", "ma", "la" },
            End = new[] { "a", "e", "i", "in", "su", "ya", "ra", "na", "ka", "la", "sa" }
        });
        _Profiles.Add((CultureTypeForName.Desert, true), new NameSyllableProfile
        {
            Start = new[] { "Ha", "Sa", "Ka", "Ra", "Ta", "Fa", "Ma", "Ba", "Nu", "Li", "Az", "Am", "Da", "Al", "Ar", "Gh", "Zar", "Il", "Om", "Re", "Si", "Or" },
            Middle = new[] { "li", "ma", "na", "ra", "ar", "ul", "il", "ah", "em", "ir", "an", "el", "ar", "im", "on", "ur", "al", "as", "esh", "in" },
            End = new[] { "n", "r", "m", "ah", "im", "ul", "ar", "an", "as", "ir", "em", "esh" }
        });
        _Profiles.Add((CultureTypeForName.Desert, false), new NameSyllableProfile
        {
            Start = new[] { "Na", "Sa", "Ra", "La", "Fa", "Ma", "Ya", "Al", "Az", "Il", "Zi", "Am", "Le", "Ha", "Li", "Ar", "Sh", "Ti", "No" },
            Middle = new[] { "li", "ya", "na", "ra", "ma", "la", "sa", "da", "ah", "em", "il", "ar", "un", "el", "as", "im", "esh", "ur" },
            End = new[] { "a", "ah", "ya", "ra", "na", "ia", "in", "is", "el", "esh" }
        });
        _Profiles.Add((CultureTypeForName.Empire, true), new NameSyllableProfile
        {
            Start = new[] { "Lu", "Ca", "Ma", "Ti", "Se", "Vi", "Ju", "An", "Ga", "Cl", "Do", "Fa", "Pa", "Ro", "Qu" },
            Middle = new[] { "ri", "an", "us", "ar", "en", "or", "ic", "im", "al", "ar", "on", "el", "in", "ar", "um" },
            End = new[] { "us", "or", "an", "ar", "ius", "en", "em", "o", "el", "im" }
        });
        _Profiles.Add((CultureTypeForName.Empire, false), new NameSyllableProfile
        {
            Start = new[] { "Lu", "Ma", "Vi", "An", "Se", "Cl", "Fa", "Ro", "Ju", "Pa", "Is", "Be", "Da", "El", "La" },
            Middle = new[] { "ra", "na", "ia", "a", "el", "in", "is", "um", "e", "on", "or", "al", "ar", "im", "en" },
            End = new[] { "a", "ia", "ina", "ella", "is", "um", "on", "e", "el", "ine" }
        });
        _Profiles.Add((CultureTypeForName.Merchant, true), new NameSyllableProfile
        {
            Start = new[] { "Ed", "Al", "Wil", "Har", "Ge", "Jo", "Be", "Ri", "Th", "Sa", "Le", "Do", "Ca", "Ha", "Ma" },
            Middle = new[] { "win", "ric", "ard", "el", "an", "on", "or", "et", "is", "en", "in", "al", "er", "un", "ic" },
            End = new[] { "ton", "ley", "son", "man", "er", "el", "an", "on", "is", "ett" }
        });
        _Profiles.Add((CultureTypeForName.Merchant, false), new NameSyllableProfile
        {
            Start = new[] { "El", "Ma", "Jo", "Be", "Li", "Sa", "Ha", "Cl", "An", "Vi", "Le", "Da", "Ra", "Na", "Is" },
            Middle = new[] { "a", "la", "na", "ra", "is", "en", "et", "el", "ie", "in", "or", "un", "al", "er", "on" },
            End = new[] { "a", "elle", "ette", "ina", "is", "on", "ley", "ine", "et", "an" }
        });
        _Profiles.Add((CultureTypeForName.Artist, true), new NameSyllableProfile
        {
            Start = new[] { "Al", "Be", "Lu", "Vi", "Ro", "Jo", "Ed", "Le", "Th", "Cl", "Ar", "Ma", "Pa", "Ge", "Ch" },
            Middle = new[] { "an", "el", "ar", "ien", "eau", "on", "ou", "re", "il", "et", "al", "is", "or", "im", "un" },
            End = new[] { "e", "en", "el", "on", "ier", "ot", "is", "ac", "in", "ard" }
        });
        _Profiles.Add((CultureTypeForName.Artist, false), new NameSyllableProfile
        {
            Start = new[] { "El", "Ma", "An", "Cl", "Is", "Lu", "Vi", "Re", "Je", "Sa", "Au", "Be", "Ce", "Da", "La" },
            Middle = new[] { "la", "le", "ne", "ra", "ie", "ou", "in", "elle", "ette", "an", "is", "or", "el", "ai", "on" },
            End = new[] { "a", "e", "ie", "elle", "ette", "ine", "on", "in", "anne", "ette" }
        });

        _Profiles.Add((CultureTypeForName.WestSteppe, true), new NameSyllableProfile
        {
            Start = new[] { "Ja", "Mi", "Ty", "Br", "Ch", "Da", "Jo", "Ke", "Za", "El", "Ra", "Lo", "Bo", "Ka", "Ti", "Al", "Ro", "No", "Sa", "col" },
            Middle = new[] { "son", "li", "an", "er", "on", "ar", "el", "ic", "ar", "in", "or", "um", "as", "et", "al", "ar", "un", "ol", "ta" },
            End = new[] { "n", "r", "s", "on", "er", "y", "el", "um", "as", "in", "or" }
        });
        _Profiles.Add((CultureTypeForName.WestSteppe, false), new NameSyllableProfile
        {
            Start = new[] { "Em", "Ol", "Li", "Ch", "Ma", "Ha", "Sa", "El", "Be", "Jo", "Ka", "Vi", "Ta", "Ra", "Le", "Na", "Mi" },
            Middle = new[] { "li", "na", "ra", "lo", "ma", "be", "ta", "sa", "la", "ra", "mi", "ka", "la", "ne", "el", "ra", "an", "ia" },
            End = new[] { "a", "y", "ie", "lyn", "elle", "ah", "a", "in", "ra", "na", "el" }
        });
        _Profiles.Add((CultureTypeForName.Snow, true), new NameSyllableProfile
        {
            Start = new[] { "Bj", "Er", "Ha", "Si", "Ol", "Le", "Ka", "Tor", "Ul", "Ar", "Ing", "Fen", "Gor", "Vid", "Rik", "Sten", "Har", "Dag", "Bal" },
            Middle = new[] { "ar", "or", "en", "il", "un", "rik", "vald", "in", "ar", "ur", "el", "on", "an", "id", "ot", "as", "im", "el", "un" },
            End = new[] { "son", "sen", "r", "d", "k", "vik", "helm", "tor", "ik", "ar", "d" }
        });
        _Profiles.Add((CultureTypeForName.Snow, false), new NameSyllableProfile
        {
            Start = new[] { "Fre", "In", "Astr", "El", "Liv", "Si", "Ka", "Al", "Gun", "Rag", "Siv", "Yr", "Thy", "Dag", "Eir", "Hild", "Solve", "An", "Is" },
            Middle = new[] { "da", "vi", "ra", "li", "sa", "ma", "un", "el", "in", "a", "or", "ur", "is", "ja", "yan", "en" },
            End = new[] { "a", "e", "i", "dis", "hild", "borg", "un", "iel", "th", "a", "la" }
        });

    }
}
