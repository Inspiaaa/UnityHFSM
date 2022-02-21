
namespace FSM
{
	public interface IActionable<TEvent>
	{
		void OnAction(TEvent trigger);
		void OnAction<TData>(TEvent trigger, TData data);
	}

	public interface IActionable : IActionable<string>
	{
	}
}
