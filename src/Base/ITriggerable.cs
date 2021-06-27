
namespace FSM
{
	public interface ITriggerable<TEvent>
	{
		/// <summary>
		/// Called when a trigger is activated.
		/// </summary>
		/// <param name="trigger">The name / identifier of the trigger</param>
		void Trigger(TEvent trigger);
	}

	public interface ITriggerable : ITriggerable<string>
	{
	}
}
