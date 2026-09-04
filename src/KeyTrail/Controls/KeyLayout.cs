namespace KeyTrail.Controls;

internal readonly record struct KeySpec(int Vk, string Label, double X, double Y, double Width);

internal static class KeyboardLayout
{
    public static IReadOnlyList<KeySpec> Keys { get; } = Build();

    private static List<KeySpec> Build()
    {
        var keys = new List<KeySpec>();
        double funcY = 0.35;
        double funcW = 0.95;
        double funcGap = 0.35;

        Add(keys, 0x1B, "Esc", 0.1, funcY, funcW);
        double fx = 1.4;
        string[] functionNames = ["F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12"];
        for (int i = 0; i < functionNames.Length; i++)
        {
            if (i == 4 || i == 8)
            {
                fx += funcGap;
            }

            Add(keys, 0x70 + i, functionNames[i], fx, funcY, funcW);
            fx += funcW + funcGap;
        }

        Add(keys, 0x2C, "PrtSc", fx, funcY, funcW);
        Add(keys, 0x91, "ScrLk", fx + funcW + funcGap, funcY, funcW);
        Add(keys, 0x13, "Pause", fx + 2 * (funcW + funcGap), funcY, funcW);

        const double rowY0 = 1.9;
        const double rowGap = 1.18;
        const double mainX = 2.3;
        double row1 = rowY0;
        double row2 = rowY0 + rowGap;
        double row3 = rowY0 + 2 * rowGap;
        double row4 = rowY0 + 3 * rowGap;
        double row5 = rowY0 + 4 * rowGap;

        // Row 1
        Add(keys, 0xC0, "`", mainX + 0, row1, 1);
        for (int i = 0; i < 10; i++)
        {
            Add(keys, 0x30 + i, ((char)(0x30 + i)).ToString(), mainX + 1 + i, row1, 1);
        }

        Add(keys, 0xBD, "-", mainX + 11, row1, 1);
        Add(keys, 0xBB, "=", mainX + 12, row1, 1);
        Add(keys, 0x08, "⌫", mainX + 13, row1, 2);

        // Row 2
        Add(keys, 0x09, "Tab", mainX + 0, row2, 1.5);
        string row2Chars = "QWERTYUIOP";
        for (int i = 0; i < row2Chars.Length; i++)
        {
            Add(keys, row2Chars[i], row2Chars[i].ToString(), mainX + 1.5 + i, row2, 1);
        }

        Add(keys, 0xDB, "[", mainX + 11.5, row2, 1);
        Add(keys, 0xDD, "]", mainX + 12.5, row2, 1);
        Add(keys, 0xDC, "\\", mainX + 13.5, row2, 1.5);

        // Row 3
        Add(keys, 0x14, "Caps", mainX + 0, row3, 1.75);
        string row3Chars = "ASDFGHJKL";
        for (int i = 0; i < row3Chars.Length; i++)
        {
            Add(keys, row3Chars[i], row3Chars[i].ToString(), mainX + 1.75 + i, row3, 1);
        }

        Add(keys, 0xBA, ";", mainX + 10.75, row3, 1);
        Add(keys, 0xDE, "'", mainX + 11.75, row3, 1);
        Add(keys, 0x0D, "Enter", mainX + 12.75, row3, 2.25);

        // Row 4
        Add(keys, 0xA0, "Shift", mainX + 0, row4, 2.25);
        string row4Chars = "ZXCVBNM";
        for (int i = 0; i < row4Chars.Length; i++)
        {
            Add(keys, row4Chars[i], row4Chars[i].ToString(), mainX + 2.25 + i, row4, 1);
        }

        Add(keys, 0xBC, ",", mainX + 9.25, row4, 1);
        Add(keys, 0xBE, ".", mainX + 10.25, row4, 1);
        Add(keys, 0xBF, "/", mainX + 11.25, row4, 1);
        Add(keys, 0xA1, "Shift", mainX + 12.25, row4, 2.75);

        // Row 5
        Add(keys, 0xA2, "Ctrl", mainX + 0, row5, 1.25);
        Add(keys, 0x5B, "Win", mainX + 1.25, row5, 1.25);
        Add(keys, 0xA4, "Alt", mainX + 2.5, row5, 1.25);
        Add(keys, 0x20, "Space", mainX + 3.75, row5, 6.25);
        Add(keys, 0xA5, "Alt", mainX + 10, row5, 1.25);
        Add(keys, 0x5C, "Win", mainX + 11.25, row5, 1.25);
        Add(keys, 0x5D, "Menu", mainX + 12.5, row5, 1.25);
        Add(keys, 0xA3, "Ctrl", mainX + 13.75, row5, 1.25);

        return keys;
    }

    private static void Add(List<KeySpec> keys, int vk, string label, double x, double y, double width)
    {
        keys.Add(new KeySpec(vk, label, x, y, width));
    }
}
