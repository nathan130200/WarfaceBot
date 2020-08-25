using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Warface.Bot
{
	class Program
	{
		static async Task Main(string[] args)
		{
			var logger = new TraceSource("WarfaceClient", SourceLevels.All);
			logger.Listeners.Add(new TextWriterTraceListener(Console.Out));

			var config = new WarfaceClientConfiguration
			{
				ConnectServer = "us-comm.wf.my.com",
				Username = "foo",
				Password = "bar",
				PerformCertificateValidation = false,
				UseTls = true,
				Version = "1.22100.2081.34300",
				Protect =
				{
					UseProtect = true,
					CryptKey = "511074014,2802349803,1845853172,306870106,1913513264,3444525495,3469498652,4004889749,775752808,1345923615,2591009239,2628112042,1135312856,3169297862,4037457392,1701895742,1413228313,2356288144,2396766860,2745924066,2318764751,3722323435,628320797,1531605067,1482403435,2994344611,930618263,3090018487,3466396200,3423528467,206240932,1592870169",
					CryptIv = "834724096,29884556,849283813,14157667,779975000,969872986,327122214,893084885"
				},
				Logger = logger
			};

			var bot = new WarfaceClient(config);

			bot.ClientErrored += e =>
			{
				Console.WriteLine(e.Exception.ToString());
				return Task.CompletedTask;
			};

			bot.Connected += e =>
			{
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine("Connected");
				Console.ResetColor();

				return Task.CompletedTask;
			};

			bot.Disconnected += e =>
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("Disconnected");
				Console.ResetColor();

				return Task.CompletedTask;
			};

			await bot.ConnectAsync();

			while (true)
			{
				await Task.Delay(1);
			}
		}
	}
}
