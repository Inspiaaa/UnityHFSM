
namespace UnityHFSM
{
	/// <summary>
	/// Interface for states that can receive events (triggers), such as StateMachines.
	/// </summary>
	/// <typeparam name="TEvent"></typeparam>
	public interface ITriggerable<TEvent>
	{
		/// <summary>
		/// Called when a trigger is activated.
		/// </summary>
		/// <param name="trigger">The name / identifier of the trigger</param>
		void Trigger(TEvent trigger);
	}

	/// <inheritdoc />
	public interface ITriggerable : ITriggerable<string>
	{
	}
}
