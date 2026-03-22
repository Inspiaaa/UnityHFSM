
namespace UnityHFSM
{
	/// <summary>
	/// Provides an interface for states to respond to custom, user-defined actions.
	/// Actions are like the builtin events <c>OnEnter</c> / <c>OnLogic</c> / ... but can be
	/// defined by the user.
	/// </summary>
	/// <remarks>
	/// Actions are hierarchical. In UnityHFSM, triggering an action on the current state will
	/// also automatically propagate that action through the nested hierarchy to any active
	/// child states.
	/// </remarks>
	/// <typeparam name="TEvent">The type used to identify actions (typically an enum or string).</typeparam>
	public interface IActionable<TEvent>
	{
		/// <summary>
		/// Triggers the specified action on the currently active state.
		/// If the action is defined in this state or any active sub-states, the associated logic will be executed.
		/// </summary>
		/// <remarks>
		/// If the active state has not defined an action for the specified trigger, then nothing happens.
		/// </remarks>
		/// <param name="trigger">The identifier of the action to trigger.</param>
		void OnAction(TEvent trigger);

		/// <summary>
		/// Triggers the specified action with a data parameter on the currently active state.
		/// If the action is defined in this state or any active sub-states, the associated logic will be executed.
		/// </summary>
		/// <remarks>
		/// If the active state has not defined an action for the specified trigger, then nothing happens.
		/// </remarks>
		/// <param name="trigger">The identifier of the action to trigger.</param>
		/// <param name="data">The custom data to pass to the action's logic.</param>
		/// <typeparam name="TData">Type of the data parameter.
		/// 	Should match the data type of the action that was added via <c>AddAction&lt;T&gt;(...)</c>.</typeparam>
		void OnAction<TData>(TEvent trigger, TData data);

		/// <summary>
		/// Checks whether a handler for the specified action exists in the current state
		/// or any of its active sub-states.
		/// </summary>
		bool HasAction(TEvent trigger);
	}

	/// <inheritdoc />
	public interface IActionable : IActionable<string>
	{
	}
}
