namespace Maui.TouchEffect;
public partial class TouchEffect : IDisposable
{
    bool isDisposed;

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
	/// Dispose the object.
	/// </summary>
	protected virtual void Dispose(bool disposing)
    {
        if (isDisposed)
        {
            return;
        }

        if (disposing)
        {
            // free managed resources
            gestureManager.Dispose();
            //PlatformDispose();
        }

        isDisposed = true;
    }
}
