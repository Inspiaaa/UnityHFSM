using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Animations;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace UnityHFSM.Visualization
{
	public static class HfsmAnimatorGraph
	{
		/// <summary>
		/// Prints the animator states and transitions to an Animator for easy viewing.
		/// Only call this after all states and transitions have been added!
		/// </summary>
		/// <param name="pathToCreateDebugAnimator">
		///		Leave this empty if you want to use the default path of Assets/DebugAnimators/</param>
		public static void PrintToAnimator<TOwnId, TStateId, TEvent>(
			this StateMachine<TOwnId, TStateId, TEvent> hfsm,
			string pathToFolderForDebugAnimator = "",
			string animatorName = "StateMachineDebugger.controller")
		{
			if (hfsm.stateBundlesByName.Count == 0)
			{
				Debug.LogError(
					"Trying to print an empty HFSM. You probably forgot to add the states and transitions before calling this method.");
				return;
			}

			if (!animatorName.Contains(".controller"))
			{
				animatorName = string.Concat(animatorName, ".controller");
			}

			if (pathToFolderForDebugAnimator == "")
				pathToFolderForDebugAnimator = Path.Combine("Assets", "DebugAnimators" + Path.DirectorySeparatorChar);

			if (!Directory.Exists(pathToFolderForDebugAnimator))
				Directory.CreateDirectory(pathToFolderForDebugAnimator);

			var fullPathToDebugAnimator = Path.Combine(pathToFolderForDebugAnimator, animatorName);

			var animatorMirror = AssetDatabase.LoadAssetAtPath<AnimatorController>(fullPathToDebugAnimator);
			if (animatorMirror == null)
				animatorMirror = AnimatorController.CreateAnimatorControllerAtPath(fullPathToDebugAnimator);

			// Suppress Animator warnings about transitions not having transition conditions.
			animatorMirror.parameters = new AnimatorControllerParameter[0];
			animatorMirror.AddParameter(AnimatorExtensions.globalParameterName, AnimatorControllerParameterType.Bool);

			// Remove old transitions from state machine before setting it up freshly.
			RemoveTransitionsFromStateMachine(animatorMirror.layers[0].stateMachine);

			SetupAnimatorStateMachine(animatorMirror.layers[0].stateMachine, hfsm, new(), new());
		}

		// Sets up an AnimatorStateMachine based upon the HFSM supplied as a parameter.
		// Called recursively when entering a sub-state of an HFSM.
		private static void SetupAnimatorStateMachine<TOwnId, TStateId, TEvent>(
			AnimatorStateMachine animatorStateMachine,
			StateMachine<TOwnId, TStateId, TEvent> hfsm,
			Dictionary<TStateId, AnimatorState> animatorStateDict,
			Dictionary<TStateId, AnimatorStateMachine> animatorStateMachineDict)
		{
			// Add Animator states mirroring HFSM states.
			foreach (StateBase<TStateId> state in hfsm.stateBundlesByName.Values?.Select(bundle => bundle.state))
			{
				if (state is StateMachine<TOwnId, TStateId, TEvent> subFsm)
					AddStateMachineToAnimator(subFsm, state.name, animatorStateMachine, animatorStateMachineDict,
						animatorStateDict);
				else
					AddStateToAnimator(state, hfsm, animatorStateMachine, animatorStateDict);
			}

			RemoveTransitionsFromStateMachine(animatorStateMachine); // Remove all transitions so that they can be re-placed.

			// Add transitions to Animator which mirror transitions in the HFSM.
			// This cannot be in the same loop as above because the state which is receiving a
			// transition might not have been created yet.
			foreach (StateMachine<TOwnId, TStateId, TEvent>.StateBundle stateBundle in hfsm.stateBundlesByName.Values)
			{
				if (stateBundle.state is StateMachine<TOwnId, TStateId, TEvent> subFsm)
					AddStateMachineTransitionsToAnimator(stateBundle, subFsm, animatorStateMachineDict, animatorStateDict);
				else
				{
					RemoveTransitionsFromState(
						animatorStateDict
							[stateBundle.state.name]); //remove existing transitions so that they can be replaced
					AddStateTransitionsToAnimator(stateBundle, animatorStateDict, animatorStateMachineDict);
				}
			}

			// Trigger transitions are treated exactly the same as normal transitions,
			// so concatenate them into one IEnumerable.
			hfsm.transitionsFromAny
				.Concat(hfsm.triggerTransitionsFromAny.Values.SelectMany(x => x))
				.ForEach(transition
					=> animatorStateMachine.AddTransitionFromAnyStateWithCondition(animatorStateDict[transition.to]));
		}

		private static void AddStateTransitionsToAnimator<TOwnId, TStateId, TEvent>(
			StateMachine<TOwnId, TStateId, TEvent>.StateBundle stateBundle,
			Dictionary<TStateId, AnimatorState> animatorStateDict,
			Dictionary<TStateId, AnimatorStateMachine> animatorStateMachineDict)
		{
			var fromState = animatorStateDict[stateBundle.state.name];

			foreach (var transition in stateBundle.transitions ?? Enumerable.Empty<TransitionBase<TStateId>>())
			{
				if (animatorStateDict.ContainsKey(transition.to))
				{
					fromState.AddTransitionToStateWithCondition(animatorStateDict[transition.to]);
				}
				else // If the destination is not a state, then it must be a state machine.
				{
					fromState.AddTransitionToStateMachineWithCondition(animatorStateMachineDict[transition.to]);
				}
			}

			foreach (var transition in stateBundle.triggerToTransitions?.Values?.SelectMany(x => x)
			         ?? Enumerable.Empty<TransitionBase<TStateId>>())
			{
				fromState.AddTransitionToStateWithCondition(animatorStateDict[transition.to]);
			}
		}

		// Removes transitions between a state and other states.
		private static void RemoveTransitionsFromState(AnimatorState state)
		{
			foreach (AnimatorStateTransition animatorTransition in state.transitions)
				Undo.DestroyObjectImmediate(animatorTransition);

			state.transitions = new AnimatorStateTransition[0];
		}

		// This removes entry and any-state transitions to and from the state machine itself.
		// It does not remove transitions between states.
		private static void RemoveTransitionsFromStateMachine(AnimatorStateMachine animatorStateMachine)
		{
			// Remove all entry transitions.
			foreach (AnimatorTransition animatorTransition in animatorStateMachine.entryTransitions)
				Undo.DestroyObjectImmediate(animatorTransition);

			animatorStateMachine.entryTransitions = new AnimatorTransition[0];

			// Remove any-state transitions.
			foreach (AnimatorStateTransition animatorTransition in animatorStateMachine.anyStateTransitions)
				Undo.DestroyObjectImmediate(animatorTransition);

			animatorStateMachine.anyStateTransitions = new AnimatorStateTransition[0];
		}

		// Adds transitions within a nested StateMachine.
		private static void AddStateMachineTransitionsToAnimator<TOwnId, TStateId, TEvent>(
			StateMachine<TOwnId, TStateId, TEvent>.StateBundle stateBundle,
			StateMachine<TOwnId, TStateId, TEvent> subFsm,
			Dictionary<TStateId, AnimatorStateMachine> animatorStatemachineDict,
			Dictionary<TStateId, AnimatorState> animatorStateDict)
		{
			AnimatorStateMachine animatorStateMachine = animatorStatemachineDict[stateBundle.state.name];

			// Add transitionsFromAny and triggerTransitionsFromAny. Both are represented as AnyStateTransitions
			// in the Animator.
			IEnumerable<TransitionBase<TStateId>> subFsmTransitionsFromAny =
				subFsm.transitionsFromAny.Concat(subFsm.triggerTransitionsFromAny.Values.SelectMany(x => x));
			foreach (var transition in subFsmTransitionsFromAny)
				animatorStateMachine.AddTransitionFromAnyStateWithCondition(animatorStateDict[transition.to]);

			// Trigger transitions are treated exactly the same as normal transitions, so concatenate them
			// into one IEnumerable.
			IEnumerable<TransitionBase<TStateId>> transitionsFromAny = stateBundle.transitions;
			if (stateBundle.triggerToTransitions != null)
				transitionsFromAny.Concat(stateBundle.triggerToTransitions.Values.SelectMany(x => x));

			foreach (var transition in transitionsFromAny)
			{
				// AnimatorStateMachine is not interchangeable with AnimatorState, so we must check each dictionary separately.
				if (animatorStatemachineDict.ContainsKey(transition.to))
				{
					animatorStateMachine.AddTransitionToStateMachineWithCondition(animatorStatemachineDict[transition.to]);
				}
				else // If the destination is not a state machine, then it must be a state.
				{
					animatorStateMachine.AddTransitionToStateWithCondition(animatorStateDict[transition.to]);
				}
			}
		}

		private static void AddStateToAnimator<TOwnId, TStateId, TEvent>(
			StateBase<TStateId> stateToAdd,
			StateMachine<TOwnId, TStateId, TEvent> parentFSM,
			AnimatorStateMachine animatorStateMachine,
			Dictionary<TStateId, AnimatorState> animatorStateDict)
		{
			// Search to see if the state machine contains a state with the same name as the stateToAdd.
			var (foundStateWithSameName, foundChildState) =
				animatorStateMachine.states.FirstOrFalse(state => state.state.name == stateToAdd.name.ToString());

			if (!foundStateWithSameName)
				foundChildState.state = animatorStateMachine.AddState(stateToAdd.name.ToString());

			// If the parent fsm doesn't have a start state or we are the start state, then make this state
			// the default in the animator.
			if (parentFSM.startState.hasState == false
			    || parentFSM.startState.state.ToString() == stateToAdd.name.ToString())
				animatorStateMachine.defaultState = foundChildState.state;

			animatorStateDict.Add(stateToAdd.name, foundChildState.state);
		}

		private static void AddStateMachineToAnimator<TOwnId, TStateId, TEvent>(
			StateMachine<TOwnId, TStateId, TEvent> subFsm,
			TStateId stateMachineName,
			AnimatorStateMachine parentAnimatorStateMachine,
			Dictionary<TStateId, AnimatorStateMachine> stateMachineDictionary,
			Dictionary<TStateId, AnimatorState> animatorStateDict)
		{
			// Search to see if the state machine contains a child state machine with the same name as stateToAdd.
			var (didFindStateMachine, childStateMachine) =
				parentAnimatorStateMachine.stateMachines.FirstOrFalse(childStateMachine
					=> childStateMachine.stateMachine.name == subFsm.name.ToString());

			if (!didFindStateMachine)
				childStateMachine.stateMachine = parentAnimatorStateMachine.AddStateMachine(subFsm.name.ToString());

			stateMachineDictionary.Add(stateMachineName, childStateMachine.stateMachine);

			SetupAnimatorStateMachine(childStateMachine.stateMachine, subFsm, animatorStateDict, stateMachineDictionary);
		}

		private static (bool didFind, T element) FirstOrFalse<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
		{
			foreach (T element in collection)
			{
				if (predicate(element))
					return (true, element);
			}

			return (false, default(T));
		}

		private static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			foreach (T t in source)
				action.Invoke(t);
		}
	}

	public static class AnimatorExtensions
	{
		// Used to suppress Animator warnings about transitions not having transition conditions.
		public const string globalParameterName = "Generated Parameter";

		public static void AddTransitionFromAnyStateWithCondition(
			this AnimatorStateMachine fromStateMachine,
			AnimatorState destinationState)
		{
			AnimatorStateTransition animatorTransition = fromStateMachine.AddAnyStateTransition(destinationState);
			animatorTransition.AddCondition(AnimatorConditionMode.If, 1f, globalParameterName);
		}

		public static void AddTransitionToStateMachineWithCondition(
			this AnimatorStateMachine fromStateMachine,
			AnimatorStateMachine destinationStateMachine)
		{
			AnimatorTransition animatorTransition = fromStateMachine.AddStateMachineTransition(destinationStateMachine);
			animatorTransition.AddCondition(AnimatorConditionMode.If, 1f, globalParameterName);
		}

		public static void AddTransitionToStateWithCondition(
			this AnimatorStateMachine fromStateMachine,
			AnimatorState destinationState)
		{
			AnimatorTransition animatorTransition =
				fromStateMachine.AddStateMachineTransition(fromStateMachine, destinationState);
			animatorTransition.AddCondition(AnimatorConditionMode.If, 1f, globalParameterName);
		}

		public static void AddTransitionToStateWithCondition(this AnimatorState fromState, AnimatorState destinationState)
		{
			AnimatorStateTransition animatorTransition = fromState.AddTransition(destinationState);
			animatorTransition.AddCondition(AnimatorConditionMode.If, 1f, globalParameterName);
		}

		public static void AddTransitionToStateMachineWithCondition(
			this AnimatorState fromState,
			AnimatorStateMachine destinationStateMachine)
		{
			AnimatorStateTransition animatorTransition = fromState.AddTransition(destinationStateMachine);
			animatorTransition.AddCondition(AnimatorConditionMode.If, 1f, globalParameterName);
		}
	}
}
#endif
