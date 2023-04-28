namespace GitLabSymbolServerProxy;

public static class LockExtensions {
	public static async Task WithLock(this SemaphoreSlim semaphore, Action action) {
		await semaphore.WaitAsync();
		try {
			action();
		} finally {
			semaphore.Release();
		}
	}

	public static async Task WithLock(this SemaphoreSlim semaphore, Func<Task> fn) {
		await semaphore.WaitAsync();
		try {
			await fn();
		} finally {
			semaphore.Release();
		}
	}

	public static async Task<T> WithLock<T>(this SemaphoreSlim semaphore, Func<T> fn) {
		await semaphore.WaitAsync();
		try {
			return fn();
		} finally {
			semaphore.Release();
		}
	}
}