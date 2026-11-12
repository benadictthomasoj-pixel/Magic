<#
.SYNOPSIS
    Reference PowerShell setup template for installing MAGIC Backend as a Windows Service and configuring Windows Firewall.
    PENDING WINDOWS VERIFICATION — authored during Phase 1 on macOS ARM64; test on Windows 10/11 host before production use.
#>

param(
    [string]$ServiceName = "MagicBackend",
    [string]$BinaryPath = "C:\Program Files\MAGIC\Backend\Magic.Api.exe --contentRoot ""C:\Program Files\MAGIC\Backend""",
    [string]$LocalSubnet = "192.168.1.0/24",
    [int]$ApiPort = 5001
)

Write-Host "=== MAGIC Windows Service Registration Template ==="
Write-Host "1. Register Windows Service (run as Administrator on Windows 10/11):"
Write-Host "   New-Service -Name '$ServiceName' -BinaryPathName '$BinaryPath' -DisplayName 'MAGIC ERP Backend Host' -StartupType Automatic"
Write-Host "   sc.exe failure '$ServiceName' reset= 86400 actions= restart/60000/restart/60000/none/0"
Write-Host ""
Write-Host "2. Restrict Windows Firewall to LAN Private Profile Port ${ApiPort}:"
Write-Host "   New-NetFirewallRule -DisplayName 'MAGIC ERP HTTPS API' -Direction Inbound -Protocol TCP -LocalPort $ApiPort -RemoteAddress '$LocalSubnet' -Profile Private -Action Allow"
Write-Host ""
Write-Host "3. Ensure PostgreSQL TCP 5432 remains loopback-only in postgresql.conf (listen_addresses = 'localhost')."

