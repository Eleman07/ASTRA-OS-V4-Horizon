using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;

namespace AstraOS.Core;

/// <summary>Stores an OpenRouter API key encrypted for the current Windows account.</summary>
public sealed class OpenRouterCredentialStore
{
    private readonly string _path;
    public OpenRouterCredentialStore(string? directory = null)
    {
        string root = directory ?? Path.Combine(AstraPaths.UserDataRoot, "ai");
        _path = Path.Combine(root, "openrouter-key.dpapi");
    }

    public bool HasCredential => File.Exists(_path) || WindowsCredential() is not null;

    public void Save(string apiKey)
    {
        Validate(apiKey);
        byte[] clear = Encoding.UTF8.GetBytes(apiKey);
        string? temp = null;
        try
        {
            string? folder = Path.GetDirectoryName(_path);
            if (folder is null) throw new InvalidOperationException(UiText.T("Percorso della credenziale non valido."));
            Directory.CreateDirectory(folder);
            byte[] encrypted = ProtectedData.Protect(clear, null, DataProtectionScope.CurrentUser);
            temp = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try { File.WriteAllBytes(temp, encrypted); File.Move(temp, _path, true); }
            finally { CryptographicOperations.ZeroMemory(encrypted); }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(clear);
            if (temp is not null && File.Exists(temp)) File.Delete(temp);
        }
    }

    public string? Load()
    {
        if (!File.Exists(_path)) return WindowsCredential();
        var info = new FileInfo(_path);
        if (info.Length is < 32 or > 16_384) throw new InvalidDataException(UiText.T("Credenziale OpenRouter non valida."));
        byte[] clear = ProtectedData.Unprotect(File.ReadAllBytes(_path), null, DataProtectionScope.CurrentUser);
        try
        {
            string key = Encoding.UTF8.GetString(clear);
            Validate(key);
            return key;
        }
        finally { CryptographicOperations.ZeroMemory(clear); }
    }

    public void Delete()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }

    // Import only ASTRA's two historical targets. Never enumerate or disclose unrelated credentials.
    private static string? WindowsCredential()
    {
        if(!OperatingSystem.IsWindows())return null;
        foreach(string target in new[]{"openrouter@ASTRA Learning Suite","ASTRA Learning Suite"})
        {
            if(!CredRead(target,1,0,out var pointer))continue;
            try
            {
                var credential=Marshal.PtrToStructure<NativeCredential>(pointer);
                if(credential.CredentialBlobSize is 0 or >2048)continue;
                var bytes=new byte[credential.CredentialBlobSize];Marshal.Copy(credential.CredentialBlob,bytes,0,bytes.Length);
                try
                {
                    foreach(var encoding in new[]{Encoding.Unicode,Encoding.UTF8})
                    {
                        string candidate=encoding.GetString(bytes).TrimEnd('\0');
                        try{Validate(candidate);return candidate;}catch(ArgumentException){}
                    }
                }
                finally{CryptographicOperations.ZeroMemory(bytes);}
            }
            finally{CredFree(pointer);}
        }
        return null;
    }
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
    private struct NativeCredential
    {
        public uint Flags,Type;public string TargetName,Comment;public long LastWritten;
        public uint CredentialBlobSize;public IntPtr CredentialBlob;public uint Persist,AttributeCount;
        public IntPtr Attributes;public string TargetAlias,UserName;
    }
    [DllImport("advapi32.dll",EntryPoint="CredReadW",CharSet=CharSet.Unicode,SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]private static extern bool CredRead(string target,uint type,uint flags,out IntPtr credential);
    [DllImport("advapi32.dll")]private static extern void CredFree(IntPtr pointer);

    private static void Validate(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length is < 20 or > 512 ||
            !apiKey.StartsWith("sk-or-v1-", StringComparison.Ordinal) || apiKey.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_'))
            throw new ArgumentException(UiText.T("Inserisci una chiave API OpenRouter valida (inizia con sk-or-v1-)."), nameof(apiKey));
    }
}
