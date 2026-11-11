param(
    [string]$FtpHost = "208.91.198.227",
    [string]$Username = "patnag5f",
    [string]$Password = "CYHBrand@PassAK",
    [string]$Target = "Admin" # "Admin" or "Api" or "Both"
)

$ErrorActionPreference = "Continue"

function Make-FtpDir {
    param([string]$Url, [System.Net.NetworkCredential]$Cred)
    try {
        $req = [System.Net.FtpWebRequest]::Create($Url)
        $req.Credentials = $Cred
        $req.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
        $req.UsePassive = $true
        $res = $req.GetResponse()
        $res.Close()
    } catch {
        # Directory already exists or cannot be created, ignore
    }
}

function Upload-FtpFile {
    param(
        [string]$LocalFile,
        [string]$RemoteUrl,
        [System.Net.NetworkCredential]$Cred
    )
    $retries = 3
    while ($retries -gt 0) {
        try {
            $req = [System.Net.FtpWebRequest]::Create($RemoteUrl)
            $req.Credentials = $Cred
            $req.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
            $req.UsePassive = $true
            $req.UseBinary = $true
            $req.Timeout = 30000

            $fileBytes = [System.IO.File]::ReadAllBytes($LocalFile)
            $req.ContentLength = $fileBytes.Length
            $reqStream = $req.GetRequestStream()
            $reqStream.Write($fileBytes, 0, $fileBytes.Length)
            $reqStream.Close()

            $res = $req.GetResponse()
            $res.Close()
            return $true
        } catch {
            $retries--
            if ($retries -eq 0) {
                Write-Host "    [FAILED] $LocalFile -> $($_.Exception.Message)" -ForegroundColor Red
                return $false
            }
            Start-Sleep -Milliseconds 500
        }
    }
    return $false
}

function Deploy-Directory {
    param(
        [string]$LocalRoot,
        [string]$RemoteBaseUrl,
        [System.Net.NetworkCredential]$Cred
    )

    $allFiles = Get-ChildItem -Path $LocalRoot -Recurse -File
    $total = $allFiles.Count
    $idx = 0

    Write-Host "Deploying $total files to $RemoteBaseUrl..." -ForegroundColor Cyan

    # First collect and create all directories
    $allDirs = Get-ChildItem -Path $LocalRoot -Recurse -Directory
    foreach ($d in $allDirs) {
        $relPath = $d.FullName.Substring($LocalRoot.Length).TrimStart('\', '/') -replace '\\', '/'
        $dirUrl = "$RemoteBaseUrl/$relPath"
        Make-FtpDir -Url $dirUrl -Cred $Cred
    }

    # Put app_offline.htm if updating
    $offlineBytes = [System.Text.Encoding]::UTF8.GetBytes("<h2>CyhAstro Deploying... Please wait 10 seconds.</h2>")
    try {
        $req = [System.Net.FtpWebRequest]::Create("$RemoteBaseUrl/app_offline.htm")
        $req.Credentials = $Cred
        $req.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
        $req.UsePassive = $true
        $s = $req.GetRequestStream()
        $s.Write($offlineBytes, 0, $offlineBytes.Length)
        $s.Close()
        $res = $req.GetResponse()
        $res.Close()
    } catch {}

    # Now upload all files
    $success = 0
    $failed = 0
    foreach ($f in $allFiles) {
        $idx++
        $relPath = $f.FullName.Substring($LocalRoot.Length).TrimStart('\', '/') -replace '\\', '/'
        $remoteFileUrl = "$RemoteBaseUrl/$relPath"

        $pct = [math]::Round(($idx / $total) * 100)
        Write-Host "[$idx/$total - $pct%] $relPath"

        $ok = Upload-FtpFile -LocalFile $f.FullName -RemoteUrl $remoteFileUrl -Cred $Cred
        if ($ok) { $success++ } else { $failed++ }
    }

    # Delete index.html if present (to avoid Plesk placeholder overriding ASP.NET Core)
    try {
        $req = [System.Net.FtpWebRequest]::Create("$RemoteBaseUrl/index.html")
        $req.Credentials = $Cred
        $req.Method = [System.Net.WebRequestMethods+Ftp]::DeleteFile
        $req.UsePassive = $true
        $res = $req.GetResponse()
        $res.Close()
        Write-Host "Deleted default Plesk index.html" -ForegroundColor Yellow
    } catch {}

    # Remove app_offline.htm to bring site back online
    try {
        $req = [System.Net.FtpWebRequest]::Create("$RemoteBaseUrl/app_offline.htm")
        $req.Credentials = $Cred
        $req.Method = [System.Net.WebRequestMethods+Ftp]::DeleteFile
        $req.UsePassive = $true
        $res = $req.GetResponse()
        $res.Close()
    } catch {}

    Write-Host "Done! Success: $success, Failed: $failed" -ForegroundColor Green
}

$cred = New-Object System.Net.NetworkCredential($Username, $Password)

if ($Target -eq "Admin" -or $Target -eq "Both") {
    Write-Host "==========================================" -ForegroundColor Magenta
    Write-Host "Deploying CyhAstro.Admin -> cyhastro.patnaorthoclinic.com" -ForegroundColor Magenta
    Write-Host "==========================================" -ForegroundColor Magenta
    $localAdmin = (Resolve-Path ".\publish\Admin").Path
    $remoteAdmin = "ftp://$FtpHost/cyhastro.patnaorthoclinic.com"
    Deploy-Directory -LocalRoot $localAdmin -RemoteBaseUrl $remoteAdmin -Cred $cred
}

if ($Target -eq "Api" -or $Target -eq "Both") {
    Write-Host "==========================================" -ForegroundColor Magenta
    Write-Host "Deploying CyhAstro.Api -> cyhastroapi.patnaorthoclinic.com" -ForegroundColor Magenta
    Write-Host "==========================================" -ForegroundColor Magenta
    $localApi = (Resolve-Path ".\publish\Api").Path
    $remoteApi = "ftp://$FtpHost/cyhastroapi.patnaorthoclinic.com"
    Deploy-Directory -LocalRoot $localApi -RemoteBaseUrl $remoteApi -Cred $cred
}
