$ErrorActionPreference = 'Stop'

$mysqlAdmin = 'C:\Program Files\MySQL\MySQL Server 26.7\bin\mysqladmin.exe'
if (-not (Test-Path -LiteralPath $mysqlAdmin)) {
    throw "mysqladmin was not found at $mysqlAdmin"
}

if (-not (Test-NetConnection -ComputerName '127.0.0.1' -Port 3306 -InformationLevel Quiet -WarningAction SilentlyContinue)) {
    Write-Output 'Local MySQL is not running.'
    exit 0
}

Write-Output 'Enter the MySQL password when prompted to stop the server safely.'
& $mysqlAdmin --protocol=TCP --host=127.0.0.1 --port=3306 --user=root --password shutdown
if ($LASTEXITCODE -ne 0) {
    throw "mysqladmin returned exit code $LASTEXITCODE."
}
Write-Output 'Local MySQL stopped.'
