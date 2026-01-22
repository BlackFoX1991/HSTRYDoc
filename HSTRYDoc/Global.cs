// Global.cs
using System.Text;

namespace HSTRYDoc
{
    public static class Global
    {
        public static readonly string AppName = "HstryDocu";
        public static readonly string AppVersion = "1.0.0";

        /// <summary>
        /// Represents the version identifier for the container format.
        /// </summary>
        public static readonly byte ContainerVersion = 0x10;

        /// <summary>
        /// Default encoding for RTF string <-> bytes.
        /// The RTF text itself may contain \ansi/\ansicpg directives, but we store the .NET string deterministically as UTF-8 by default.
        /// </summary>
        public static Encoding CurrentEditorEncoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// File magic for quick identification.
        /// </summary>
        public static readonly byte[] FileMagic = { (byte)'H', (byte)'S', (byte)'T', (byte)'R', (byte)'Y' };
    }
}
