using System;
using System.IO;

/// <summary>Serializer is injected so persistence can be tested without the Unity runtime.</summary>
public sealed class ProgressStore
{
    private readonly string path;
    private readonly GameDatabase database;
    private readonly Func<PlayerProgress, string> serialize;
    private readonly Func<string, PlayerProgress> deserialize;
    public ProgressStore(string path, GameDatabase database, Func<PlayerProgress, string> serialize,
        Func<string, PlayerProgress> deserialize)
    { this.path = path; this.database = database; this.serialize = serialize; this.deserialize = deserialize; }

    public bool TryLoad(out PlayerProgress progress, out string error)
    {
        progress = null;
        error = null;
        if (!File.Exists(path)) return false;
        try
        {
            if (new FileInfo(path).Length > 1024 * 1024) throw new InvalidDataException("Save is too large.");
            var loaded = deserialize(File.ReadAllText(path));
            if (loaded == null || !loaded.IsValid(database)) throw new InvalidDataException("Invalid save version, stats or card IDs.");
            progress = loaded;
            return true;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException ||
            exception is ArgumentException || exception is FormatException)
        { error = exception.Message; return false; }
    }

    public bool TrySave(PlayerProgress progress, out string error)
    {
        error = null;
        if (progress == null || !progress.IsValid(database)) { error = "Invalid progress."; return false; }
        try
        {
            string directory = Path.GetDirectoryName(Path.GetFullPath(path));
            Directory.CreateDirectory(directory);
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, serialize(progress));
            // On supported local desktop filesystems this is an atomic replacement.
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
            else File.Move(temporary, path);
            return true;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException ||
            exception is ArgumentException || exception is NotSupportedException)
        { error = exception.Message; return false; }
    }
}
