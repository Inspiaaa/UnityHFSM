using System;

namespace FSM
{
	public static class StateMachineShortcuts
	{
		/*
			"Shortcut" methods
			- These are meant to reduce the boilerplate code required by the user for simple
			states and transitions.
			- They do this by creating a new State / Transition instance in the background
			and then setting the desired fields.
			- They can also optimise certain cases for you by choosing the best type,
			such as a StateBase for an empty state instead of a State instance.
		*/

		/// <summary>
		/// Shortcut method for adding a regular state.
		/// It creates a new State() instance under the hood. => See State for more information.
		/// For empty states with no logic it creates a new StateBase for optimal performance.
		/// </summary>
		public static void AddState<TOwnId, TStateId, TEvent>(
			this StateMachine<TOwnId, TStateId, TEvent> fsm,
			TStateId name,
			Action<State<TStateId, TEvent>> onEnter = null,
			Action<State<TStateId, TEvent>> onLogic = null,
			Action<State<TStateId, TEvent>> onExit = null,
			Func<State<TStateId, TEvent>, bool> canExit = null,
			bool needsExitTime = false)
		{
			// Optimise for empty states
			if (onEnter == null && onLogic == null && onExit == null && canExit == null)
			{
				fsm.AddState(name, new StateBase<TStateId>(needsExitTime));
				return;
			}

			fsm.AddState(name, new State<TStateId, TEvent>(onEnter, onLogic, onExit, canExit, needsExitTime));
		}

		/// <summary>
		/// Creates the most efficient transition type possible for the given parameters.
		/// It creates a Transition instance when a condition is specified and otherwise
		/// it returns a TransitionBase.
		/// </summary>
		private static TransitionBase<TStateId> CreateOptimizedTransition<TStateId>(
			TStateId from,
			TStateId to,
			Func<Transition<TStateId>, bool> condition = null,
			bool forceInstantly = false)
		{
			if (condition == null)
				return new TransitionBase<TStateId>(from, to, forceInstantly);

			return new Transition<TStateId>(from, to, condition, forceInstantly);
		}

		/// <summary>
		/// Shortcut method for adding a regular transition.
		/// It creates a new Transition() instance under the hood. => See Transition for more information.
		/// When no condition is required, it creates a TransitionBase for optimal performance.
		/// </summary>
		public static void AddTransition<TOwnId, TStateId, TEvent>(
			this StateMachine<TOwnId, TStateId, TEvent> fsm,
			TStateId from,
			TStateId to,
			Func<Transition<TStateId>, bool> condition = null,
			bool forceInstantly = false)
		{
			fsm.AddTransition(CreateOptimizedTransition(from, to, condition, forceInstantly));
		}

		/// <summary>
		/// Shortcut method for adding a regular transition that can happen from any state.
		/// It creates a new Transition() instance under the hood. => See Transition for more information.
		/// When no condition is required, it creates a TransitionBase for optimal performance.
		/// </summary>
		public static void AddTransitionFromAny<TOwnId, TStateId, TEvent>(
			this StateMachine<TOwnId, TStateId, TEvent> fsm,
			TStateId to,
			Func<Transition<TStateId>, bool> condition = null,
			bool forceInstantly = false)
		{
			fsm.AddTransition(CreateOptimizedTransition(default, to, condition, forceInstantly));
		}

		/// <summary>
		/// Shortcut method for adding a new trigger transition between two states that is only checked
		/// when the specified trigger is activated.
		/// It creates a new Transition() instance under the hood. => See Transition for more information.
		/// When no condition is required, it creates a TransitionBase for optimal performance.
		/// </summary>
		public static void AddTriggerTransition<TOwnId, TStateId, TEvent>(
			this StateMachine<TOwnId, TStateId, TEvent> fsm,
			TEvent trigger,
			TStateId from,
			TStateId to,
			Func<Transition<TStateId>, bool> condition = null,
			bool forceInstantly = false)
		{
			fsm.AddTriggerTransition(trigger, CreateOptimizedTransition(from, to, condition, forceInstantly));
		}

		/// <summary>
		/// Shortcut method for adding a new trigger transition that can happen from any possible state, but is only
		/// checked when the specified trigger is activated.
		/// It creates a new Transition() instance under the hood. => See Transition for more information.
		/// When no condition is required, it creates a TransitionBase for optimal performance.
		/// </summary>
		public static void AddTriggerTransitionFromAny<TOwnId, TStateId, TEvent>(
			this StateMachine<TOwnId, TStateId, TEvent> fsm,
			TEvent trigger,
			TStateId to,
			Func<Transition<TStateId>, bool> condition = null,
			bool forceInstantly = false)
		{
			fsm.AddTriggerTransitionFromAny(trigger, CreateOptimizedTransition(default, to, condition, forceInstantly));
		}
	}
}