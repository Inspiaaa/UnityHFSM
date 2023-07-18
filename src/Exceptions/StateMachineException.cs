using System;

namespace FSM.Exceptions
{
	[Serializable]
	public class StateMachineException : Exception
	{
        public StateMachineException(string message) : base(message) { }
    }
}