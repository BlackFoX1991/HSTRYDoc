using System;
using System.Windows.Forms;

namespace HSTRYDoc
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ApplyCommandLineArgs(args);

            ApplicationConfiguration.Initialize();
            Application.Run(new hsMain());
        }

        private static void ApplyCommandLineArgs(string[]? args)
        {
            if (args == null || args.Length == 0)
                return;

            for (int i = 0; i < args.Length; i++)
            {
                string raw = args[i] ?? string.Empty;
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                // Only handle switches starting with "--"
                if (!raw.StartsWith("--", StringComparison.Ordinal))
                    continue;

                string key = raw;
                string? value = null;

                // Support: --key=value
                int eq = raw.IndexOf('=');
                if (eq >= 0)
                {
                    key = raw.Substring(0, eq);
                    value = raw.Substring(eq + 1);
                }
                else
                {
                    // Support: --key value  (value is next arg if it is NOT another switch)
                    if (i + 1 < args.Length)
                    {
                        string next = args[i + 1] ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(next) && !next.StartsWith("--", StringComparison.Ordinal))
                        {
                            value = next;
                            i++; // consume next token as the value
                        }
                    }
                }

                // Normalize
                key = key.ToLowerInvariant();

                switch (key)
                {
                    case "--dev":
                        Global.Testmode = true;
                        break;

                    // Example: --loglevel=debug  OR  --loglevel debug
                    // case "--loglevel":
                    //     Global.LogLevel = string.IsNullOrWhiteSpace(value) ? "info" : value!;
                    //     break;

                    // Example: --exportdir "C:\Temp"
                    // case "--exportdir":
                    //     if (!string.IsNullOrWhiteSpace(value))
                    //         Global.ExportDirectory = value!;
                    //     break;

                    default:
                        // Unknown switch: ignore silently (or log in dev mode)
                        break;
                }
            }
        }
    }
}
