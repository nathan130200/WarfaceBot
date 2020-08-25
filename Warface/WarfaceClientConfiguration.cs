using System;
using System.Diagnostics;
using System.Net.Security;
using System.Text;

namespace Warface
{
	/// <summary>
	/// Represents warface client configuration class.
	/// </summary>
	public class WarfaceClientConfiguration
	{
		/// <summary>
		/// Connection logging trace.
		/// </summary>
		public TraceSource Logger { internal get; set; } = default;

		/// <summary>
		/// Server host to connect to.
		/// </summary>
		public string ConnectServer { internal get; set; }

		/// <summary>
		/// XMPP Hostname. Default: warface.
		/// </summary>
		public string Server { internal get; set; } = "warface";

		/// <summary>
		/// Server port to connect to. Defaults to: 5222.
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
		/// Determins if SSL/TLS handshake is required. Defaults to: <see langword="true" />.
		/// </summary>
		public bool UseTls { internal get; set; }

		internal string BuildSaslArguments()
		{
			var sasl = string.Concat(this.Password, '\0', this.Username, '\0');
			return Convert.ToBase64String(Encoding.UTF8.GetBytes(sasl));
		}

		/// <summary>
		/// Determins if tls handshake should validate remote server certificate. Defaults to: <see langword="false" />.
		/// </summary>
		public bool PerformCertificateValidation { internal get; set; }

		/// <summary>
		/// Custom remote server certificate validation callback. Defaults to: none.
		/// </summary>
		public RemoteCertificateValidationCallback CertificateValidationCallback { internal get; set; } = default;

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
			/// Determins if protect layer will be used or not. Defaults to: <see langword="false" />. <i>Protect is an extra encryptation layer in official warface server.</i>
			/// </summary>
			public bool UseProtect { internal get; set; } = false;

			/// <summary>
			/// Encryption Key to use. Defaults to: none
			/// </summary>
			public string CryptKey { internal get; set; } = "834724096,29884556,849283813,14157667,779975000,969872986,327122214,893084885";

			/// <summary>
			/// Encryption IV (initialization vector) to use. Defaults to: none
			/// </summary>
			public string CryptIv { internal get; set; } = "0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0";
		}
	}
}
