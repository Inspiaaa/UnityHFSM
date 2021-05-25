
namespace FSM
{
	public interface ITriggerable
	{
		/// <summary>
		/// Called when a trigger is activated.
		/// </summary>
		/// <param name="trigger">The name / identifier of the trigger</param>
		void Trigger(string trigger);
	}
}
