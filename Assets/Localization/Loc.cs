using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Chinese is the source language. English text is looked up by the Chinese string,
// ignoring whitespace so line breaks and indentation in the originals do not matter.
public static class Loc
{
    const string PrefKey = "language";

    public static bool English { get; private set; }
    public static event Action Changed;

    static Dictionary<string, string> zhToEn;

    public static void Init()
    {
        if (zhToEn != null) return;
        zhToEn = new Dictionary<string, string>();
        foreach (var (zh, en) in LocStrings.Pairs)
            zhToEn[Normalize(zh)] = en;

        bool systemChinese = Application.systemLanguage == SystemLanguage.Chinese
            || Application.systemLanguage == SystemLanguage.ChineseSimplified
            || Application.systemLanguage == SystemLanguage.ChineseTraditional;
        English = PlayerPrefs.GetInt(PrefKey, systemChinese ? 0 : 1) == 1;
    }

    public static void Toggle()
    {
        English = !English;
        PlayerPrefs.SetInt(PrefKey, English ? 1 : 0);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static bool Has(string zh) => !string.IsNullOrEmpty(zh) && zhToEn.ContainsKey(Normalize(zh));

    // Returns the English text when English is active, otherwise the Chinese input unchanged.
    public static string T(string zh) => English ? En(zh) : zh;

    // The English translation regardless of the active language, or the input if none exists.
    public static string En(string zh)
    {
        Init();
        if (string.IsNullOrEmpty(zh)) return zh;
        return zhToEn.TryGetValue(Normalize(zh), out var en) ? en : zh;
    }

    static string Normalize(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
            if (!char.IsWhiteSpace(c)) sb.Append(c);
        return sb.ToString();
    }
}
