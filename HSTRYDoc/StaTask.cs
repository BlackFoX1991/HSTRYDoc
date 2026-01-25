namespace HSTRYDoc
{
    // STA helper for WinForms components (RichTextBox / PrintDocument) used off-UI-thread.
    public static class StaTask
    {
        public static Task RunAsync(Action action, CancellationToken token = default)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

            var thread = new Thread(() =>
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    action();
                    tcs.TrySetResult(null);
                }
                catch (OperationCanceledException oce)
                {
                    tcs.TrySetCanceled(oce.CancellationToken);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return tcs.Task;
        }

        public static Task<T> RunAsync<T>(Func<T> func, CancellationToken token = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

            var thread = new Thread(() =>
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    T result = func();
                    tcs.TrySetResult(result);
                }
                catch (OperationCanceledException oce)
                {
                    tcs.TrySetCanceled(oce.CancellationToken);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return tcs.Task;
        }
    }
}
