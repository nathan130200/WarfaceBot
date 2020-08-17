using System.Net.Security;

namespace Warface
{
	/// <summary>
	/// Represents warface client configuration class.
	/// </summary>
	public class WarfaceClientConfiguration
	{
		/// <summary>
		/// Server host to connect to.
		/// </summary>
		public string ConnectServer { internal get; set; }

		/// <summary>
		/// XMPP Hostname.
		/// </summary>
		public string Server { internal get; set; } = "warface";

		/// <summary>
		/// Server port to connect to.
		/// </summary>
		public ushort Port { internal get; set; } = 5222;

		/// <summary>
		/// XMPP Username.
		/// </summary>
		public string Username { internal get; set; }

		/// <summary>
		/// XMPP Password.
		/// </summary>
		public string Password { internal get; set; }

		/// <summary>
		/// Determins if tls handshake is required or not.
		/// </summary>
		public bool UseTls { internal get; set; }

		/// <summary>
		/// Determins if tls handshake should validate remote server certificate.
		/// </summary>
		public bool PerformCertificateValidation { internal get; set; }

		/// <summary>
		/// Custom remote server certificate validation callback.
		/// </summary>
		public RemoteCertificateValidationCallback CertificateValidationCallback { internal get; set; }

		/// <summary>
		/// Warface version.
		/// </summary>
		public string Version { internal get; set; }

		/// <summary>
		/// Protect configuration.
		/// </summary>
		public WarfaceProtectConfiguration Protect { get; } = new WarfaceProtectConfiguration();


		// ----------------------- Inner Classes ----------------------- //

		/// <summary>
		/// Protect configuration class.
		/// </summary>
		public sealed class WarfaceProtectConfiguration
		{
			/// <summary>
			/// Determins if protect layer will be used or not.
			/// <para><i>Protect is an extra encryptation layer in official warface server.</i></para>
			/// </summary>
			public bool UseProtect { internal get; set; } = false;

			/// <summary>
			/// 
			/// </summary>
			public string CryptKey { internal get; set; } = "834724096,29884556,849283813,14157667,779975000,969872986,327122214,893084885";

			/// <summary>
			/// 
			/// </summary>
			public string CryptIv { internal get; set; } = "0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0";
		}
	}
}
