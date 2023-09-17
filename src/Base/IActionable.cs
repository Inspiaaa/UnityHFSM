
namespace UnityHFSM
{
	/// <summary>
	/// Interface for states that support custom actions. Actions are like the
	/// builtin events OnEnter / OnLogic / ... but are defined by the user.
	/// </summary>
	public interface IActionable<TEvent>
	{
		void OnAction(TEvent trigger);
		void OnAction<TData>(TEvent trigger, TData data);
	}

	/// <inheritdoc />
	public interface IActionable : IActionable<string>
	{
	}
}
