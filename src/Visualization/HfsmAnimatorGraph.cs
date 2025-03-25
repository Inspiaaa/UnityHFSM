using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Animations;
using UnityEditor;
using UnityEngine;
using UnityHFSM.Inspection;

#if UNITY_EDITOR
namespace UnityHFSM.Visualization
{
public static class HfsmAnimatorGraph
{
	/// <summary>
	/// Helper class that records the current layout of the animator states so that it can be restored
	/// once the animator graph has been regenerated.
	/// </summary>
	private class StateMachinePositionInformation
	{
		// Positions of the "special" states within this state machine.
		private readonly Vector3 entryPosition;
		private readonly Vector3 exitPosition;
		private readonly Vector3 anyStatePosition;
		private readonly Vector3 parentStateMachinePosition;

		// Positions of child states and child state machines.
		private readonly Dictionary<string, Vector3> statePositions
			= new Dictionary<string, Vector3>();
		private readonly Dictionary<string, StateMachinePositionInformation> childStateMachines
			= new Dictionary<string, StateMachinePositionInformation>();

		private StateMachinePositionInformation(AnimatorStateMachine animator)
		{
			entryPosition = animator.entryPosition;
			exitPosition = animator.exitPosition;
			anyStatePosition = animator.anyStatePosition;
			parentStateMachinePosition = animator.parentStateMachinePosition;

			foreach (var child in animator.states)
			{
				statePositions[child.state.name] = child.position;
			}

			foreach (var child in animator.stateMachines)
			{
				statePositions[child.stateMachine.name] = child.position;
				childStateMachines[child.stateMachine.name] =
					new StateMachinePositionInformation(child.stateMachine);
			}
		}

		public static StateMachinePositionInformation ExtractFromAnimator(AnimatorStateMachine animator)
		{
			return new StateMachinePositionInformation(animator);
		}

		/// <summary>
		/// Recursively updates the positions of the states in the given animator state machine to match
		/// the positions specified by this object. If no information is known, the current position is
		/// left as-is.
		/// </summary>
		public void Apply(AnimatorStateMachine animator)
		{
			animator.entryPosition = entryPosition;
			animator.exitPosition = exitPosition;
			animator.anyStatePosition = anyStatePosition;
			animator.parentStateMachinePosition = parentStateMachinePosition;

			// Overwriting the states / stateMachines arrays is the only way to update the positions.

			animator.states = animator.states
				.Select(state => {
					string name = state.state.name;
					if (statePositions.TryGetValue(name, out Vector3 position))
					{
						state.position = position;
					}
					return state;
				})
				.ToArray();

			animator.stateMachines = animator.stateMachines
				.Select(stateMachine => {
					string name = stateMachine.stateMachine.name;
					if (statePositions.TryGetValue(name, out Vector3 position))
					{
						stateMachine.position = position;
					}
					return stateMachine;
				})
				.ToArray();

			// Recursively update the positions within each nested state machine.
			foreach (var stateMachine in animator.stateMachines)
			{
				string name = stateMachine.stateMachine.name;
				if (childStateMachines.TryGetValue(name, out StateMachinePositionInformation positionInformation))
				{
					positionInformation.Apply(stateMachine.stateMachine);
				}
			}
		}
	}

	private class AnimatorGraphGenerator : IStateMachineHierarchyVisitor
	{
		// Used to suppress Animator warnings about transitions not having transition conditions.
		public const string dummyProperty = "See Code";

		private Dictionary<StateMachinePath, AnimatorStateMachine> animatorStateMachines
			= new Dictionary<StateMachinePath, AnimatorStateMachine>();

		private Dictionary<StateMachinePath, AnimatorState> animatorStates
			= new Dictionary<StateMachinePath, AnimatorState>();

		private HashSet<StateMachinePath> startStates
			= new HashSet<StateMachinePath>();

		public AnimatorGraphGenerator(AnimatorStateMachine rootStateMachine)
		{
			animatorStateMachines.Add(StateMachinePath.Root, rootStateMachine);
		}

		public void VisitStateMachine<TOwnId, TStateId, TEvent>(
			StateMachinePath fsmPath,
			StateMachine<TOwnId, TStateId, TEvent> fsm)
		{
			startStates.Add(new StateMachinePath<TStateId>(fsmPath, fsm.GetStartStateName()));

			if (fsmPath.IsRoot)
				return;

			var animator = animatorStateMachines[fsmPath.parentPath];
			var animatorStateMachine = animator.AddStateMachine(fsm.name.ToString());
			animatorStateMachines[fsmPath] = animatorStateMachine;

			// If this state machine is the start state of its parent state machine, then
			// the entry transition will be set in ExitStateMachine().
			// The child states are added in subsequent VisitRegularState() and VisitStateMachine() calls.
			// Transitions are finally added in the ExitStateMachine() call.
		}

