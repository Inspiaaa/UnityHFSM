using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Animations;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace UnityHFSM
{
	public static class EditorStateMachineShortcuts
	{
		/// <summary>
		/// Prints the animator states and transitions to an Animator for easy viewing. Only call this after all states and transitions have been added!
		/// </summary>
		/// <param name="pathToCreateDebugAnimator">Leave this empty if you want to use the default path of Assets/DebugAnimators/</param>
		public static void PrintToAnimator<TOwnId, TStateId, TEvent>(this StateMachine<TOwnId, TStateId, TEvent> hfsm,
		string pathToFolderForDebugAnimator = "", string animatorName = "StateMachineDebugger.controller")
		{
			if (hfsm.stateBundlesByName.Count == 0)
			{
				Debug.LogError("Trying to print an empty HFSM. You probably forgot to add the states and transitions before calling this method.");
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

			//remove old transitions from state machine before setting it up freshly
			RemoveTransitionsFromStateMachine(animatorMirror.layers[0].stateMachine);

			SetupAnimatorStateMachine(animatorMirror.layers[0].stateMachine, hfsm, new(), new());
		}

		private static void SetupAnimatorStateMachine<TOwnId, TStateId, TEvent>(AnimatorStateMachine animatorStateMachine, StateMachine<TOwnId, TStateId, TEvent> hfsm,
		Dictionary<TStateId, AnimatorState> animatorStateDict, Dictionary<TStateId, AnimatorStateMachine> animatorStateMachineDict)
		{
			//Add Animator states mirroring HFSM states
			foreach (StateBase<TStateId> state in hfsm.stateBundlesByName.Values?.Select(bundle => bundle.state))
			{
				if (state is StateMachine<TOwnId, TStateId, TEvent> subFsm)
					AddStateMachineToAnimator(subFsm, state.name, animatorStateMachine, animatorStateMachineDict, animatorStateDict);
				else
					AddStateToAnimator(state, hfsm, animatorStateMachine, animatorStateDict);
			}

			//Add transitions to Animator which mirror transitions in the HFSM.
			//This cannot be in the same loop as above because the state which is receiving a transition might not have been created yet
			foreach (StateMachine<TOwnId, TStateId, TEvent>.StateBundle stateBundle in hfsm.stateBundlesByName.Values)
			{
				if (stateBundle.state is StateMachine<TOwnId, TStateId, TEvent> subFsm)
					AddStateMachineTransitionsToAnimator(stateBundle, subFsm, animatorStateMachineDict, animatorStateDict);
				else
					AddStateTransitionsToAnimator(stateBundle, animatorStateDict, animatorStateMachineDict);
			}

			foreach (var transition in hfsm.transitionsFromAny)
			{
				animatorStateMachine.AddAnyStateTransition(animatorStateDict[transition.to]);
			}

			foreach (var transitionList in hfsm.triggerTransitionsFromAny.Values)
			{
				foreach (var transition in transitionList)
				{
					animatorStateMachine.AddAnyStateTransition(animatorStateDict[transition.to]);
				}
			}
		}

		private static void AddStateTransitionsToAnimator<TOwnId, TStateId, TEvent>(StateMachine<TOwnId, TStateId, TEvent>.StateBundle stateBundle, 
		Dictionary<TStateId, AnimatorState> animatorStateDict, Dictionary<TStateId, AnimatorStateMachine> animatorStateMachineDict)
		{
			var fromState = animatorStateDict[stateBundle.state.name];

			//remove all existing transitions so that they can be replaced
			AnimatorStateTransition[] transitionsCopy = new AnimatorStateTransition[fromState.transitions.Length];
			Array.Copy(fromState.transitions, transitionsCopy, fromState.transitions.Length);
			foreach (var animatorTransition in transitionsCopy)
				fromState.RemoveTransition(animatorTransition);

			if (stateBundle.transitions != null)
			{
				foreach (var transition in stateBundle.transitions)
				{
					if (animatorStateDict.ContainsKey(transition.to))
						fromState.AddTransition(animatorStateDict[transition.to]);
					else  //if the destination is not a state, then it must be a state machine
					{
						AnimatorStateMachine destinationStateMachine = animatorStateMachineDict[transition.to];
						fromState.AddTransition(destinationStateMachine);
					}
				}
			}

			if (stateBundle.triggerToTransitions == null)
				return;

			foreach (var transitionList in stateBundle.triggerToTransitions.Values)
			{
				foreach (TransitionBase<TStateId> transition in transitionList)
				{
					AnimatorState destinationState = animatorStateDict[transition.to];
					fromState.AddTransition(destinationState);
				}
			}
		}

		private static void RemoveTransitionsFromStateMachine(AnimatorStateMachine animatorStateMachine)
		{
			//remove all entry transitions
			AnimatorTransition[] entryTransitionsCopy = new AnimatorTransition[animatorStateMachine.entryTransitions.Length];
			Array.Copy(animatorStateMachine.entryTransitions, entryTransitionsCopy, animatorStateMachine.entryTransitions.Length);
			foreach (AnimatorTransition animatorTransition in entryTransitionsCopy)
				animatorStateMachine.RemoveEntryTransition(animatorTransition);

			//remove any-state transitions
			AnimatorStateTransition[] anyTransitionsCopy = new AnimatorStateTransition[animatorStateMachine.anyStateTransitions.Length];
			Array.Copy(animatorStateMachine.anyStateTransitions, anyTransitionsCopy, animatorStateMachine.anyStateTransitions.Length);
			foreach (AnimatorStateTransition animatorTransition in anyTransitionsCopy)
				animatorStateMachine.RemoveAnyStateTransition(animatorTransition);
		}

		private static void AddStateMachineTransitionsToAnimator<TOwnId, TStateId, TEvent>(StateMachine<TOwnId, TStateId, TEvent>.StateBundle stateBundle, StateMachine<TOwnId, TStateId, TEvent> subFsm,
		Dictionary<TStateId, AnimatorStateMachine> animatorStatemachineDict, Dictionary<TStateId, AnimatorState> animatorStateDict)
		{
			AnimatorStateMachine animatorStateMachine = animatorStatemachineDict[stateBundle.state.name];

			//Remove all transitoins so that they can be re-placed
			RemoveTransitionsFromStateMachine(animatorStateMachine);

			//Add Any state transitions
			foreach (var transition in subFsm.transitionsFromAny)
			{
				animatorStateMachine.AddAnyStateTransition(animatorStateDict[transition.to]);
			}

			foreach (var transitionList in subFsm.triggerTransitionsFromAny.Values)
			{
				foreach (var transition in transitionList)
				{
					animatorStateMachine.AddAnyStateTransition(animatorStateDict[transition.to]);
				}
			}

			//trigger transitions are treated exactly the same as normal transitions, so concatenate them into one IEnumerable
			IEnumerable<TransitionBase<TStateId>> transitions = stateBundle.transitions;
			if (stateBundle.triggerToTransitions != null)
			{
				foreach (var transitionList in stateBundle.triggerToTransitions.Values)
					transitions.Concat(transitionList);
			}

			foreach (var transition in transitions)
			{
				//AnimatorStatemachine is not interchangable with AnimatorState, so we must check each dictionary separately
				if (animatorStatemachineDict.ContainsKey(transition.to))
				{
					AnimatorStateMachine destinationState = animatorStatemachineDict[transition.to];
					animatorStateMachine.AddStateMachineTransition(destinationState);
				}
				else //if the destination is not a state machine, then it must be a state
				{
					AnimatorState destinationState = animatorStateDict[transition.to];
					animatorStateMachine.AddStateMachineTransition(animatorStateMachine, destinationState);
				}
			}
		}

		private static void AddStateToAnimator<TOwnId, TStateId, TEvent>(StateBase<TStateId> stateToAdd, StateMachine<TOwnId, TStateId, TEvent> parentFSM,
		AnimatorStateMachine animatorStateMachine, Dictionary<TStateId, AnimatorState> animatorStateDict)
		{
			//search to see if the state machine contains a state with the same name as the stateToAdd
			var (foundStateWithSameName, foundChildState) = animatorStateMachine.states.FirstOrFalse(state => state.state.name == stateToAdd.name.ToString());

			if (!foundStateWithSameName)
				foundChildState.state = animatorStateMachine.AddState(stateToAdd.name.ToString());

			//if the parent fsm doesn't have a start state or we are the start state, then make this state the default in the animator
			if (parentFSM.startState.hasState == false || parentFSM.startState.state.ToString() == stateToAdd.name.ToString())
				animatorStateMachine.defaultState = foundChildState.state;

			animatorStateDict.Add(stateToAdd.name, foundChildState.state);
		}

		private static void AddStateMachineToAnimator<TOwnId, TStateId, TEvent>(StateMachine<TOwnId, TStateId, TEvent> subFsm, TStateId stateMachineName, AnimatorStateMachine parentAnimatorStateMachine,
		Dictionary<TStateId, AnimatorStateMachine> stateMachineDictionary, Dictionary<TStateId, AnimatorState> animatorStateDict)
		{
			//search to see if the state machine contains a child state machine with the same name as stateToAdd
			var (didFindStateMachine, childStateMachine) = parentAnimatorStateMachine.stateMachines.FirstOrFalse(childStateMachine => childStateMachine.stateMachine.name == subFsm.name.ToString());

			if (!didFindStateMachine)
				childStateMachine.stateMachine = parentAnimatorStateMachine.AddStateMachine(subFsm.name.ToString());

			stateMachineDictionary.Add(stateMachineName, childStateMachine.stateMachine);

			SetupAnimatorStateMachine(childStateMachine.stateMachine, subFsm, animatorStateDict, stateMachineDictionary);
		}

		public static (bool didFind, T element) FirstOrFalse<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
		{
			foreach (T element in collection)
			{
				if (predicate(element))
					return (true, element);
			}

			return (false, default(T));
		}
	}
}
#endif
