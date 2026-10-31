#requires -Version 5.1
# Downloads Microsoft installers without running them or changing Windows.
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$destination = Join-Path $root 'dist\release-prerequisites'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
Add-Type -AssemblyName System.Net.Http
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$client = New-Object Net.Http.HttpClient
$client.Timeout = [TimeSpan]::FromMinutes(10)
$sources = @(
    @{ File = 'MicrosoftEdgeWebView2RuntimeInstallerX64.exe'; Url = 'https://go.microsoft.com/fwlink/?linkid=2124701'; Documentation = 'https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution' },
    @{ File = 'vc_redist.x64.exe'; Url = 'https://aka.ms/vc14/vc_redist.x64.exe'; Documentation = 'https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist' }
)
$records = @()
try {
    foreach ($source in $sources) {
        $file = Join-Path $destination $source.File
        $resolvedUrl = $source.Url
        if (-not (Test-Path -LiteralPath $file)) {
            Write-Output "Downloading official Microsoft installer: $($source.File)"
            $response = $client.GetAsync($source.Url, [Net.Http.HttpCompletionOption]::ResponseHeadersRead).GetAwaiter().GetResult()
            try {
                $response.EnsureSuccessStatusCode() | Out-Null
                $resolvedUrl = $response.RequestMessage.RequestUri.AbsoluteUri
                $hostName = $response.RequestMessage.RequestUri.Host
                if ($response.RequestMessage.RequestUri.Scheme -ne 'https' -or
                    $hostName -notmatch '(^|\.)(microsoft\.com|windows\.net|visualstudio\.microsoft\.com)$') {
                    throw "Unexpected Microsoft download redirect: $resolvedUrl"
                }
                $stream = [IO.File]::Open($file, [IO.FileMode]::CreateNew)
                try { $response.Content.CopyToAsync($stream).GetAwaiter().GetResult() }
                finally { $stream.Dispose() }
            } finally { $response.Dispose() }
        }
        $signature = Get-AuthenticodeSignature -LiteralPath $file
        if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch '(^|, )O=Microsoft Corporation(,|$)') {
            throw "Microsoft signature verification failed for $file : $($signature.Status)"
        }
        $item = Get-Item -LiteralPath $file
        if ($item.Length -lt 10000000) { throw "Expected full offline installer, not a bootstrapper: $file" }
        $records += [ordered]@{
            file = $source.File
            source = $source.Url
            resolvedSource = $resolvedUrl
            documentation = $source.Documentation
            bytes = $item.Length
            sha256 = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash
            fileVersion = $item.VersionInfo.FileVersion
            productName = $item.VersionInfo.ProductName
            signatureStatus = [string]$signature.Status
            publisher = $signature.SignerCertificate.Subject
            certificateThumbprint = $signature.SignerCertificate.Thumbprint
        }
        Write-Output "Verified: $($source.File) ($($item.Length) bytes, Microsoft signature valid)."
    }
    [ordered]@{ verifiedUtc = [DateTime]::UtcNow.ToString('o'); installers = $records } |
        ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $destination 'sources.json') -Encoding UTF8
} finally { $client.Dispose() }
