$ErrorActionPreference = 'Stop'
$webRoot = $PSScriptRoot
$previewUrl = 'http://127.0.0.1:8765'
$pythonCommand = Get-Command python -ErrorAction SilentlyContinue
if (-not $pythonCommand) { throw 'Python 3 is needed for this local preview launcher. You can also upload this folder to any static web host.' }
try { $response = Invoke-WebRequest -Uri $previewUrl -TimeoutSec 2 -UseBasicParsing; $alreadyRunning = $response.Content -match 'Sun &amp; Moon' } catch { $alreadyRunning = $false }
if (-not $alreadyRunning) {
  Start-Process -FilePath $pythonCommand.Source -ArgumentList @('-m','http.server','8765','--bind','127.0.0.1') -WorkingDirectory $webRoot -WindowStyle Hidden -RedirectStandardOutput (Join-Path $webRoot 'preview-server.log') -RedirectStandardError (Join-Path $webRoot 'preview-errors.log')
  Start-Sleep -Seconds 1
}
Start-Process $previewUrl
