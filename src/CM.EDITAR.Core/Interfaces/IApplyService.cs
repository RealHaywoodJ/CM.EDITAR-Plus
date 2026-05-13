namespace CM.EDITAR.Core;

/// <summary>Orchestrates the snapshot → apply → SHChangeNotify → verify → rollback pipeline.</summary>
public interface IApplyService
{
    Task<ApplyResult> ApplyAsync(ApplyManifest manifest, CancellationToken ct = default);

    /// <summary>Undo the most recent successful apply by restoring its snapshot.</summary>
    Task<OperationResult> UndoLastAsync(CancellationToken ct = default);

    /// <summary>Restore every snapshot in reverse chronological order. Stops on first failure.</summary>
    Task<OperationResult> UndoAllAsync(CancellationToken ct = default);
}
