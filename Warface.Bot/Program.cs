using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Warface.Bot
{
	class Program
	{
		static async Task Main(string[] args)
		{
			Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

			var config = new WarfaceClientConfiguration
			{
				ConnectServer = "us-comm.wf.my.com",
				Username = "foo",
				Password = "bar",
				PerformCertificateValidation = false,
				UseTls = true,
				Version = "1.22000.688.34300",
				Protect =
				{
					UseProtect = true,
					CryptKey = "2258229240,3024742394,708948876,4080447507,552450252,2900560250,26478313,1607021895,1605563313,1381902462,420810433,2803461988,3617965618,2004419549,3574193287,2699934923,865956517,704595833,2774802385,1283253789,2075299369,2549839160,390365850,9772642,2088597202,3911309918,1894394891,1151281516,2582183832,451349429,4212179508,3778383935",
					CryptIv = "834724096,29884556,849283813,14157667,779975000,969872986,327122214,893084885"
				}
			};

			var bot = new WarfaceClient(config);
			bot.ClientErrored += async e => Console.WriteLine(e.Exception);
			await bot.ConnectAsync();

			while (true)
			{
				await Task.Delay(1);
			}
		}
	}
}
