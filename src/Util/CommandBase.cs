using System;

namespace FSM
{
    public abstract class CommandBase<TStateId>
    {
        internal abstract void Register(State<TStateId> state);
    }
		
    public class Command<TStateId, TCommand>: CommandBase<TStateId>
    {
        private readonly Action<State<TStateId>, TCommand> handler;

        public Command(Action<State<TStateId>, TCommand> handler)
        {
            this.handler = handler;
        }

        internal override void Register(State<TStateId> state)
        {
            if (handler == null)
            {
                return;
            }
            state.SetCommandHandlerInternal<TCommand>(command =>
            {
                handler?.Invoke(state, command);
            });
        }
    }

    public class Command<TCommand> : Command<string, TCommand>
    {
        public Command(Action<State<string>, TCommand> handler) : base(handler)
        {
        }
    }
}