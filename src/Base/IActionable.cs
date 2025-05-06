
namespace UnityHFSM
{
	/// <summary>
	/// Interface for states that support custom actions. Actions are like the
	/// builtin events <c>OnEnter</c> / <c>OnLogic</c> / ... but are defined by the user.
	/// </summary>
	public interface IActionable<TEvent>
	{
		bool OnAction(TEvent trigger);
		bool OnAction<TData>(TEvent trigger, TData data);
	}

	/// <inheritdoc />
	public interface IActionable : IActionable<string>
	{
	}
}
