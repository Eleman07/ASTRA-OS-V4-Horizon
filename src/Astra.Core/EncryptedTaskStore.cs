using System.Security.Cryptography;
using System.Text.Json;

namespace AstraOS.Core;

/// <summary>Local task list encrypted using Windows DPAPI, scoped to the current Windows account.</summary>
public sealed class EncryptedTaskStore
{
    private readonly string _path;
    public EncryptedTaskStore(string? directory = null)
    {
        var root = directory ?? Path.Combine(AstraPaths.UserDataRoot, "tasks");
        _path = Path.Combine(root, "tasks.dpapi");
    }

    public List<AstraTaskItem> Load()
    {
        if (!File.Exists(_path)) return new();
        if (new FileInfo(_path).Length > 128_000) throw new InvalidDataException("Archivio attività troppo grande.");
        byte[] plain = ProtectedData.Unprotect(File.ReadAllBytes(_path), null, DataProtectionScope.CurrentUser);
        try
        {
            var tasks = JsonSerializer.Deserialize<List<AstraTaskItem>>(plain);
            return PreferenceStore.SanitizeTasks(tasks);
        }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }

    public void Save(List<AstraTaskItem> tasks)
    {
        tasks = PreferenceStore.SanitizeTasks(tasks);
        var json = JsonSerializer.SerializeToUtf8Bytes(tasks);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var ciphertext = ProtectedData.Protect(json, null, DataProtectionScope.CurrentUser);
            var tmp = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(tmp, ciphertext);
                File.Move(tmp, _path, overwrite: true);
            }
            finally { if (File.Exists(tmp)) File.Delete(tmp); }
        }
        finally { CryptographicOperations.ZeroMemory(json); }
    }
}