		public void VisitRegularState<TStateId>(StateMachinePath statePath, StateBase<TStateId> state)
		{
			var animator = animatorStateMachines[statePath.parentPath];
			var animatorState = animator.AddState(state.name.ToString());

			animatorStates[statePath] = animatorState;

			if (startStates.Contains(statePath))
			{
				animator.defaultState = animatorState;
			}
		}

		public void ExitStateMachine<TOwnId, TStateId, TEvent>(
			StateMachinePath fsmPath,
			StateMachine<TOwnId, TStateId, TEvent> fsm)
		{
			// Set the start state.
			if (startStates.Contains(fsmPath))
			{
				// As a state machine cannot be the "default state" of an animator state machine, which implicitly
				// comes with the entry transition, we have to instead set the start state to one of the states
				// within the child state machine. In this case, we choose the start state of the child state machine
				// (and if it is also a state machine, then its start state, and so forth...).
				var parentAnimator = animatorStateMachines[fsmPath.parentPath];
				var trueNestedStartState = FindMostNestedChildStartState(fsmPath);
				parentAnimator.defaultState = animatorStates[trueNestedStartState];
			}

			// At this point, all states have been added. Now, we can add the transitions.
			var animator = animatorStateMachines[fsmPath];

			// Normal transitions.
			foreach (var transition in fsm.GetAllTransitions())
			{
				AddTransition(animator, fsmPath, transition);
			}

			// Trigger transitions (treated the same as normal transitions).
			// TODO: Use the trigger (event) as the transition condition instead?
			foreach (var transition in fsm.GetAllTriggerTransitions().Values.SelectMany(list => list))
			{
				AddTransition(animator, fsmPath, transition);
			}

			// Transitions from any.
			foreach (var transition in fsm.GetAllTransitionsFromAny())
			{
				AddTransitionFromAny(animator, fsmPath, transition);
			}

			// Trigger transitions from any.
			foreach (var transition in fsm.GetAllTriggerTransitionsFromAny().Values.SelectMany(list => list))
			{
				AddTransitionFromAny(animator, fsmPath, transition);
			}
		}

		private StateMachinePath FindMostNestedChildStartState(StateMachinePath startStatePath)
		{
			foreach (var state in startStates)
			{
				if (state.IsChildPathOf(startStatePath))
				{
					startStatePath = state;
				}
			}

			return startStatePath;
		}

		private void AddTransition<TStateId>(
			AnimatorStateMachine animator,
			StateMachinePath fsmPath,
			TransitionBase<TStateId> transition)
		{
			var fromPath = new StateMachinePath<TStateId>(fsmPath, transition.from);

			if (transition.isExitTransition)
			{
				AddExitTransition(animator, fromPath);
			}
			else
			{
				var toPath = new StateMachinePath<TStateId>(fsmPath, transition.to);
				AddTransition(animator, fromPath, toPath);
			}
		}

		private void AddExitTransition(AnimatorStateMachine animator, StateMachinePath fromPath)
		{
			if (animatorStates.TryGetValue(fromPath, out AnimatorState state))
			{
				SetupAnimatorTransition(state.AddExitTransition());
			}
			else if (animatorStateMachines.TryGetValue(fromPath, out AnimatorStateMachine childStateMachine))
			{
				SetupAnimatorTransition(animator.AddStateMachineExitTransition(childStateMachine));
			}
		}

		private void AddTransition(AnimatorStateMachine animator, StateMachinePath fromPath, StateMachinePath toPath)
		{
			var fromState = animatorStates.GetValueOrDefault(fromPath, null);
			var fromStateMachine = animatorStateMachines.GetValueOrDefault(fromPath, null);

			var toState = animatorStates.GetValueOrDefault(toPath, null);
			var toStateMachine = animatorStateMachines.GetValueOrDefault(toPath, null);

			if (fromState != null)
			{
				SetupAnimatorTransition(toState != null
					? fromState.AddTransition(toState)
					: fromState.AddTransition(toStateMachine));
			}
			else
			{
				SetupAnimatorTransition(toState != null
					? animator.AddStateMachineTransition(fromStateMachine, toState)
					: animator.AddStateMachineTransition(fromStateMachine, toStateMachine));
			}
		}

		private void AddTransitionFromAny<TStateId>(
			AnimatorStateMachine animator,
			StateMachinePath fsmPath,
			TransitionBase<TStateId> transition)
		{
			if (transition.isExitTransition)
			{
				Debug.LogWarning("Exit transitions from any are currently not supported in the animator graph.");
				return;
			}

			var toPath = new StateMachinePath<TStateId>(fsmPath, transition.to);

			if (animatorStates.TryGetValue(toPath, out AnimatorState state))
			{
				SetupAnimatorTransition(animator.AddAnyStateTransition(state));
			}
			else if (animatorStateMachines.TryGetValue(toPath, out AnimatorStateMachine childStateMachine))
			{
				SetupAnimatorTransition(animator.AddAnyStateTransition(childStateMachine));
			}
		}

