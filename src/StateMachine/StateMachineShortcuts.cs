using System;

namespace UnityHFSM
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
		/// </summary>
		/// <remarks>
		/// It creates a new State() instance under the hood. => See State for more information.
		/// For empty states with no logic it creates a new StateBase for optimal performance.
		/// </remarks>
		/// <inheritdoc cref="State{TStateId, TEvent}(Action{State{TStateId, TEvent}}, Action{State{TStateId, TEvent}},
		/// 	Action{State{TStateId, TEvent}}, Func{State{TStateId, TEvent}, bool}, bool, bool)"/>
		public static void AddState<TOwnId, TStateId, TEvent>(
			this StateMachine<TOwnId, TStateId, TEvent> fsm,
			TStateId name,
			Action<State<TStateId, TEvent>> onEnter = null,
			Action<State<TStateId, TEvent>> onLogic = null,
			Action<State<TStateId, TEvent>> onExit = null,
			Func<State<TStateId, TEvent>, bool> canExit = null,
			bool needsExitTime = false,
			bool isGhostState = false)
		{
			// Optimise for empty states
			if (onEnter == null && onLogic == null && onExit == null && canExit == null)
			{
				fsm.AddState(name, new StateBase<TStateId>(needsExitTime, isGhostState));
				return;
			}

			fsm.AddState(
				name,
				new State<TStateId, TEvent>(
					onEnter,
					onLogic,
					onExit,
					canExit,
					needsExitTime: needsExitTime,
					isGhostState: isGhostState
				)
			);
		}

		/// <summary>
		/// Creates the most efficient transition type possible for the given parameters.
		/// It creates a Transition instance when a condition or transition callbacks are specified,
		/// otherwise it returns a TransitionBase.
		/// </summary>
		private static TransitionBase<TStateId> CreateOptimizedTransition<TStateId>(
			TStateId from,
			TStateId to,
			Func<Transition<TStateId>, bool> condition = null,
			Action<Transition<TStateId>> onTransition = null,
			Action<Transition<TStateId>> afterTransition = null,
			bool forceInstantly = false)
		{
			if (condition == null && onTransition == null && afterTransition == null)
				return new TransitionBase<TStateId>(from, to, forceInstantly);

			return new Transition<TStateId>(
				from,
				to,
				condition,
				onTransition: onTransition,
				afterTransition: afterTransition,
				forceInstantly: forceInstantly
			);
		}

		/// <summary>
		/// Shortcut method for adding a regular transition.
		/// It creates a new Transition() instance under the hood. => See Transition for more information.
		/// </summary>
		/// <remarks>
		/// When no condition or callbacks are required, it creates a TransitionBase for optimal performance,
		/// otherwise a Transition object.
		/// </remarks>
		/// <inheritdoc cref="Transition{TStateId}(TStateId, TStateId, Func{Transition{TStateId}, bool},
		/// 	Action{Transition{TStateId}}, Action{Transition{TStateId}}, bool)" />
		public static void AddTransition<TOwnId, TStateId, TEvent>(
			this StateMachine<TOwnId, TStateId, TEvent> fsm,
			TStateId from,
			TStateId to,
			Func<Transition<TStateId>, bool> condition = null,
			Action<Transition<TStateId>> onTransition = null,
			Action<Transition<TStateId>> afterTransition = null,
			bool forceInstantly = false)
		{
			fsm.AddTransition(CreateOptimizedTransition(
				from,
				to,
				condition,
				onTransition: onTransition,
				afterTransition: afterTransition,
				forceInstantly: forceInstantly
			));
		}

		/// <summary>
		/// Shortcut method for adding a regular transition that can happen from any state.
		/// It creates a new Transition() instance under the hood. => See Transition for more information.
		/// </summary>
		/// <remarks>
		/// When no condition or callbacks are required, it creates a TransitionBase for optimal performance,
		/// otherwise a Transition object.
		/// </remarks>
		/// <inheritdoc cref="Transition{TStateId}(TStateId, TStateId, Func{Transition{TStateId}, bool},
		/// 	Action{Transition{TStateId}}, Action{Transition{TStateId}}, bool)" />
		public static void AddTransitionFromAny<TOwnId, TStateId, TEvent>(
			this StateMachine<TOwnId, TStateId, TEvent> fsm,
			TStateId to,
			Func<Transition<TStateId>, bool> condition = null,
			Action<Transition<TStateId>> onTransition = null,
			Action<Transition<TStateId>> afterTransition = null,
			bool forceInstantly = false)
		{
			fsm.AddTransitionFromAny(CreateOptimizedTransition(
				default,
				to,
				condition,
				onTransition: onTransition,
				afterTransition: afterTransition,
				forceInstantly: forceInstantly
			));
		}

		/// <summary>
		/// Shortcut method for adding a new trigger transition between two states that is only checked
		/// when the specified trigger is activated.
		/// It creates a new Transition() instance under the hood. => See Transition for more information.
		/// </summary>
		/// <remarks>
		/// When no condition or callbacks are required, it creates a TransitionBase for optimal performance,
		/// otherwise a Transition object.
		/// </remarks>
		/// <inheritdoc cref="Transition{TStateId}(TStateId, TStateId, Func{Transition{TStateId}, bool},
		/// 	Action{Transition{TStateId}}, Action{Transition{TStateId}}, bool)" />
		public static void AddTriggerTransition<TOwnId, TStateId, TEvent>(
			this StateMachine<TOwnId, TStateId, TEvent> fsm,
			TEvent trigger,
			TStateId from,
			TStateId to,
			Func<Transition<TStateId>, bool> condition = null,
			Action<Transition<TStateId>> onTransition = null,
			Action<Transition<TStateId>> afterTransition = null,
			bool forceInstantly = false)
		{
			fsm.AddTriggerTransition(trigger, CreateOptimizedTransition(
				from,
				to,
				condition,
				onTransition: onTransition,
				afterTransition: afterTransition,
				forceInstantly: forceInstantly
			));
		}

		/// <summary>
		/// Shortcut method for adding a new trigger transition that can happen from any possible state, but is only
		/// checked when the specified trigger is activated.
		/// It creates a new Transition() instance under the hood. => See Transition for more information.
		/// </summary>
		/// <remarks>
		/// When no condition or callbacks are required, it creates a TransitionBase for optimal performance,
		/// otherwise a Transition object.
		/// </remarks>
		/// <inheritdoc cref="Transition{TStateId}(TStateId, TStateId, Func{Transition{TStateId}, bool},
		/// 	Action{Transition{TStateId}}, Action{Transition{TStateId}}, bool)" />
		public static void AddTriggerTransitionFromAny<TOwnId, TStateId, TEvent>(
			this StateMachine<TOwnId, TStateId, TEvent> fsm,
			TEvent trigger,
			TStateId to,
			Func<Transition<TStateId>, bool> condition = null,
			Action<Transition<TStateId>> onTransition = null,
			Action<Transition<TStateId>> afterTransition = null,
			bool forceInstantly = false)
		{
			fsm.AddTriggerTransitionFromAny(trigger, CreateOptimizedTransition(
				default,
				to,
				condition,
				onTransition: onTransition,
				afterTransition: afterTransition,
				forceInstantly: forceInstantly
			));
		}

		/// <summary>
		/// Shortcut method for adding two transitions:
		/// If the condition function is true, the fsm transitions from the "from"
		/// state to the "to" state. Otherwise it performs a transition in the opposite direction,
		/// i.e. from "to" to "from".
		/// </summary>
		/// <remarks>
		/// For the reverse transition the afterTransition callback is called before the transition
		/// and the onTransition callback afterwards. If this is not desired then replicate the behaviour
		/// of the two way transitions by creating two separate transitions.
		/// </remarks>
		/// <inheritdoc cref="Transition{TStateId}(TStateId, TStateId, Func{Transition{TStateId}, bool},
		/// 	Action{Transition{TStateId}}, Action{Transition{TStateId}}, bool)" />
		public static void AddTwoWayTransition<TOwnId, TStateId, TEvent>(
			this StateMachine<TOwnId, TStateId, TEvent> fsm,
			TStateId from,
			TStateId to,
			Func<Transition<TStateId>, bool> condition,
			Action<Transition<TStateId>> onTransition = null,
			Action<Transition<TStateId>> afterTransition = null,
			bool forceInstantly = false)
		{
			fsm.AddTwoWayTransition(new Transition<TStateId>(
				from,
				to,
				condition,
				onTransition: onTransition,
				afterTransition: afterTransition,
				forceInstantly: forceInstantly
			));
		}

		/// <summary>
		/// Shortcut method for adding two transitions that are only checked when the specified trigger is activated:
		/// If the condition function is true, the fsm transitions from the "from"
		/// state to the "to" state. Otherwise it performs a transition in the opposite direction,
		/// i.e. from "to" to "from".
		/// </summary>
		/// <remarks>
		/// For the reverse transition the afterTransition callback is called before the transition
		/// and the onTransition callback afterwards. If this is not desired then replicate the behaviour
		/// of the two way transitions by creating two separate transitions.
		/// </remarks>
		/// <inheritdoc cref="Transition{TStateId}(TStateId, TStateId, Func{Transition{TStateId}, bool},
		/// 	Action{Transition{TStateId}}, Action{Transition{TStateId}}, bool)" />
		public static void AddTwoWayTriggerTransition<TOwnId, TStateId, TEvent>(
			this StateMachine<TOwnId, TStateId, TEvent> fsm,
			TEvent trigger,
			TStateId from,
			TStateId to,
			Func<Transition<TStateId>, bool> condition,
			Action<Transition<TStateId>> onTransition = null,
			Action<Transition<TStateId>> afterTransition = null,
			bool forceInstantly = false)
		{
			fsm.AddTwoWayTriggerTransition(trigger, new Transition<TStateId>(
				from,
				to,
				condition,
				onTransition: onTransition,
				afterTransition: afterTransition,
				forceInstantly: forceInstantly
			));
		}

		/// <summary>
		/// Shortcut method for adding a new exit transition from a state.
		/// It represents an exit point that allows the fsm to exit and the parent fsm to continue to the next state.
		/// It is only checked if the parent fsm has a pending transition.
		/// </summary>
		/// <remarks>
		/// When no condition or callbacks are required, it creates a TransitionBase for optimal performance,
		/// otherwise a Transition object.
		/// </remarks>
		/// <inheritdoc cref="Transition{TStateId}(TStateId, TStateId, Func{Transition{TStateId}, bool},
		/// 	Action{Transition{TStateId}}, Action{Transition{TStateId}}, bool)" />
		public static void AddExitTransition<TOwnId, TStateId, TEvent>(
			this StateMachine<TOwnId, TStateId, TEvent> fsm,
			TStateId from,
			Func<Transition<TStateId>, bool> condition = null,
			Action<Transition<TStateId>> onTransition = null,
			Action<Transition<TStateId>> afterTransition = null,
			bool forceInstantly = false)
		{
			fsm.AddExitTransition(CreateOptimizedTransition(
				from,
				default,
				condition,
				onTransition: onTransition,
				afterTransition: afterTransition,
				forceInstantly: forceInstantly
			));
		}

		/// <summary>
		/// Shortcut method for adding a new exit transition that can happen from any state.
		/// It represents an exit point that allows the fsm to exit and the parent fsm to continue to the next state.
		/// It is only checked if the parent fsm has a pending transition.
		/// </summary>
		/// <remarks>
		/// When no condition is required, it creates a TransitionBase for optimal performance,
		/// otherwise a Transition object.
		/// </remarks>
		/// <inheritdoc cref="Transition{TStateId}(TStateId, TStateId, Func{Transition{TStateId}, bool},
		/// 	Action{Transition{TStateId}}, Action{Transition{TStateId}}, bool)" />
		public static void AddExitTransitionFromAny<TOwnId, TStateId, TEvent>(
			this StateMachine<TOwnId, TStateId, TEvent> fsm,
			Func<Transition<TStateId>, bool> condition = null,
			Action<Transition<TStateId>> onTransition = null,
			Action<Transition<TStateId>> afterTransition = null,
			bool forceInstantly = false)
		{
			fsm.AddExitTransitionFromAny(CreateOptimizedTransition(
				default,
				default,
				condition,
				onTransition: onTransition,
				afterTransition: afterTransition,
				forceInstantly: forceInstantly
			));
		}

		/// <summary>
		/// Shortcut method for adding a new exit transition from a state that is only checked when the
		/// specified trigger is activated.
		/// It represents an exit point that allows the fsm to exit and the parent fsm to continue to the next state.
		/// It is only checked if the parent fsm has a pending transition.
		/// </summary>
		/// <remarks>
		/// When no condition is required, it creates a TransitionBase for optimal performance,
		/// otherwise a Transition object.
		/// </remarks>
		/// <inheritdoc cref="Transition{TStateId}(TStateId, TStateId, Func{Transition{TStateId}, bool},
		/// 	Action{Transition{TStateId}}, Action{Transition{TStateId}}, bool)" />
		public static void AddExitTriggerTransition<TOwnId, TStateId, TEvent>(
			this StateMachine<TOwnId, TStateId, TEvent> fsm,
			TEvent trigger,
			TStateId from,
			Func<Transition<TStateId>, bool> condition = null,
			Action<Transition<TStateId>> onTransition = null,
			Action<Transition<TStateId>> afterTransition = null,
			bool forceInstantly = false)
		{
			fsm.AddExitTriggerTransition(
				trigger,
				CreateOptimizedTransition(
					from,
					default,
					condition,
					onTransition: onTransition,
					afterTransition: afterTransition,
					forceInstantly: forceInstantly
				)
			);
		}

		/// <summary>
		/// Shortcut method for adding a new exit transition from a state that can happen from any possible state
		/// and is only checked when the specified trigger is activated.
		/// It represents an exit point that allows the fsm to exit and the parent fsm to continue to the next state.
		/// It is only checked if the parent fsm has a pending transition.
		/// </summary>
		/// <remarks>
		/// When no condition is required, it creates a TransitionBase for optimal performance,
		/// otherwise a Transition object.
		/// </remarks>
		/// <inheritdoc cref="Transition{TStateId}(TStateId, TStateId, Func{Transition{TStateId}, bool},
		/// 	Action{Transition{TStateId}}, Action{Transition{TStateId}}, bool)" />
		public static void AddExitTriggerTransitionFromAny<TOwnId, TStateId, TEvent>(
			this StateMachine<TOwnId, TStateId, TEvent> fsm,
			TEvent trigger,
			Func<Transition<TStateId>, bool> condition = null,
			Action<Transition<TStateId>> onTransition = null,
			Action<Transition<TStateId>> afterTransition = null,
			bool forceInstantly = false)
		{
			fsm.AddExitTriggerTransitionFromAny(
				trigger,
				CreateOptimizedTransition(
					default,
					default,
					condition,
					onTransition: onTransition,
					afterTransition: afterTransition,
					forceInstantly: forceInstantly
				)
			);
		}
	}
}
