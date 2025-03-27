#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Animations;
using UnityEditor;
using UnityEngine;
using UnityHFSM.Inspection;

namespace UnityHFSM.Visualization
{
/// <summary>
/// A visualisation / debugging tool that allows you to generate a Unity <see cref="AnimatorController"/>
/// that reflects the structure of a given hierarchical state machine. This gives you a visual representation
/// of a hierarchy that you can view in the editor.
/// It can also be configured to show you which state is active at runtime (live preview).
/// </summary>
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

	/// <summary>
	/// Helper class that creates unique names for each state, avoiding naming collisions. It is necessary,
	/// as the live previewer feature, which uses an Animator component, needs to supply the name of the active
	/// state. If the state names are not unique, the animator may show the wrong state as being active.
	/// </summary>
	private class StateNamingInformation
	{
		private readonly Dictionary<StateMachinePath, string> namesByState
			= new Dictionary<StateMachinePath, string>();

		private readonly HashSet<string> usedNames = new HashSet<string>();

		public string CreateUniqueNameForState(StateMachinePath path)
		{
			string name = path.LastNodeName;

			int counter = 0;
			string uniqueName = name;
			while (usedNames.Contains(uniqueName))
			{
				uniqueName = name + "'" + counter;
				counter++;
			}

			usedNames.Add(uniqueName);
			namesByState[path] = uniqueName;
			return uniqueName;
		}

		public string GetNameForState(StateMachinePath path)
		{
			if (!namesByState.TryGetValue(path, out string name))
			{
				Debug.LogError($"Cannot find string name of state {path}.");
				return path.LastNodeName;
			}

			return name;
		}
	}

	/// <summary>
	/// Recursively walks through the state machine, setting up a Unity <see cref="AnimatorController"/> to
	/// reflect the states and structure of the hierarchy.
	/// </summary>
	private class AnimatorGraphGenerator : IStateMachineHierarchyVisitor
	{
		// Used to suppress Animator warnings about transitions not having transition conditions.
		public const string dummyProperty = "See Code";

		private readonly Dictionary<StateMachinePath, AnimatorStateMachine> animatorStateMachines
			= new Dictionary<StateMachinePath, AnimatorStateMachine>();

		private readonly Dictionary<StateMachinePath, AnimatorState> animatorStates
			= new Dictionary<StateMachinePath, AnimatorState>();

		private readonly HashSet<StateMachinePath> startStates
			= new HashSet<StateMachinePath>();

		public readonly StateNamingInformation stateNamingInformation;

		public AnimatorGraphGenerator(AnimatorStateMachine rootStateMachine)
		{
			animatorStateMachines.Add(StateMachinePath.Root, rootStateMachine);
			stateNamingInformation = new StateNamingInformation();
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

			string name = stateNamingInformation.CreateUniqueNameForState(statePath);
			var animatorState = animator.AddState(name);

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
	/// Enables a live preview of a state machine via an animator. It sets the active state of the animator
	/// (which can be viewed in the Unity editor) to the active state of the state machine.
	/// Using this type is the recommended way to implement a live preview via an animator, as it can deal
	/// with naming collisions between states in different nested state machines.
	/// </summary>
	/// <remarks>
	/// Although the previewer is bound to one state machine instance, it can also be used to display
	/// other state machine instances with the same structure in an animator.
	/// </remarks>
	public interface IPreviewer
	{
		public void PreviewStateMachineInAnimator<TOwnId, TStateId, TEvent>(
			StateMachine<TOwnId, TStateId, TEvent> fsm,
			Animator animator);

		public void PreviewStateMachineInAnimator(Animator animator);
	}

	// Default implementation of the IPreviewer type. An interface is used in order to better encapsulate
	// and hide the implementation details of the previewer. Amongst other things, this choice also makes the
	// IPreviewer type less cumbersome to use in code, as the user does not have to provide the generic type
	// parameters used by the internal state machine instance.
	/// <inheritdoc cref="IPreviewer"/>
	private class Previewer<TFsmOwnId, TFsmStateId, TFsmEvent> : IPreviewer
	{
		private readonly StateMachine<TFsmOwnId, TFsmStateId, TFsmEvent> originalFsm;
		private readonly StateNamingInformation stateNamingInformation;

		public Previewer(
			StateMachine<TFsmOwnId, TFsmStateId, TFsmEvent> fsm,
			StateNamingInformation stateNamingInformation)
		{
			this.originalFsm = fsm;
			this.stateNamingInformation = stateNamingInformation;
		}

		public void PreviewStateMachineInAnimator(Animator animator)
		{
			PreviewStateMachineInAnimator(originalFsm, animator);
		}

		public void PreviewStateMachineInAnimator<TOwnId, TStateId, TEvent>(
			StateMachine<TOwnId, TStateId, TEvent> fsm,
			Animator animator)
		{
			StateMachinePath path = StateMachineWalker.GetActiveStatePath(fsm);
			string activeStateName = stateNamingInformation.GetNameForState(path);

			int hashCode = Animator.StringToHash(activeStateName);
			if (animator.HasState(0, hashCode))
			{
				animator.Play(hashCode);
			}
		}
	}

	/// <summary>
	/// Creates a new <see cref="AnimatorController"/> for a given hierarchical finite state machine. The animator is
	/// written to a file in the Assets folder (see parameters for customisation), which can also be viewed
	/// in the editor outside the play mode.
	/// If the animator file already exists from a previous call, the user-defined positions of states (layout)
	/// will be preserved.
	/// </summary>
	/// <remarks>
	/// Only call this method when the state machine has been fully set up (start state, states, transitions).
	/// </remarks>
	/// <returns>
	/// Returns the generated <see cref="AnimatorController"/> and an <see cref="IPreviewer"/> object that can
	/// be used for live preview.
	/// </returns>
	public static (AnimatorController, IPreviewer) CreateAnimatorFromStateMachine<TOwnId, TStateId, TEvent>(
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
		StateNamingInformation stateNamingInformation;
		if (animator == null)
		{
			(animator, stateNamingInformation) = CreateNewAnimatorFromStateMachine(fsm, fullPathToDebugAnimator);
		}
		else
		{
			stateNamingInformation = UpdateExistingAnimatorFromStateMachine(animator, fsm);
		}

		return (animator, new Previewer<TOwnId, TStateId, TEvent>(fsm, stateNamingInformation));
	}

	/// <summary>
	/// Enables a live preview of a state machine via an animator. It sets the active state of the animator
	/// (which can be viewed in the Unity editor) to the active state of the state machine.
	/// </summary>
	/// <remarks>
	/// This is a convenience method and comes with a caveat: If two child states in different nested state machines
	/// have the same name, then the preview will not be correct. If possible, the <see cref="IPreviewer"/> returned
	/// by <see cref="CreateAnimatorFromStateMachine{TOwnId,TStateId,TEvent}"/> should be used, as it does not
	/// suffer from this problem.
	/// </remarks>
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

	private static (AnimatorController, StateNamingInformation) CreateNewAnimatorFromStateMachine<TOwnId, TStateId, TEvent>(
		StateMachine<TOwnId, TStateId, TEvent> fsm,
		string outputPath)
	{
		var animator = AnimatorController.CreateAnimatorControllerAtPath(outputPath);
		var stateNamingInformation = UpdateExistingAnimatorFromStateMachine(animator, fsm);
		return (animator, stateNamingInformation);
	}

	private static StateNamingInformation UpdateExistingAnimatorFromStateMachine<TOwnId, TStateId, TEvent>(
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

		return generator.stateNamingInformation;
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
