
namespace FSM
{
	public interface ITransitionListener
	{
        void BeforeTransition();
        void AfterTransition();
    }
}