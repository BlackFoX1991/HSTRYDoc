using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HSTRYDoc
{
    internal static class RichTextBoxPrintHelper
    {
        private const int WM_USER = 0x0400;
        private const int EM_FORMATRANGE = WM_USER + 57;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CHARRANGE
        {
            public int cpMin;
            public int cpMax;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FORMATRANGE
        {
            public IntPtr hdc;
            public IntPtr hdcTarget;
            public RECT rc;
            public RECT rcPage;
            public CHARRANGE chrg;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        // Returns the next char index after what was rendered
        public static int FormatRange(RichTextBox rtb, System.Drawing.Printing.PrintPageEventArgs e, int charFrom, int charTo)
        {
            IntPtr hdc = e.Graphics!.GetHdc();
            try
            {
                RECT rc = new RECT
                {
                    Top = HundredthsInchToTwips(e.MarginBounds.Top),
                    Bottom = HundredthsInchToTwips(e.MarginBounds.Bottom),
                    Left = HundredthsInchToTwips(e.MarginBounds.Left),
                    Right = HundredthsInchToTwips(e.MarginBounds.Right)
                };

                RECT rcPage = new RECT
                {
                    Top = HundredthsInchToTwips(e.PageBounds.Top),
                    Bottom = HundredthsInchToTwips(e.PageBounds.Bottom),
                    Left = HundredthsInchToTwips(e.PageBounds.Left),
                    Right = HundredthsInchToTwips(e.PageBounds.Right)
                };

                FORMATRANGE fr = new FORMATRANGE
                {
                    hdc = hdc,
                    hdcTarget = hdc,
                    rc = rc,
                    rcPage = rcPage,
                    chrg = new CHARRANGE { cpMin = charFrom, cpMax = charTo }
                };

                IntPtr lParam = IntPtr.Zero;
                try
                {
                    int cb = Marshal.SizeOf(fr);
                    lParam = Marshal.AllocCoTaskMem(cb);
                    Marshal.StructureToPtr(fr, lParam, false);

                    // wParam=1: render
                    IntPtr res = SendMessage(rtb.Handle, EM_FORMATRANGE, new IntPtr(1), lParam);
                    return res.ToInt32();
                }
                finally
                {
                    if (lParam != IntPtr.Zero)
                        Marshal.FreeCoTaskMem(lParam);
                }
            }
            finally
            {
                e.Graphics.ReleaseHdc(hdc);
            }
        }

        public static void FormatRangeDone(RichTextBox rtb)
        {
            // wParam=0, lParam=0: release cached information
            SendMessage(rtb.Handle, EM_FORMATRANGE, IntPtr.Zero, IntPtr.Zero);
        }

        private static int HundredthsInchToTwips(int n) => (int)(n * 14.4);
    }
}
