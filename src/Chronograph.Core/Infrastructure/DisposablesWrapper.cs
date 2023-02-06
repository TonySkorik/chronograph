namespace Chronograph.Core.Infrastructure;

internal class DisposablesWrapper : IDisposable
{
	private readonly ICollection<IDisposable> _disposables;

	internal DisposablesWrapper(ICollection<IDisposable> disposablesToWrap)
	{
		_disposables = disposablesToWrap;
	}

	public void Dispose()
	{
		if (_disposables == null || _disposables.Count == 0)
		{
			return;
		}

		foreach (var disposable in _disposables)
		{
			disposable.Dispose();
		}
	}
}