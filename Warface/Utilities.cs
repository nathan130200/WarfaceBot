using System.Diagnostics;
using System.Text;
using AgsXMPP.Xml.Dom;

namespace Warface
{
	public static class Utilities
	{
		public static byte[] GetBytes(this string str)
			=> Encoding.UTF8.GetBytes(str);

		public static byte[] GetBytes(this Element e)
			=> e.ToString(false).GetBytes();

		internal static void LogVerbose(this WarfaceClient client, string message)
			=> client.Config.Logger?.TraceEvent(TraceEventType.Verbose, 0, $"[{client.Jid}]: {message}");

		internal static void LogWarning(this WarfaceClient client, string message)
			=> client.Config.Logger?.TraceEvent(TraceEventType.Warning, 0, $"[{client.Jid}] {message}");

		internal static void LogError(this WarfaceClient client, string message)
			=> client.Config.Logger?.TraceEvent(TraceEventType.Error, 0, $"[{client.Jid}] {message}");

		internal static void LogInformation(this WarfaceClient client, string message)
			=> client.Config.Logger?.TraceEvent(TraceEventType.Information, 0, $"[{client.Jid}] {message}");
	}
}