		private void SetupAnimatorTransition(AnimatorTransition transition)
		{
			transition.AddCondition(AnimatorConditionMode.If, 1f, dummyProperty);
		}

		private void SetupAnimatorTransition(AnimatorStateTransition transition)
		{
			transition.AddCondition(AnimatorConditionMode.If, 1f, dummyProperty);
		}
	};

	/// <summary>
	/// Prints the animator states and transitions to an Animator for easy viewing.
	/// Only call this after all states and transitions have been added!
	/// </summary>
	public static AnimatorController CreateAnimatorFromStateMachine<TOwnId, TStateId, TEvent>(
		StateMachine<TOwnId, TStateId, TEvent> fsm,
		string outputFolderPath = "Assets/DebugAnimators",
		string animatorName = "StateMachineAnimatorGraph.controller")
	{
		if (fsm.GetAllStates().Count == 0)
		{
			throw new InvalidOperationException(UnityHFSM.Exceptions.ExceptionFormatter.Format(
				context: "Generating an animator graph from a state machine.",
				problem: "The state machine is empty.",
				solution: "Only call this method after adding the states and transitions to the state machine."));
		}

		if (!animatorName.Contains(".controller"))
		{
			animatorName = string.Concat(animatorName, ".controller");
		}

		if (!Directory.Exists(outputFolderPath))
			Directory.CreateDirectory(outputFolderPath);

		var fullPathToDebugAnimator = Path.Combine(outputFolderPath, animatorName);

		var animator = AssetDatabase.LoadAssetAtPath<AnimatorController>(fullPathToDebugAnimator);
		if (animator == null)
		{
			animator = CreateNewAnimatorFromStateMachine(fsm, fullPathToDebugAnimator);
		}
		else
		{
			UpdateExistingAnimatorFromStateMachine(animator, fsm);
		}

		return animator;
	}

	public static void PreviewStateMachineInAnimator<TOwnId, TStateId, TEvent>(
		StateMachine<TOwnId, TStateId, TEvent> fsm,
		Animator animator)
	{
		StateMachinePath path = StateMachineWalker.GetActiveStatePath(fsm);
		string activeState = path.LastNodeName;

		int hashCode = Animator.StringToHash(activeState);
		if (animator.HasState(0, hashCode))
		{
			animator.Play(hashCode);
		}
	}

	private static AnimatorController CreateNewAnimatorFromStateMachine<TOwnId, TStateId, TEvent>(
		StateMachine<TOwnId, TStateId, TEvent> fsm,
		string outputPath)
	{
		var animator = AnimatorController.CreateAnimatorControllerAtPath(outputPath);
		UpdateExistingAnimatorFromStateMachine(animator, fsm);
		return animator;
	}

	private static void UpdateExistingAnimatorFromStateMachine<TOwnId, TStateId, TEvent>(
		AnimatorController animator,
		StateMachine<TOwnId, TStateId, TEvent> fsm)
	{
		// Save the current state positions in the animator so that they can be restored after regeneration.
		StateMachinePositionInformation layout = null;
		if (animator.layers.Length > 0)
		{
			layout = StateMachinePositionInformation.ExtractFromAnimator(animator.layers[0].stateMachine);
		}

		// Clear the animator.
		animator.parameters = new AnimatorControllerParameter[0];
		ClearAnimatorLayers(animator);
		var rootStateMachine = animator.layers[0].stateMachine;
		ClearAnimatorStateMachine(rootStateMachine);

		// Introduce a parameter to suppress Animator warnings about transitions not
		// having transition conditions.
		animator.AddParameter(AnimatorGraphGenerator.dummyProperty, AnimatorControllerParameterType.Bool);

		// Add states and transitions.
		var generator = new AnimatorGraphGenerator(rootStateMachine);
		StateMachineWalker.Walk(fsm, generator);

		// Restore the original layout.
		layout?.Apply(rootStateMachine);
	}

	private static void ClearAnimatorLayers(AnimatorController animator)
	{
		for (int i = 0, count = animator.layers.Length - 1; i < count; i++)
		{
			animator.RemoveLayer(1);
		}

		if (animator.layers.Length == 0)
		{
			animator.AddLayer("State Machine");
		}
		else
		{
			animator.layers[0].name = "State Machine";
		}
	}

	private static void ClearAnimatorStateMachine(AnimatorStateMachine animator)
	{
		animator.states = new ChildAnimatorState[0];
		animator.stateMachines = new ChildAnimatorStateMachine[0];
	}
}
}
#endif
