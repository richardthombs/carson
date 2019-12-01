namespace Experiment1
{
	public interface ILogService
	{
		void Write(string message = null, params object[] args);
		void Speak(string message, params object[] args);
	}
}
