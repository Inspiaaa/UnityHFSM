namespace UnityHFSM.Visualization
{
	// Weirdly, when Unity builds a project and a namespace contains no classes (e.g. because of preprocessor
	// directives that remove code from the build), the namespace no longer exists. This can cause build problems
	// if it is referenced in a "using" directive, even when no editor-only class is used in the code.
	// The following class keeps this namespace alive, so that developers do not have to add a preprocessor
	// guard to their code when they reference this namespace.

	/// <summary>
	/// Helper class to keep the <see cref="UnityHFSM.Visualization"/> namespace available when building the project.
	/// </summary>
	internal class _VisualizationNamespacePlaceholder
	{

	}
}
