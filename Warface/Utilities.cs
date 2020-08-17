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
	}
}
