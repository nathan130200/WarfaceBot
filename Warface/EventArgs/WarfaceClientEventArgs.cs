using System;

namespace Warface.EventArgs
{
	public class WarfaceClientEventArgs : AsyncEventArgs
	{
		public WarfaceClient Client { get; internal set; }
	}

	public class WarfaceClientErrorEventArgs : WarfaceClientEventArgs
	{
		public Exception Exception { get; internal set; }
	}
}
