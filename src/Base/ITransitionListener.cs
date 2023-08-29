
namespace UnityHFSM
{
	public interface ITransitionListener
	{
		void BeforeTransition();
		void AfterTransition();
	}
}
