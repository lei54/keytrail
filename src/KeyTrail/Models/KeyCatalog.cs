namespace KeyTrail.Models;

public enum KeyGroup
{
    Modifier,
    Letters,
    Digits,
    Punctuation,
    Editing,
    Navigation,
    Function,
    Other,
}

public static class KeyCatalog
{
    private const int VkBack = 0x08;
    private const int VkTab = 0x09;
    private const int VkReturn = 0x0D;
    private const int VkShift = 0x10;
    private const int VkControl = 0x11;
    private const int VkMenu = 0x12;
    private const int VkPause = 0x13;
    private const int VkCapital = 0x14;
    private const int VkEscape = 0x1B;
    private const int VkSpace = 0x20;
    private const int VkPrintScreen = 0x2C;
    private const int VkScrollLock = 0x91;
    private const int VkPrior = 0x21;
    private const int VkNext = 0x22;
    private const int VkEnd = 0x23;
    private const int VkHome = 0x24;
    private const int VkLeft = 0x25;
    private const int VkUp = 0x26;
    private const int VkRight = 0x27;
    private const int VkDown = 0x28;
    private const int VkInsert = 0x2D;
    private const int VkDelete = 0x2E;
    private const int VkLWin = 0x5B;
    private const int VkRWin = 0x5C;
    private const int VkApps = 0x5D;
    private const int VkNumpad0 = 0x60;
    private const int VkNumpad9 = 0x69;

    private static readonly Dictionary<int, string> Names = BuildNames();
    private static readonly Dictionary<int, KeyGroup> Groups = BuildGroups();

    public static string Name(int vk)
    {
        if (Names.TryGetValue(vk, out string? name))
        {
            return name;
        }

        if (vk is >= 0x41 and <= 0x5A)
        {
            return ((char)vk).ToString();
        }

        if (vk is >= 0x30 and <= 0x39)
        {
            return ((char)vk).ToString();
        }

        return $"VK{vk:X2}";
    }

    public static KeyGroup Group(int vk) =>
        Groups.TryGetValue(vk, out KeyGroup g) ? g : KeyGroup.Other;

    public static bool IsModifier(int vk) =>
        vk is VkShift or VkControl or VkMenu or VkLWin or VkRWin
        || (vk is >= 0xA0 and <= 0xA5);

    public static string ShortcutText(int modifiersMask, int vk)
    {
        var parts = new List<string>();
        if ((modifiersMask & 1) != 0) parts.Add("Ctrl");
        if ((modifiersMask & 2) != 0) parts.Add("Shift");
        if ((modifiersMask & 4) != 0) parts.Add("Alt");
        if ((modifiersMask & 8) != 0) parts.Add("Win");
        parts.Add(Name(vk));
        return string.Join("+", parts);
    }

    private static Dictionary<int, string> BuildNames()
    {
        var map = new Dictionary<int, string>
        {
            [VkBack] = "Backspace",
            [VkTab] = "Tab",
            [VkReturn] = "Enter",
            [VkShift] = "Shift",
            [VkControl] = "Ctrl",
            [VkMenu] = "Alt",
            [VkPause] = "Pause",
            [VkCapital] = "Caps",
            [VkEscape] = "Esc",
            [VkSpace] = "Space",
            [VkPrintScreen] = "PrtSc",
            [VkScrollLock] = "ScrLk",
            [VkPrior] = "PgUp",
            [VkNext] = "PgDn",
            [VkEnd] = "End",
            [VkHome] = "Home",
            [VkLeft] = "←",
            [VkUp] = "↑",
            [VkRight] = "→",
            [VkDown] = "↓",
            [VkInsert] = "Ins",
            [VkDelete] = "Del",
            [VkLWin] = "Win",
            [VkRWin] = "Win",
            [VkApps] = "Menu",
            [0xBB] = "=",
            [0xBC] = ",",
            [0xBD] = "-",
            [0xBE] = ".",
            [0xBF] = "/",
            [0xC0] = "`",
            [0xDB] = "[",
            [0xDC] = "\\",
            [0xDD] = "]",
            [0xBA] = ";",
            [0xDE] = "'",
            [0xA0] = "Shift",
            [0xA1] = "Shift",
            [0xA2] = "Ctrl",
            [0xA3] = "Ctrl",
            [0xA4] = "Alt",
            [0xA5] = "Alt",
        };

        for (int i = 0; i <= 9; i++)
        {
            map[VkNumpad0 + i] = $"Num{i}";
        }

        for (int i = 1; i <= 24; i++)
        {
            map[0x70 + i - 1] = $"F{i}";
        }

        return map;
    }

    private static Dictionary<int, KeyGroup> BuildGroups()
    {
        var map = new Dictionary<int, KeyGroup>
        {
            [VkShift] = KeyGroup.Modifier,
            [VkControl] = KeyGroup.Modifier,
            [VkMenu] = KeyGroup.Modifier,
            [VkLWin] = KeyGroup.Modifier,
            [VkRWin] = KeyGroup.Modifier,
            [0xA0] = KeyGroup.Modifier,
            [0xA1] = KeyGroup.Modifier,
            [0xA2] = KeyGroup.Modifier,
            [0xA3] = KeyGroup.Modifier,
            [0xA4] = KeyGroup.Modifier,
            [0xA5] = KeyGroup.Modifier,
            [VkBack] = KeyGroup.Editing,
            [VkTab] = KeyGroup.Editing,
            [VkReturn] = KeyGroup.Editing,
            [VkSpace] = KeyGroup.Editing,
            [VkPrintScreen] = KeyGroup.Editing,
            [VkScrollLock] = KeyGroup.Editing,
            [VkInsert] = KeyGroup.Editing,
            [VkDelete] = KeyGroup.Editing,
            [VkCapital] = KeyGroup.Editing,
            [VkEscape] = KeyGroup.Editing,
            [VkPrior] = KeyGroup.Navigation,
            [VkNext] = KeyGroup.Navigation,
            [VkHome] = KeyGroup.Navigation,
            [VkEnd] = KeyGroup.Navigation,
            [VkLeft] = KeyGroup.Navigation,
            [VkUp] = KeyGroup.Navigation,
            [VkRight] = KeyGroup.Navigation,
            [VkDown] = KeyGroup.Navigation,
        };

        for (int i = 0; i < 26; i++)
        {
            map[0x41 + i] = KeyGroup.Letters;
        }

        for (int i = 0; i < 10; i++)
        {
            map[0x30 + i] = KeyGroup.Digits;
            map[VkNumpad0 + i] = KeyGroup.Digits;
        }

        for (int i = 1; i <= 24; i++)
        {
            map[0x70 + i - 1] = KeyGroup.Function;
        }

        foreach (int oem in new[] { 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF, 0xC0, 0xDB, 0xDC, 0xDD, 0xDE })
        {
            map[oem] = KeyGroup.Punctuation;
        }

        return map;
    }
}
