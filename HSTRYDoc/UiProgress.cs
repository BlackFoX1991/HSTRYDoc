namespace HSTRYDoc
{
    // Small progress DTO used by reporterDiag
    public sealed class UiProgress
    {
        public string Message { get; init; } = "";
        public int? Value { get; init; }          // ProgressBar.Value
        public int? Maximum { get; init; }        // ProgressBar.Maximum
        public bool Indeterminate { get; init; }  // ProgressBar.Style = Marquee
    }
}
