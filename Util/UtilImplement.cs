using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HCore.Modules.Base;

namespace HCore.Packages.HShellUtils.Util;

/// <summary>
/// One-shot command dispatcher. All HShellUtils commands share a single module
/// entry point. The command name is <c>_args[0]</c>.
/// </summary>
public sealed class UtilImplement : BaseImplement, IOneshotCommand
{
    private string[] _args = [];

    private static readonly string[] BuiltinCommands =
    [
        "afcp", "append", "cat", "cd", "cp", "exit", "help", "kill",
        "ls", "mkdir", "mv", "pwd", "rename", "rm", "rmdir", "run",
        "service", "spawn", "touch", "write"
    ];

    public void SetArguments(string[] args) => _args = args;

    public void Run()
    {
        if (_args.Length < 1)
        {
            Console.WriteLine("usage: <command> [args]");
            return;
        }

        switch (_args[0].ToLowerInvariant())
        {
            // ── hash ────────────────────────────────────────────────
            case "sha256": case "sha256sum": RunHash("SHA256", SHA256.HashData, _args); break;
            case "sha512": case "sha512sum": RunHash("SHA512", SHA512.HashData, _args); break;
            case "sha1":   case "sha1sum":   RunHash("SHA1",   SHA1.HashData,   _args); break;
            case "md5":    case "md5sum":    RunHash("MD5",    MD5.HashData,    _args); break;

            // ── encoding ────────────────────────────────────────────
            case "base64": RunBase64(_args); break;

            // ── binary ──────────────────────────────────────────────
            case "hexdump": case "xxd": case "hd": RunHexDump(_args); break;

            // ── text ────────────────────────────────────────────────
            case "wc":   RunWc(_args);   break;
            case "head": RunHead(_args); break;
            case "tail": RunTail(_args); break;
            case "grep": RunGrep(_args); break;
            case "sort": RunSort(_args); break;
            case "uniq": RunUniq(_args); break;

            // ── system ──────────────────────────────────────────────
            case "date":  RunDate(_args);  break;
            case "env":   RunEnv(_args);   break;
            case "which": RunWhich(_args); break;

            // ── terminal ────────────────────────────────────────────
            case "clear": case "cls": Console.Clear(); break;

            // ── file test ───────────────────────────────────────────
            case "exists": case "test": RunExists(_args); break;

            default:
                Console.WriteLine($"{_args[0]}: unknown utility command");
                break;
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  Hash commands
    // ══════════════════════════════════════════════════════════════════

    private void RunHash(string label, Func<byte[], byte[]> hasher, string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine($"usage: {args[0]} <file>");
            return;
        }

        try
        {
            var bytes = ReadFileBytes(args[1]);
            var hash = hasher(bytes);
            Console.WriteLine(Convert.ToHexStringLower(hash));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{args[0]}: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  Base64
    // ══════════════════════════════════════════════════════════════════

    private void RunBase64(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("usage: base64 encode|decode [input]");
            return;
        }

        var mode = args[1].ToLowerInvariant();
        switch (mode)
        {
            case "encode":
            case "e":
            {
                var input = args.Length > 2
                    ? args[2]
                    : Console.In.ReadToEnd();
                Console.WriteLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(input)));
                break;
            }
            case "decode":
            case "d":
            {
                var input = args.Length > 2
                    ? args[2]
                    : Console.In.ReadToEnd().Trim();
                Console.WriteLine(Encoding.UTF8.GetString(Convert.FromBase64String(input)));
                break;
            }
            default:
                Console.WriteLine("base64: unknown mode (use encode|decode)");
                break;
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  Hex dump
    // ══════════════════════════════════════════════════════════════════

    private void RunHexDump(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine($"usage: {args[0]} <file>");
            return;
        }

        try
        {
            var bytes = ReadFileBytes(args[1]);
            for (int offset = 0; offset < bytes.Length; offset += 16)
            {
                var chunk = bytes.AsSpan(offset, Math.Min(16, bytes.Length - offset));
                Console.Write($"{offset:X8}  ");

                for (int i = 0; i < 16; i++)
                {
                    if (i < chunk.Length)
                        Console.Write($"{chunk[i]:X2} ");
                    else
                        Console.Write("   ");

                    if (i == 7) Console.Write(' ');
                }

                Console.Write(" |");
                for (int i = 0; i < chunk.Length; i++)
                {
                    var b = chunk[i];
                    Console.Write(b >= 32 && b <= 126 ? (char)b : '.');
                }
                Console.WriteLine('|');
            }

            Console.WriteLine($"{bytes.Length:X8}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{args[0]}: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  wc
    // ══════════════════════════════════════════════════════════════════

    private void RunWc(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("usage: wc <file>");
            return;
        }

        try
        {
            var text = Vfs.ReadAllText(args[1]);
            var lines = text.Split('\n');
            var lineCount = text.EndsWith('\n') ? lines.Length - 1 : lines.Length;
            var wordCount = 0;
            var charCount = 0L;
            // Use memory snapshot to count characters properly in text
            var span = text.AsSpan();
            var inWord = false;
            for (int i = 0; i < span.Length; i++)
            {
                charCount++;
                if (char.IsWhiteSpace(span[i]))
                {
                    inWord = false;
                }
                else if (!inWord)
                {
                    inWord = true;
                    wordCount++;
                }
            }

            Console.WriteLine($"{lineCount,8} {wordCount,8} {charCount,8} {args[1]}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"wc: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  head / tail
    // ══════════════════════════════════════════════════════════════════

    private void RunHead(string[] args)
    {
        var count = 10;
        var fileIdx = 1;

        if (args.Length >= 2 && (args[1] == "-n" || args[1] == "--lines"))
        {
            if (args.Length < 4 || !int.TryParse(args[2], out count))
            {
                Console.WriteLine("usage: head [-n N] <file>");
                return;
            }
            fileIdx = 3;
        }

        if (args.Length <= fileIdx)
        {
            Console.WriteLine("usage: head [-n N] <file>");
            return;
        }

        try
        {
            var lines = Vfs.ReadAllText(args[fileIdx]).Split('\n');
            for (int i = 0; i < Math.Min(count, lines.Length); i++)
                Console.WriteLine(lines[i]);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"head: {ex.Message}");
        }
    }

    private void RunTail(string[] args)
    {
        var count = 10;
        var fileIdx = 1;

        if (args.Length >= 2 && (args[1] == "-n" || args[1] == "--lines"))
        {
            if (args.Length < 4 || !int.TryParse(args[2], out count))
            {
                Console.WriteLine("usage: tail [-n N] <file>");
                return;
            }
            fileIdx = 3;
        }

        if (args.Length <= fileIdx)
        {
            Console.WriteLine("usage: tail [-n N] <file>");
            return;
        }

        try
        {
            var lines = Vfs.ReadAllText(args[fileIdx]).Split('\n');
            var start = Math.Max(0, lines.Length - count);
            for (int i = start; i < lines.Length; i++)
                Console.WriteLine(lines[i]);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"tail: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  grep
    // ══════════════════════════════════════════════════════════════════

    private void RunGrep(string[] args)
    {
        var ignoreCase = false;
        var invert = false;
        var i = 1;

        while (i < args.Length && args[i].StartsWith('-'))
        {
            switch (args[i])
            {
                case "-i": case "--ignore-case": ignoreCase = true; i++; break;
                case "-v": case "--invert-match": invert = true; i++; break;
                default:
                    Console.WriteLine($"grep: unknown option '{args[i]}'");
                    return;
            }
        }

        if (args.Length - i < 2)
        {
            Console.WriteLine("usage: grep [-i] [-v] <pattern> <file>");
            return;
        }

        var pattern = args[i];
        var file = args[i + 1];

        try
        {
            var options = RegexOptions.None;
            if (ignoreCase) options |= RegexOptions.IgnoreCase;

            var regex = new Regex(pattern, options);
            var lines = Vfs.ReadAllText(file).Split('\n');

            foreach (var line in lines)
            {
                var match = regex.IsMatch(line);
                if (invert ? !match : match)
                    Console.WriteLine(line);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"grep: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  sort
    // ══════════════════════════════════════════════════════════════════

    private void RunSort(string[] args)
    {
        var reverse = false;
        var unique = false;
        var fileIdx = 1;

        while (fileIdx < args.Length && args[fileIdx].StartsWith('-'))
        {
            switch (args[fileIdx])
            {
                case "-r": reverse = true; fileIdx++; break;
                case "-u": unique = true;  fileIdx++; break;
                default:
                    Console.WriteLine($"sort: unknown option '{args[fileIdx]}'");
                    return;
            }
        }

        if (args.Length <= fileIdx)
        {
            Console.WriteLine("usage: sort [-r] [-u] <file>");
            return;
        }

        try
        {
            var lines = Vfs.ReadAllText(args[fileIdx])
                .Split('\n', StringSplitOptions.RemoveEmptyEntries);

            Array.Sort(lines, StringComparer.OrdinalIgnoreCase);

            if (reverse)
                Array.Reverse(lines);

            string? prev = null;
            foreach (var line in lines)
            {
                if (unique && line == prev) continue;
                Console.WriteLine(line);
                prev = line;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"sort: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  uniq
    // ══════════════════════════════════════════════════════════════════

    private void RunUniq(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("usage: uniq <file>");
            return;
        }

        try
        {
            var lines = Vfs.ReadAllText(args[1]).Split('\n');
            string? prev = null;

            foreach (var line in lines)
            {
                if (line != prev)
                    Console.WriteLine(line);
                prev = line;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"uniq: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  date
    // ══════════════════════════════════════════════════════════════════

    private static void RunDate(string[] args)
    {
        Console.WriteLine(DateTime.Now.ToString("o"));
    }

    // ══════════════════════════════════════════════════════════════════
    //  env
    // ══════════════════════════════════════════════════════════════════

    private static void RunEnv(string[] args)
    {
        var vars = Environment.GetEnvironmentVariables();
        var keys = new List<string>(vars.Keys.Count);
        foreach (var key in vars.Keys)
            keys.Add(key?.ToString() ?? "");

        keys.Sort(StringComparer.OrdinalIgnoreCase);
        foreach (var key in keys)
        {
            var val = vars[key]?.ToString() ?? "";
            Console.WriteLine($"{key}={val}");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  which
    // ══════════════════════════════════════════════════════════════════

    private void RunWhich(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("usage: which <command>");
            return;
        }

        var needle = args[1].ToLowerInvariant();

        // Check builtins first
        foreach (var b in BuiltinCommands)
        {
            if (b.Equals(needle, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"{b}: builtin");
                return;
            }
        }

        // Scan pack manifests
        try
        {
            foreach (var pack in Vfs.ListDirectory("/packs"))
            {
                var manifestPath = $"/packs/{pack}/manifest.json";
                if (!Vfs.Exists(manifestPath)) continue;

                try
                {
                    var json = Vfs.ReadAllText(manifestPath);
                    var doc = JsonDocument.Parse(json);
                    if (!doc.RootElement.TryGetProperty("commands", out var commands)) continue;

                    foreach (var cmd in commands.EnumerateArray())
                    {
                        var name = cmd.TryGetProperty("name", out var n) ? n.GetString() : "";
                        if (string.Equals(name, needle, StringComparison.OrdinalIgnoreCase))
                        {
                            var module = cmd.TryGetProperty("moduleName", out var m) ? m.GetString() : "?";
                            Console.WriteLine($"{name}: /packs/{pack} ({module})");
                            return;
                        }
                    }
                }
                catch { }
            }
        }
        catch { }

        Console.WriteLine($"which: {needle} not found");
    }

    // ══════════════════════════════════════════════════════════════════
    //  exists
    // ══════════════════════════════════════════════════════════════════

    private void RunExists(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("usage: exists <path>");
            return;
        }

        Console.WriteLine(Vfs.Exists(args[1]) ? "true" : "false");
    }

    // ══════════════════════════════════════════════════════════════════
    //  Helpers
    // ══════════════════════════════════════════════════════════════════

    private byte[] ReadFileBytes(string path)
    {
        using var stream = Vfs.OpenFileStream(path, FileMode.Open, FileAccess.Read);
        var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
