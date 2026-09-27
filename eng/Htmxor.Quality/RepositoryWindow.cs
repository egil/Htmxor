namespace Htmxor.Quality;

internal enum WorktreeState
{
	CleanThroughout,
	DirtyBeforeRun,
	ChangedDuringRun,
}

/// <summary>
/// The repository as sampled before and after the commands a receipt describes. The state compares
/// HEAD and the full porcelain text, not the dirty flags, so a tree that was already dirty and
/// changed further is still reported as changed
/// (https://github.com/egil/Htmxor/issues/234#issuecomment-5854112160). Nothing between the two
/// samples is observed, so a change made and reverted inside the window stays invisible.
/// </summary>
internal sealed record RepositoryWindow(RepositoryEvidence Opening, RepositoryEvidence Closing)
{
	public WorktreeState State =>
		Opening.Head != Closing.Head || Opening.Status != Closing.Status
			? WorktreeState.ChangedDuringRun
			: Opening.Dirty ? WorktreeState.DirtyBeforeRun : WorktreeState.CleanThroughout;

	public string StateName => State switch
	{
		WorktreeState.CleanThroughout => "cleanThroughout",
		WorktreeState.DirtyBeforeRun => "dirtyBeforeRun",
		_ => "changedDuringRun",
	};

	public string StateWording => State switch
	{
		WorktreeState.CleanThroughout => "clean throughout",
		WorktreeState.DirtyBeforeRun => "dirty before the run began",
		_ => "changed during the run",
	};
}
