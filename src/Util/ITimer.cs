
namespace FSM
{
	public interface ITimer
	{
		float Elapsed {
			get;
		}

		void Reset();
	}
}
