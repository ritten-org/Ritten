// This is a copy of the .Net internal AnsiParser class used in Microsoft.Extensions.Logging.Console
// https://github.com/dotnet/runtime/blob/release/8.0/src/libraries/Microsoft.Extensions.Logging.Console/src/AnsiParser.cs
//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Ritten.Core.Logging;

internal sealed class AnsiParser
{
    private readonly Action<string, int, int, ConsoleColor?, ConsoleColor?> _onParseWrite;

    public AnsiParser(Action<string, int, int, ConsoleColor?, ConsoleColor?> onParseWrite)
    {
        ArgumentNullException.ThrowIfNull(onParseWrite);

        _onParseWrite = onParseWrite;
    }

    /// <summary>
    /// Parses a subset of display attributes
    /// Set Display Attributes
    /// Set Attribute Mode [{attr1};...;{attrn}m
    /// Sets multiple display attribute settings. The following lists standard attributes that are getting parsed:
    /// 1 Bright
    /// Foreground Colours
    /// 30 Black
    /// 31 Red
    /// 32 Green
    /// 33 Yellow
    /// 34 Blue
    /// 35 Magenta
    /// 36 Cyan
    /// 37 White
    /// Background Colours
    /// 40 Black
    /// 41 Red
    /// 42 Green
    /// 43 Yellow
    /// 44 Blue
    /// 45 Magenta
    /// 46 Cyan
    /// 47 White
    /// </summary>
    public void Parse(string message)
    {
        var startIndex = -1;
        var length = 0;
        ConsoleColor? foreground = null;
        ConsoleColor? background = null;
        var span = message.AsSpan();
        const char escapeChar = '';
        var isBright = false;
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] == escapeChar && span.Length >= i + 4 && span[i + 1] == '[')
            {
                int escapeCode;
                if (span[i + 3] == 'm')
                {
                    if (IsDigit(span[i + 2]))
                    {
                        escapeCode = span[i + 2] - '0';
                        if (startIndex != -1)
                        {
                            _onParseWrite(message, startIndex, length, background, foreground);
                            startIndex = -1;
                            length = 0;
                        }

                        if (escapeCode == 1)
                        {
                            isBright = true;
                        }

                        i += 3;
                        continue;
                    }
                }
                else if (span.Length >= i + 5 && span[i + 4] == 'm')
                {
                    if (IsDigit(span[i + 2]) && IsDigit(span[i + 3]))
                    {
                        escapeCode = (span[i + 2] - '0') * 10 + (span[i + 3] - '0');
                        if (startIndex != -1)
                        {
                            _onParseWrite(message, startIndex, length, background, foreground);
                            startIndex = -1;
                            length = 0;
                        }

                        if (TryGetForegroundColor(escapeCode, isBright, out var color))
                        {
                            foreground = color;
                            isBright = false;
                        }
                        else if (TryGetBackgroundColor(escapeCode, out color))
                        {
                            background = color;
                        }

                        i += 4;
                        continue;
                    }
                }
            }

            if (startIndex == -1)
            {
                startIndex = i;
            }

            var nextEscapeIndex = -1;
            if (i < message.Length - 1)
            {
                nextEscapeIndex = message.IndexOf(escapeChar, i + 1);
            }

            if (nextEscapeIndex < 0)
            {
                length = message.Length - startIndex;
                break;
            }

            length = nextEscapeIndex - startIndex;
            i = nextEscapeIndex - 1;
        }

        if (startIndex != -1)
        {
            _onParseWrite(message, startIndex, length, background, foreground);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDigit(char c) => (uint)(c - '0') <= ('9' - '0');

    internal const string DefaultForegroundColor = "[39m[22m";
    internal const string DefaultBackgroundColor = "[49m";

    internal static string GetForegroundColorEscapeCode(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => "[30m",
            ConsoleColor.DarkRed => "[31m",
            ConsoleColor.DarkGreen => "[32m",
            ConsoleColor.DarkYellow => "[33m",
            ConsoleColor.DarkBlue => "[34m",
            ConsoleColor.DarkMagenta => "[35m",
            ConsoleColor.DarkCyan => "[36m",
            ConsoleColor.Gray => "[37m",
            ConsoleColor.Red => "[1m[31m",
            ConsoleColor.Green => "[1m[32m",
            ConsoleColor.Yellow => "[1m[33m",
            ConsoleColor.Blue => "[1m[34m",
            ConsoleColor.Magenta => "[1m[35m",
            ConsoleColor.Cyan => "[1m[36m",
            ConsoleColor.White => "[1m[37m",
            _ => DefaultForegroundColor
        };
    }

    internal static string GetBackgroundColorEscapeCode(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => "[40m",
            ConsoleColor.DarkRed => "[41m",
            ConsoleColor.DarkGreen => "[42m",
            ConsoleColor.DarkYellow => "[43m",
            ConsoleColor.DarkBlue => "[44m",
            ConsoleColor.DarkMagenta => "[45m",
            ConsoleColor.DarkCyan => "[46m",
            ConsoleColor.Gray => "[47m",
            _ => DefaultBackgroundColor
        };
    }

    private static bool TryGetForegroundColor(int number, bool isBright, out ConsoleColor? color)
    {
        color = number switch
        {
            30 => ConsoleColor.Black,
            31 => isBright ? ConsoleColor.Red : ConsoleColor.DarkRed,
            32 => isBright ? ConsoleColor.Green : ConsoleColor.DarkGreen,
            33 => isBright ? ConsoleColor.Yellow : ConsoleColor.DarkYellow,
            34 => isBright ? ConsoleColor.Blue : ConsoleColor.DarkBlue,
            35 => isBright ? ConsoleColor.Magenta : ConsoleColor.DarkMagenta,
            36 => isBright ? ConsoleColor.Cyan : ConsoleColor.DarkCyan,
            37 => isBright ? ConsoleColor.White : ConsoleColor.Gray,
            _ => null
        };
        return color != null || number == 39;
    }

    private static bool TryGetBackgroundColor(int number, out ConsoleColor? color)
    {
        color = number switch
        {
            40 => ConsoleColor.Black,
            41 => ConsoleColor.DarkRed,
            42 => ConsoleColor.DarkGreen,
            43 => ConsoleColor.DarkYellow,
            44 => ConsoleColor.DarkBlue,
            45 => ConsoleColor.DarkMagenta,
            46 => ConsoleColor.DarkCyan,
            47 => ConsoleColor.Gray,
            _ => null
        };
        return color != null || number == 49;
    }
}
