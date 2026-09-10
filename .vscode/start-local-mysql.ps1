$ErrorActionPreference = 'Stop'

$mysqlExecutable = 'C:\Program Files\MySQL\MySQL Server 26.7\bin\mysqld.exe'
$mysqlConfig = 'C:\Users\BGQ\Documents\Codex\2026-09-08\ok\mysql-local\my.ini'

if (-not (Test-Path -LiteralPath $mysqlExecutable)) {
    throw "MySQL Server was not found at $mysqlExecutable"
}
if (-not (Test-Path -LiteralPath $mysqlConfig)) {
    throw "The local MySQL configuration was not found at $mysqlConfig"
}

$listening = Test-NetConnection -ComputerName '127.0.0.1' -Port 3306 -InformationLevel Quiet -WarningAction SilentlyContinue
if ($listening) {
    Write-Output 'Local MySQL is already listening on 127.0.0.1:3306.'
    exit 0
}

$arguments = "--defaults-file=$mysqlConfig"
$process = Start-Process -FilePath $mysqlExecutable -ArgumentList $arguments -WorkingDirectory (Split-Path -Parent $mysqlConfig) -WindowStyle Hidden -PassThru

for ($attempt = 0; $attempt -lt 20; $attempt++) {
    Start-Sleep -Milliseconds 500
    if ($process.HasExited) {
        throw "Local MySQL exited with code $($process.ExitCode)."
    }
    if (Test-NetConnection -ComputerName '127.0.0.1' -Port 3306 -InformationLevel Quiet -WarningAction SilentlyContinue) {
        Write-Output 'Local MySQL started on 127.0.0.1:3306.'
        exit 0
    }
}

throw 'Local MySQL did not open port 3306 within 10 seconds.'
