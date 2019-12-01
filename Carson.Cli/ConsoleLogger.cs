using System;
using System.Speech.Synthesis;

namespace Experiment1
{
	public class ConsoleLogger : ILogService
	{
		SpeechSynthesizer synth;

		public ConsoleLogger()
		{
            synth = new SpeechSynthesizer();
            synth.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult);
			synth.SetOutputToDefaultAudioDevice();
		}

		public void Write(string message = null, params object[] args)
		{
			if (message == null) Console.WriteLine();
			else
			{
				message = String.Format(message, args);
				Console.WriteLine($"{DateTime.Now:H:mm:ss} {message}");
			}
		}

		public void Speak(string message, params object[] args)
		{
			message = String.Format(message, args);
			synth.SpeakAsync(message);
			Write(message);
		}
	}
}
