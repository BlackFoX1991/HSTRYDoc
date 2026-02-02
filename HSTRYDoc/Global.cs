// Global.cs
using System.Text;

namespace HSTRYDoc
{
    public static class Global
    {
        public static readonly string AppName = "HstryDocu";
        public static readonly string AppVersion = "5.1.5";

        /// <summary>
        /// Default encoding for RTF string <-> bytes.
        /// The RTF text itself may contain \ansi/\ansicpg directives, but we store the .NET string deterministically as UTF-8 by default.
        /// </summary>
        public static Encoding CurrentEditorEncoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// File magic for quick identification.
        /// </summary>
        public static readonly byte[] FileMagic = { (byte)'H', (byte)'S', (byte)'T', (byte)'R', (byte)'Y' };

        /// <summary>
        /// Gets or sets a value indicating whether the application is running in test mode.
        /// </summary>
        /// <remarks>When enabled, test mode may alter application behavior to facilitate testing
        /// scenarios. This property should be set before performing operations that depend on the application's
        /// execution mode.</remarks>
        public static bool Testmode { get; set; } = false;

    }
}
