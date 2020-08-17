using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Warface.EventArgs;

namespace Warface
{
	public delegate Task AsyncEventHandler<in T>(T e) where T : AsyncEventArgs;

	public class AsyncEvent<T> where T : AsyncEventArgs
	{
		private List<AsyncEventHandler<T>> handlerList;

		public IReadOnlyList<AsyncEventHandler<T>> Handlers
		{
			get
			{
				AsyncEventHandler<T>[] result;

				lock (this.handlerList)
					result = this.handlerList.ToArray();

				return result;
			}
		}

		public AsyncEvent()
		{
			this.handlerList = new List<AsyncEventHandler<T>>();
		}

		public void Register(AsyncEventHandler<T> handler)
		{
			lock (this.handlerList)
				this.handlerList.Add(handler);
		}

		public void Unregister(AsyncEventHandler<T> handler)
		{
			lock (this.handlerList)
				this.handlerList.Remove(handler);
		}

		public async Task InvokeAsync(T e)
		{
			var exceptions = new List<Exception>();

			foreach (var handler in this.Handlers)
			{
				try
				{
					await handler(e).ConfigureAwait(false);

					if (e.Handled)
						break;
				}
				catch (Exception ex)
				{
					exceptions.Add(ex);
				}
			}

			if (exceptions.Any())
				throw new AggregateException(exceptions);
		}
	}
}


namespace Warface.EventArgs
{
	public abstract class AsyncEventArgs : System.EventArgs
	{
		public bool Handled { get; set; }
	}
}