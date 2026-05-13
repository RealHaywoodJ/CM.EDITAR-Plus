namespace CM.EDITAR.Core;

/// <summary>Centralized resolution of every on-disk path the app uses. Honors per-user storage layout.</summary>
public static class AppPaths
{
    public const string AppFolderName = "CM.EDITAR+";

    public static string Roaming => Combine(Environment.SpecialFolder.ApplicationData);
    public static string Local => Combine(Environment.SpecialFolder.LocalApplicationData);

    public static string TemplatesDir => Path.Combine(Roaming, "Templates");
    public static string BackupsDir => Path.Combine(Local, "Backups");
    public static string AuditDir => Path.Combine(Local, "Audit");
    public static string LogsDir => Path.Combine(Local, "Logs");
    public static string SecretsDir => Path.Combine(Local, "Secrets");

    public static string AuditLogFile => Path.Combine(AuditDir, "changes.log");

    /// <summary>The named-pipe identifier used by FileCreator's RPC server. User-scoped.</summary>
    public static string FileCreatorPipeName => $"CM.EDITAR.FileCreator.{Environment.UserName}";

    public static void EnsureAll()
    {
        Directory.CreateDirectory(TemplatesDir);
        Directory.CreateDirectory(BackupsDir);
        Directory.CreateDirectory(AuditDir);
        Directory.CreateDirectory(LogsDir);
        Directory.CreateDirectory(SecretsDir);
    }

    private static string Combine(Environment.SpecialFolder folder) =>
        Path.Combine(Environment.GetFolderPath(folder, Environment.SpecialFolderOption.Create), AppFolderName);
}
