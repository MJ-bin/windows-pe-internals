<#
.SYNOPSIS
    One wide VM collection for PE_emulator (380, D182, D184 ~ D190). Five modes, run in this order:
      Check   (any PowerShell)        build 26100.9457 check, C# compile, free space, tools present
      User    (NOT elevated)          unelevated token / environment / process info / session values / P6
      Net     (any, twice)            network tables: -NetTag unplugged, then -NetTag plugged
      DryRun  (elevated)              disk walk only: files and bytes per rule and per folder -> show to the analyst, then stop
      Full    (elevated)              registry, disk tree + meta + content, SD table, sysinfo, objects, probes -> tar.gz parts

.DESCRIPTION
    Output: <Dest>\win11-<DisplayVersion>-<Build.UBR>\   (= host ms/bin/win11-24H2-26100.9457/ of windows-pe-internals)
      loose:   info.txt  manifest.csv  errors.log  identity.csv  dryrun.csv  dryrun_top.csv  tar_*.log
               registry.tar.gz  meta.tar.gz  content.tar.gz  (content_nonansi.tar.gz)  + one .sha256 each
      registry.tar.gz : registry/  (old six jsonl unchanged + user_default / user_localservice / user_networkservice
                                    + key_meta.jsonl + roots.jsonl)   sd/sd_table.jsonl
      meta.tar.gz     : disk/ (tree.csv old 6 columns, now with %windir% files, tree_meta.csv, content.csv, content_list.txt)
                        sysinfo/  objects/  probe/
      content.tar.gz  : file bytes, paths relative to C:\ (extract into C/ on the host)
    Old formats are kept (ref_reg_users.py is strict): registry/*.jsonl key lines, excluded.jsonl, disk/tree.csv 6 columns,
    identity.csv first 3 columns, manifest.csv RelPath,Size,SHA256,Rows. New information is in side files only.

    Never collected: HKLM\SAM, SECURITY, BCD00000000 contents (open status + root SD only), hives of other users,
    hive files, DigitalProductId / password / productkey values (V1), usage-history keys (K1, K2), browser caches (D1),
    Desktop, Documents, Downloads contents (D2), crypto / credential folders contents (names only).
    VM-specific values are collected raw and listed in identity.csv (Kind, Value, Source, Where) - nothing is replaced here.

.PARAMETER Mode
    Check | User | Net | DryRun | Full
.PARAMETER Dest
    Output root. Not inside %windir%. Recommended: a disk or share that is NOT the system drive (I4).
.PARAMETER NetTag
    Net mode only: unplugged | plugged
.PARAMETER AllowSystemDrive
    Allow Dest on the system drive (the output folder is then skipped by the disk walk and named in info.txt).
.PARAMETER ProbeExe
    vm_probe.exe (built by sample\build_vm.bat). Default: ..\bin\vm_probe.exe next to this script.
.PARAMETER Force
    Continue past the guards (missing dry run / user part / net parts, low free space).

.EXAMPLE
    Set-ExecutionPolicy -Scope Process Bypass
    .\collect_vm.ps1 -Mode Check  -Dest E:\vmcollect
    .\collect_vm.ps1 -Mode User   -Dest E:\vmcollect            # normal (not "Run as administrator") PowerShell
    .\collect_vm.ps1 -Mode Net    -Dest E:\vmcollect -NetTag unplugged
    .\collect_vm.ps1 -Mode Net    -Dest E:\vmcollect -NetTag plugged
    .\collect_vm.ps1 -Mode DryRun -Dest E:\vmcollect            # elevated
    .\collect_vm.ps1 -Mode Full   -Dest E:\vmcollect            # elevated, after the dry run was checked
#>
param(
    [ValidateSet('Check', 'User', 'Net', 'DryRun', 'Full')]
    [string]$Mode = 'Check',
    [Parameter(Mandatory = $true)]
    [string]$Dest,
    [ValidateSet('', 'unplugged', 'plugged')]
    [string]$NetTag = '',
    [switch]$AllowSystemDrive,
    [string]$ProbeExe = '',
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$sw = [Diagnostics.Stopwatch]::StartNew()
$TargetBuild   = '26100.9457'
$LayoutVersion = 1
$MinFreeFull   = 20GB
$Utf8NoBom     = New-Object Text.UTF8Encoding $false
$ScriptDir     = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $ProbeExe) { $ProbeExe = Join-Path (Split-Path -Parent $ScriptDir) 'bin\vm_probe.exe' }

# ---------- registry roots (<SID> = SID of the user running this script) ----------
# the first five are the old reg-users roots (same files, same format)
$RegRoots = @(
    @{ File = 'machine_software.jsonl';    Hive = 'HKLM'; Sub = 'SOFTWARE';      Nt = '\Registry\Machine\SOFTWARE' },
    @{ File = 'machine_system.jsonl';      Hive = 'HKLM'; Sub = 'SYSTEM';        Nt = '\Registry\Machine\SYSTEM' },
    @{ File = 'machine_hardware.jsonl';    Hive = 'HKLM'; Sub = 'HARDWARE';      Nt = '\Registry\Machine\HARDWARE' },
    @{ File = 'user.jsonl';                Hive = 'HKU';  Sub = '<SID>';         Nt = '\Registry\User\<SID>' },
    @{ File = 'user_classes.jsonl';        Hive = 'HKU';  Sub = '<SID>_Classes'; Nt = '\Registry\User\<SID>_Classes' },
    @{ File = 'user_default.jsonl';        Hive = 'HKU';  Sub = '.DEFAULT';      Nt = '\Registry\User\.DEFAULT' },
    @{ File = 'user_localservice.jsonl';   Hive = 'HKU';  Sub = 'S-1-5-19';      Nt = '\Registry\User\S-1-5-19' },
    @{ File = 'user_networkservice.jsonl'; Hive = 'HKU';  Sub = 'S-1-5-20';      Nt = '\Registry\User\S-1-5-20' }
)

# ---------- exclusion rules (case-insensitive) - K1 K2 V1 D1 are the old ones, D2 is new (C10) ----------
$RuleK1KeySuffix = @(
    'Explorer\RecentDocs', 'Explorer\RunMRU', 'Explorer\TypedPaths', 'Explorer\UserAssist',
    'Explorer\ComDlg32', 'Explorer\WordWheelQuery', 'Explorer\FeatureUsage',
    'Internet Explorer\TypedURLs', 'Internet Explorer\TypedURLsTime',
    'Shell\BagMRU', 'Shell\Bags', 'Shell\MuiCache', 'IdentityCRL', 'Terminal Server Client',
    'AppCompatFlags\Compatibility Assistant', 'Services\bam\State\UserSettings'
)
$RuleK2LeafSuffix = @('MRU')
$RuleV1Contains   = @('password', 'productkey')
$RuleV1Prefix     = @('DigitalProductId')
$RuleV1Equals     = @('AppCompatCache')
$RuleD1ProfileDirs = @(
    'AppData\Local\Microsoft\Windows\INetCache', 'AppData\Local\Microsoft\Windows\INetCookies',
    'AppData\Local\Microsoft\Windows\WebCache', 'AppData\Local\Microsoft\Windows\History',
    'AppData\Roaming\Microsoft\Windows\Recent', 'AppData\Local\Google\Chrome\User Data',
    'AppData\Local\Microsoft\Edge\User Data', 'AppData\Roaming\Mozilla\Firefox\Profiles',
    'AppData\Local\Mozilla\Firefox\Profiles'
)
$RuleD2ProfileDirs = @('Desktop', 'Documents', 'Downloads')

# ---------- content rules: which file BYTES go into content.tar.gz (first match wins, default = name only) ----------
# Pattern: relative to C:\, '*' inside one name, '**' anything, trailing '\**' = that folder and below.
# Kind: file | dir | *.   Mode: content | name.   (380_ms-layout-needs.md 2 section, D185, D190)
$ContentRules = @(
    @('**\*vmware*\**',                                   '*',    'name',    'vmware'),
    @('**\*vmware*',                                      'file', 'name',    'vmware'),
    @('Windows\*.log',                                    'file', 'name',    'win_root_log'),
    @('**\Microsoft\Crypto\**',                           '*',    'name',    'crypto'),
    @('**\Microsoft\Protect\**',                          '*',    'name',    'crypto'),
    @('**\Microsoft\Credentials\**',                      '*',    'name',    'crypto'),
    @('**\Microsoft\Vault\**',                            '*',    'name',    'crypto'),
    @('**\NTUSER.DAT*',                                   'file', 'name',    'hive_file'),
    @('**\UsrClass.dat*',                                 'file', 'name',    'hive_file'),
    @('Windows\System32\config\**',                       '*',    'name',    'sys32_state'),
    @('Windows\System32\DriverStore\**',                  '*',    'name',    'sys32_state'),
    @('Windows\System32\LogFiles\**',                     '*',    'name',    'sys32_state'),
    @('Windows\System32\winevt\**',                       '*',    'name',    'sys32_state'),
    @('Windows\System32\sru\**',                          '*',    'name',    'sys32_state'),
    @('Windows\System32\CatRoot\**',                      '*',    'name',    'sys32_state'),
    @('Windows\System32\catroot2\**',                     '*',    'name',    'sys32_state'),
    @('Windows\System32\spool\**',                        '*',    'name',    'sys32_state'),
    @('Windows\System32\WDI\**',                          '*',    'name',    'sys32_state'),
    @('Windows\System32\SleepStudy\**',                   '*',    'name',    'sys32_state'),
    @('Windows\System32\wbem\Repository\**',              '*',    'name',    'sys32_state'),
    @('Windows\System32\wbem\Logs\**',                    '*',    'name',    'sys32_state'),
    @('Windows\System32\Tasks\**',                        '*',    'name',    'sys32_state'),
    @('Windows\System32\MsDtc\**',                        '*',    'name',    'sys32_state'),
    @('Windows\System32\Recovery\**',                     '*',    'name',    'sys32_state'),
    @('Windows\System32\WebThreatDefSvc\**',              '*',    'name',    'sys32_state'),
    @('Windows\System32\SMI\Store\**',                    '*',    'name',    'sys32_state'),
    @('Windows\System32\Sysprep\Panther\**',              '*',    'name',    'sys32_state'),
    @('Windows\SysWOW64\config\**',                       '*',    'name',    'sys32_state'),
    @('Windows\SysWOW64\Tasks\**',                        '*',    'name',    'sys32_state'),
    @('Windows\Logs\**',                                  '*',    'name',    'win_state'),
    @('Windows\Panther\**',                               '*',    'name',    'win_state'),
    @('Windows\Prefetch\**',                              '*',    'name',    'win_state'),
    @('Windows\Temp\**',                                  '*',    'name',    'win_state'),
    @('Windows\SystemTemp\**',                            '*',    'name',    'win_state'),
    @('Windows\SoftwareDistribution\**',                  '*',    'name',    'win_state'),
    @('Windows\ServiceProfiles\**',                       '*',    'name',    'win_state'),
    @('Windows\ServiceState\**',                          '*',    'name',    'win_state'),
    @('Windows\appcompat\**',                             '*',    'name',    'win_state'),
    @('Windows\Minidump\**',                              '*',    'name',    'win_state'),
    @('Windows\LiveKernelReports\**',                     '*',    'name',    'win_state'),
    @('Windows\Installer\**',                             '*',    'name',    'win_state'),
    @('Windows\servicing\**',                             '*',    'name',    'win_state'),
    @('Windows\WaaS\**',                                  '*',    'name',    'win_state'),
    @('Windows\UUS\**',                                   '*',    'name',    'win_state'),
    @('Windows\CbsTemp\**',                               '*',    'name',    'win_state'),
    @('Windows\Debug\**',                                 '*',    'name',    'win_state'),
    @('Windows\tracing\**',                               '*',    'name',    'win_state'),
    @('Windows\CSC\**',                                   '*',    'name',    'win_state'),
    @('Windows\security\database\**',                     '*',    'name',    'win_state'),
    @('Windows\security\logs\**',                         '*',    'name',    'win_state'),
    @('Windows\Performance\WinSAT\DataStore\**',          '*',    'name',    'win_state'),
    @('Windows\assembly\temp\**',                         '*',    'name',    'win_state'),
    @('Windows\assembly\tmp\**',                          '*',    'name',    'win_state'),
    @('Windows\Microsoft.NET\**\Temporary ASP.NET Files\**', '*', 'name',    'win_state'),
    @('Windows\WinSxS\Manifests\**',                      '*',    'content', 'winsxs_manifests'),
    @('Windows\WinSxS\amd64_microsoft.windows.*\**',      '*',    'content', 'winsxs_win32'),
    @('Windows\WinSxS\x86_microsoft.windows.*\**',        '*',    'content', 'winsxs_win32'),
    @('Windows\WinSxS\amd64_microsoft.vc*\**',            '*',    'content', 'winsxs_win32'),
    @('Windows\WinSxS\x86_microsoft.vc*\**',              '*',    'content', 'winsxs_win32'),
    @('Windows\WinSxS\amd64_policy.*\**',                 '*',    'content', 'winsxs_win32'),
    @('Windows\WinSxS\x86_policy.*\**',                   '*',    'content', 'winsxs_win32'),
    @('Windows\WinSxS\**',                                '*',    'name',    'winsxs_store'),
    @('Windows\**',                                       '*',    'content', 'windows'),
    @('Program Files\WindowsApps\Microsoft.LanguageExperiencePackko-KR_*\**', '*', 'content', 'lxp_ko'),
    @('Program Files\WindowsApps\Microsoft.VCLibs.*\**',          '*', 'content', 'appx_framework'),
    @('Program Files\WindowsApps\Microsoft.UI.Xaml.*\**',         '*', 'content', 'appx_framework'),
    @('Program Files\WindowsApps\Microsoft.WindowsAppRuntime.*\**', '*', 'content', 'appx_framework'),
    @('Program Files\WindowsApps\Microsoft.NET.Native.*\**',      '*', 'content', 'appx_framework'),
    @('Program Files\WindowsApps\**',                     '*',    'name',    'appx_other')
)
foreach ($pf in @('Program Files', 'Program Files (x86)')) {
    foreach ($small in @('Common Files', 'Internet Explorer', 'Windows Defender', 'Windows Defender Advanced Threat Protection',
                         'Windows Mail', 'Windows Media Player', 'Windows NT', 'Windows Photo Viewer', 'Windows Portable Devices',
                         'Windows Security', 'Windows Sidebar', 'WindowsPowerShell', 'Microsoft.NET', 'MSBuild', 'Reference Assemblies',
                         'ModifiableWindowsApps')) {
        $ContentRules += , @(("{0}\{1}\**" -f $pf, $small), '*', 'content', 'ms_small')
    }
}
$ContentRules += , @('ProgramData\Microsoft\Windows Defender\Platform\**', '*', 'content', 'defender_platform')
$ContentRules += , @('ProgramData\Microsoft\Windows\Caches\**', '*', 'content', 'shell_caches')
$ContentRules += , @('Users\*\AppData\Local\Microsoft\Windows\Caches\**', '*', 'content', 'shell_caches')
$ContentRules += , @('**\desktop.ini', 'file', 'content', 'desktop_ini')

$ApiSets = @('ext-ms-win-gdi-edgegdi-l1-1-0', 'ext-ms-win-gdi-dc-l1-2-0', 'ext-ms-win-ntuser-window-l1-1-0', 'api-ms-win-core-apiquery-l1-1-0',
             'ext-ms-win-rtcore-ntuser-sysparams-l1-1-0', 'ext-ms-win-kernel32-package-l1-1-0')

$WmiClasses = @('Win32_ComputerSystem', 'Win32_ComputerSystemProduct', 'Win32_BIOS', 'Win32_BaseBoard', 'Win32_Processor',
                'Win32_OperatingSystem', 'Win32_VideoController', 'Win32_DesktopMonitor', 'Win32_DiskDrive', 'Win32_DiskPartition',
                'Win32_LogicalDisk', 'Win32_Volume', 'Win32_PhysicalMemory', 'Win32_NetworkAdapter', 'Win32_NetworkAdapterConfiguration',
                'Win32_Process', 'Win32_Service', 'Win32_SystemDriver', 'Win32_SoundDevice', 'Win32_Printer', 'Win32_Battery',
                'Win32_TimeZone', 'Win32_QuickFixEngineering', 'Win32_Keyboard', 'Win32_PointingDevice', 'Win32_PnPEntity',
                'Win32_StartupCommand', 'Win32_Environment', 'Win32_ShadowCopy', 'Win32_Share')

# VM-specific values (identity.csv). The first eight Kind names are the old ones.
$IdentitySources = @(
    @{ Kind = 'user_name';         Where = 'registry;disk;sysinfo';  Source = '[Environment]::UserName' },
    @{ Kind = 'user_domain';       Where = 'registry;sysinfo';       Source = '[Environment]::UserDomainName' },
    @{ Kind = 'user_sid';          Where = 'registry;sd;sysinfo';    Source = '[Security.Principal.WindowsIdentity]::GetCurrent().User.Value' },
    @{ Kind = 'user_profile';      Where = 'registry;disk;sysinfo';  Source = '$env:USERPROFILE' },
    @{ Kind = 'computer_name';     Where = 'registry;sysinfo;objects'; Source = '$env:COMPUTERNAME' },
    @{ Kind = 'dns_host_name';     Where = 'registry;sysinfo';       Source = '[Net.Dns]::GetHostName()' },
    @{ Kind = 'machine_guid';      Where = 'registry';               Source = '(Get-ItemProperty -LiteralPath "HKLM:\SOFTWARE\Microsoft\Cryptography").MachineGuid' },
    @{ Kind = 'interactive_user';  Where = 'sysinfo';                Source = '(Get-CimInstance Win32_ComputerSystem).UserName' },
    @{ Kind = 'machine_sid';       Where = 'registry;sd;sysinfo';    Source = '$u = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value; $u.Substring(0, $u.LastIndexOf("-"))' },
    @{ Kind = 'logon_sid';         Where = 'sd;sysinfo;objects';     Source = '([Security.Principal.WindowsIdentity]::GetCurrent().Groups | Where-Object { $_.Value -like "S-1-5-5-*" } | Select-Object -First 1).Value' },
    @{ Kind = 'volume_serial';     Where = 'sysinfo;registry';       Source = '(Get-CimInstance Win32_LogicalDisk -Filter "DeviceID=''$($env:SystemDrive)''").VolumeSerialNumber' },
    @{ Kind = 'volume_guid';       Where = 'sysinfo;objects;registry'; Source = '(Get-CimInstance Win32_Volume -Filter "DriveLetter=''$($env:SystemDrive)''").DeviceID' },
    @{ Kind = 'smbios_uuid';       Where = 'sysinfo;registry';       Source = '(Get-CimInstance Win32_ComputerSystemProduct).UUID' },
    @{ Kind = 'bios_serial';       Where = 'sysinfo';                Source = '(Get-CimInstance Win32_BIOS).SerialNumber' },
    @{ Kind = 'baseboard_serial';  Where = 'sysinfo';                Source = '(Get-CimInstance Win32_BaseBoard).SerialNumber' },
    @{ Kind = 'hw_profile_guid';   Where = 'registry';               Source = '(Get-ItemProperty -LiteralPath "HKLM:\SYSTEM\CurrentControlSet\Control\IDConfigDB\Hardware Profiles\0001").HwProfileGuid' },
    @{ Kind = 'product_id';        Where = 'registry';               Source = '(Get-ItemProperty -LiteralPath "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion").ProductId' },
    @{ Kind = 'sqm_machine_id';    Where = 'registry';               Source = '(Get-ItemProperty -LiteralPath "HKLM:\SOFTWARE\Microsoft\SQMClient").MachineId' },
    @{ Kind = 'sus_client_id';     Where = 'registry';               Source = '(Get-ItemProperty -LiteralPath "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate").SusClientId' },
    @{ Kind = 'install_date';      Where = 'registry';               Source = '(Get-ItemProperty -LiteralPath "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion").InstallDate' },
    @{ Kind = 'last_boot';         Where = 'sysinfo';                Source = '(Get-CimInstance Win32_OperatingSystem).LastBootUpTime.ToUniversalTime().ToString("o")' }
)
# one row per item (several adapters / disks)
$IdentityMulti = @(
    @{ Kind = 'mac';          Where = 'sysinfo;registry';          Source = 'Get-CimInstance Win32_NetworkAdapter | Where-Object { $_.MACAddress } | ForEach-Object { $_.MACAddress + " (" + $_.Name + ")" }' },
    @{ Kind = 'adapter_guid'; Where = 'sysinfo;registry;objects';  Source = 'Get-CimInstance Win32_NetworkAdapter | Where-Object { $_.GUID } | ForEach-Object { $_.GUID + " (" + $_.Name + ")" }' },
    @{ Kind = 'ip_address';   Where = 'sysinfo;registry';          Source = 'Get-CimInstance Win32_NetworkAdapterConfiguration | Where-Object { $_.IPAddress } | ForEach-Object { $_.IPAddress } ' },
    @{ Kind = 'disk_serial';  Where = 'sysinfo;registry';          Source = 'Get-CimInstance Win32_DiskDrive | ForEach-Object { [string]$_.SerialNumber + " (" + $_.Model + ")" }' },
    @{ Kind = 'disk_model';   Where = 'sysinfo;registry';          Source = 'Get-CimInstance Win32_DiskDrive | ForEach-Object { $_.Model }' },
    @{ Kind = 'gpu_name';     Where = 'sysinfo;registry';          Source = 'Get-CimInstance Win32_VideoController | ForEach-Object { $_.Name + " / " + $_.PNPDeviceID }' }
)

# ---------- helpers ----------
function Say([string]$Text) { Write-Host ("[{0:HH:mm:ss}] {1}" -f (Get-Date), $Text) }

function Write-Err([string]$Kind, [string]$Path, [string]$Msg) {
    Add-Content -LiteralPath $script:ErrLog -Value ("{0}`t{1}`t{2}" -f $Kind, $Path, $Msg) -Encoding UTF8
}

function Test-Elevated {
    $p = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    return $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Invoke-Step([string]$Name, [scriptblock]$Body) {
    Say $Name
    try { & $Body } catch { Write-Err 'STEP' $Name $_.Exception.Message; Write-Warning ("{0}: {1}" -f $Name, $_.Exception.Message) }
}

function Save-Text([string]$Rel, [scriptblock]$Body) {
    $path = Join-Path $script:Root $Rel
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $path) | Out-Null
    try { (& $Body 2>&1 | Out-String -Width 4096) | Set-Content -LiteralPath $path -Encoding UTF8 }
    catch { Write-Err 'TEXT' $Rel $_.Exception.Message }
}

function Save-Json([string]$Rel, [scriptblock]$Body) {
    $path = Join-Path $script:Root $Rel
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $path) | Out-Null
    try { ConvertTo-Json -InputObject @(& $Body) -Depth 4 -Compress | Set-Content -LiteralPath $path -Encoding UTF8 }
    catch { Write-Err 'JSON' $Rel $_.Exception.Message }
}

function Format-RegCounts($C) {
    return ("keys {0} values {1} links {2} skipped {3} denied {4} excluded_values {5}" -f $C[0], $C[1], $C[2], $C[3], $C[4], $C[5])
}

# ---------- disk tree (same row format as the old collector: Path, Kind, Size, Attributes, LinkType, Target) ----------
function New-TreeRow([IO.FileSystemInfo]$Item, [string]$Kind) {
    $size = 0
    if ($Kind -eq 'file') { $size = $Item.Length }
    $script:DiskKinds[$Kind] = $script:DiskKinds[$Kind] + 1
    [pscustomobject]@{ Path = $Item.FullName; Kind = $Kind; Size = $size; Attributes = [int]$Item.Attributes; LinkType = ''; Target = '' }
}

function New-LinkRow([IO.FileSystemInfo]$Item) {
    $row = New-TreeRow $Item 'link'
    try {
        $gi = Get-Item -LiteralPath $Item.FullName -Force
        $row.LinkType = [string]$gi.LinkType
        $row.Target = (@($gi.Target) | Where-Object { $_ }) -join ';'
    } catch { Write-Err 'LINK' $Item.FullName $_.Exception.Message }
    $row
}

# D1 / D2: <profiles root>\<one profile>\<rule path>;  plus explicit folders (the output folder when it is on C:)
function Test-ProfileSkip([string]$Full, [string]$Root) {
    $rel = $Full.Substring($Root.TrimEnd('\').Length).TrimStart('\')
    $cut = $rel.IndexOf('\')
    if ($cut -lt 0) { return $false }
    $tail = $rel.Substring($cut + 1)
    foreach ($r in ($RuleD1ProfileDirs + $RuleD2ProfileDirs)) {
        if ([string]::Equals($tail, $r, [StringComparison]::OrdinalIgnoreCase)) { return $true }
    }
    return $false
}

function Test-ExplicitSkip([string]$Full) {
    foreach ($s in $script:SkipDirs) { if ([string]::Equals($Full.TrimEnd('\'), $s, [StringComparison]::OrdinalIgnoreCase)) { return $true } }
    return $false
}

function Get-ChildRow([IO.FileSystemInfo]$It, [string]$Mode, [bool]$UseD, [string]$Root, $SubDirs) {
    if ($It.Attributes -band [IO.FileAttributes]::ReparsePoint) { return (New-LinkRow $It) }
    if ($It -is [IO.DirectoryInfo]) {
        if (($UseD -and (Test-ProfileSkip $It.FullName $Root)) -or (Test-ExplicitSkip $It.FullName)) { return (New-TreeRow $It 'skip') }
        if ($Mode -eq 'one') { return (New-TreeRow $It 'dir') }
        $SubDirs.Add($It)
        return
    }
    if ($Mode -ne 'dirs') { New-TreeRow $It 'file' }
}

function Get-TreeRows([string]$Root, [string]$Mode, [bool]$UseD) {
    if (-not $Root) { Write-Err 'MISSING-TREE' '' 'empty path'; return }
    $rootInfo = New-Object IO.DirectoryInfo($Root)
    if (-not $rootInfo.Exists) { Write-Err 'MISSING-TREE' $Root ''; return }
    if ($rootInfo.Attributes -band [IO.FileAttributes]::ReparsePoint) { New-LinkRow $rootInfo; return }
    $stack = New-Object 'System.Collections.Generic.Stack[IO.DirectoryInfo]'
    $stack.Push($rootInfo)
    while ($stack.Count -gt 0) {
        $dir = $stack.Pop()
        try { $items = $dir.GetFileSystemInfos() }
        catch { New-TreeRow $dir 'denied'; Write-Err 'LIST' $dir.FullName $_.Exception.Message; continue }
        New-TreeRow $dir 'dir'
        $subDirs = New-Object 'System.Collections.Generic.List[IO.DirectoryInfo]'
        foreach ($item in $items) { Get-ChildRow $item $Mode $UseD $Root $subDirs }
        for ($i = $subDirs.Count - 1; $i -ge 0; $i--) { $stack.Push($subDirs[$i]) }
    }
}

# Walk the six roots; rows go to $CsvPath (or nowhere) and the path / size / kind lists are kept for meta and content.
function Invoke-DiskWalk([string]$CsvPath) {
    $script:DiskKinds = @{ dir = 0; file = 0; link = 0; skip = 0; denied = 0 }
    $script:TreePaths = New-Object 'System.Collections.Generic.List[string]'
    $script:TreeSizes = New-Object 'System.Collections.Generic.List[long]'
    $script:TreeIsDir = New-Object 'System.Collections.Generic.List[bool]'
    $rows = & {
        foreach ($d in $script:DiskRoots) {
            Say ("    disk {0} ({1})" -f $d.Path, $d.Mode)
            Get-TreeRows $d.Path $d.Mode $d.D
        }
    } | ForEach-Object {
        $script:TreePaths.Add($_.Path); $script:TreeSizes.Add([long]$_.Size); $script:TreeIsDir.Add($_.Kind -ne 'file')
        $_
    }
    if ($CsvPath) { $rows | Export-Csv -LiteralPath $CsvPath -NoTypeInformation -Encoding UTF8 }
}

function Get-IdentityRows {
    $out = New-Object System.Collections.Generic.List[object]
    foreach ($s in $IdentitySources) {
        $value = ''
        try { $value = [string](& ([scriptblock]::Create($s.Source))) } catch { Write-Err 'IDENTITY' $s.Kind $_.Exception.Message }
        $out.Add([pscustomobject]@{ Kind = $s.Kind; Value = $value; Source = $s.Source; Where = $s.Where })
    }
    foreach ($s in $IdentityMulti) {
        try {
            foreach ($v in @(& ([scriptblock]::Create($s.Source)))) {
                if ($v) { $out.Add([pscustomobject]@{ Kind = $s.Kind; Value = [string]$v; Source = $s.Source; Where = $s.Where }) }
            }
        } catch { Write-Err 'IDENTITY' $s.Kind $_.Exception.Message }
    }
    return $out
}

function Get-FileRows([string]$Dir) {
    Get-ChildItem -LiteralPath $Dir -Recurse -File -Force | ForEach-Object {
        $rel = $_.FullName.Substring($script:Root.Length + 1).Replace('\', '/')
        $rows = 0
        if ($_.Extension -eq '.jsonl' -or $_.Extension -eq '.csv' -or $_.Extension -eq '.txt') {
            $rows = 0; $r = New-Object IO.StreamReader($_.FullName); while ($null -ne $r.ReadLine()) { $rows++ }; $r.Dispose()
            if ($_.Extension -eq '.csv') { $rows = [Math]::Max(0, $rows - 1) }
        }
        [pscustomobject]@{ RelPath = $rel; Size = $_.Length; SHA256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash; Rows = $rows }
    }
}

function Invoke-Tar([string]$Name, [string[]]$TarArgs) {
    $archive = Join-Path $script:Root $Name
    $log = Join-Path $script:Root ("tar_{0}.log" -f [IO.Path]::GetFileNameWithoutExtension([IO.Path]::GetFileNameWithoutExtension($Name)))
    if (Test-Path -LiteralPath ($archive + '.sha256')) { Remove-Item -LiteralPath ($archive + '.sha256') -Force }
    Say ("    tar -> {0}" -f $Name)
    $p = Start-Process -FilePath $script:Tar -ArgumentList (@('-c', '-z', '-f', ('"' + $archive + '"')) + $TarArgs) -NoNewWindow -Wait -PassThru -RedirectStandardError $log
    $exit = $p.ExitCode
    $sha = ''; $bytes = 0
    if (Test-Path -LiteralPath $archive) {
        $bytes = (Get-Item -LiteralPath $archive).Length
        $sha = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
        [IO.File]::WriteAllText($archive + '.sha256', ($sha + '  ' + $Name + "`n"), $Utf8NoBom)
    }
    if ($exit -ne 0) { Write-Err 'TAR' $Name ("tar exit {0} (see {1})" -f $exit, (Split-Path -Leaf $log)) }
    return [pscustomobject]@{ Name = $Name; Exit = $exit; Bytes = $bytes; Sha256 = $sha }
}

# ---------- preflight (every mode) ----------
if (-not [Environment]::Is64BitProcess) { Write-Error "Run this in 64-bit PowerShell (32-bit sees redirected registry and System32)."; exit 1 }
$cv = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion'
$fullBuild = "{0}.{1}" -f $cv.CurrentBuild, $cv.UBR
if ($fullBuild -ne $TargetBuild) {
    Write-Error ("STOP: this VM is build {0}, the bundle must be {1} (the -full DLL dump would no longer match). Nothing was written." -f $fullBuild, $TargetBuild)
    exit 1
}
$elevated = Test-Elevated
$winName  = (Get-ChildItem -LiteralPath ($env:SystemDrive + '\') -Directory -Force | Where-Object { $_.Name -ieq (Split-Path -Leaf $env:windir) } | Select-Object -First 1).Name
if (-not $winName) { $winName = Split-Path -Leaf $env:windir }
$winRoot  = Join-Path ($env:SystemDrive + '\') $winName                         # disk spelling (I1), e.g. C:\Windows
$sysDrv   = $env:SystemDrive + '\'
$destFull = [IO.Path]::GetFullPath($Dest).TrimEnd('\')
if (-not (Test-Path -LiteralPath ([IO.Path]::GetPathRoot($destFull)))) { Write-Error ("Drive or share for -Dest not found: {0}" -f [IO.Path]::GetPathRoot($destFull)); exit 1 }
if ($destFull.ToLowerInvariant().StartsWith($winRoot.ToLowerInvariant())) { Write-Error "Dest must not be inside $winRoot"; exit 1 }
$destOnSystem = [string]::Equals([IO.Path]::GetPathRoot($destFull).TrimEnd('\'), $env:SystemDrive, [StringComparison]::OrdinalIgnoreCase)
if ($destOnSystem -and -not $AllowSystemDrive) {
    Write-Error ("Dest {0} is on the system drive. Use another disk / share (I4), or add -AllowSystemDrive (the folder is then skipped and named in info.txt)." -f $destFull)
    exit 1
}
$script:Root = Join-Path $destFull ("win11-{0}-{1}" -f $cv.DisplayVersion, $fullBuild)
New-Item -ItemType Directory -Force -Path $script:Root | Out-Null
$script:ErrLog = Join-Path $script:Root 'errors.log'
if (-not (Test-Path -LiteralPath $script:ErrLog)) { Set-Content -LiteralPath $script:ErrLog -Value "# kind`tpath`tmessage" -Encoding UTF8 }
$script:SkipDirs = @()
if ($destOnSystem) { $script:SkipDirs += $destFull }
$script:Tar = (Get-Command tar.exe -ErrorAction SilentlyContinue).Source

if (-not ('Vc' -as [type])) {
    $sources = @('VmCommon.cs', 'VmReg.cs', 'VmNt.cs', 'VmUi.cs', 'VmDisk.cs') | ForEach-Object { Join-Path $ScriptDir $_ }
    Add-Type -Path $sources -IgnoreWarnings
}
[Vc]::ErrorsPath = $script:ErrLog
$profilesDir = [Environment]::ExpandEnvironmentVariables((Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList').ProfilesDirectory)
$script:DiskRoots = @(
    @{ Name = 'system_drive';      Path = $sysDrv;                  Mode = 'one';  D = $false },
    @{ Name = 'users';             Path = $profilesDir;             Mode = 'full'; D = $true },
    @{ Name = 'program_data';      Path = $env:ProgramData;         Mode = 'full'; D = $false },
    @{ Name = 'program_files';     Path = $env:ProgramFiles;        Mode = 'full'; D = $false },
    @{ Name = 'program_files_x86'; Path = ${env:ProgramFiles(x86)}; Mode = 'full'; D = $false },
    @{ Name = 'windows';           Path = $winRoot;                 Mode = 'full'; D = $false }
)
[Disk]::DriveRoot = $sysDrv
[Disk]::SetRules([string[]]($ContentRules | ForEach-Object { $_[0] }), [string[]]($ContentRules | ForEach-Object { $_[1] }),
                 [string[]]($ContentRules | ForEach-Object { $_[2] }), [string[]]($ContentRules | ForEach-Object { $_[3] }))
foreach ($sub in @('registry', 'sd', 'disk', 'sysinfo', 'objects', 'probe')) { New-Item -ItemType Directory -Force -Path (Join-Path $script:Root $sub) | Out-Null }
$sessionId = (Get-Process -Id $PID).SessionId

Say ("mode {0}   build {1}   elevated {2}   output {3}" -f $Mode, $fullBuild, $elevated, $script:Root)

# ======================================================================================
if ($Mode -eq 'Check') {
    $free = $null
    try { $free = (New-Object IO.DriveInfo([IO.Path]::GetPathRoot($destFull))).AvailableFreeSpace } catch { }
    Write-Host ""
    Write-Host ("build          : {0} (OK - same as {1})" -f $fullBuild, $TargetBuild)
    Write-Host ("edition        : {0} {1}" -f $cv.EditionID, $cv.DisplayVersion)
    Write-Host ("elevated       : {0}" -f $elevated)
    Write-Host ("windows folder : {0}" -f $winRoot)
    Write-Host ("dest           : {0}  (system drive: {1})" -f $script:Root, $destOnSystem)
    Write-Host ("free at dest   : {0}" -f $(if ($free -ne $null) { "{0:N1} GB (Full needs about {1:N0} GB)" -f ($free / 1GB), ($MinFreeFull / 1GB) } else { 'unknown (share?)' }))
    Write-Host ("tar.exe        : {0}" -f $(if ($script:Tar) { $script:Tar } else { 'MISSING' }))
    Write-Host ("C# helpers     : compiled OK")
    Write-Host ("vm_probe.exe   : {0}" -f $(if (Test-Path -LiteralPath $ProbeExe) { $ProbeExe } else { "not found at $ProbeExe (build with sample\build_vm.bat)" }))
    Write-Host ("user part      : {0}" -f (Test-Path -LiteralPath (Join-Path $script:Root 'user_part.done')))
    Write-Host ("net parts      : unplugged {0} / plugged {1}" -f (Test-Path -LiteralPath (Join-Path $script:Root 'sysinfo\net_unplugged')), (Test-Path -LiteralPath (Join-Path $script:Root 'sysinfo\net_plugged')))
    Write-Host ("dry run        : {0}" -f (Test-Path -LiteralPath (Join-Path $script:Root 'dryrun.done')))
    exit 0
}

# ======================================================================================
if ($Mode -eq 'User') {
    if ($elevated) { Write-Error "User mode must run in a NORMAL PowerShell (not 'Run as administrator'): it records the unelevated token and environment."; exit 1 }
    $sys = Join-Path $script:Root 'sysinfo'
    Invoke-Step 'A3 token (unelevated + linked)' { [Nt]::DumpToken((Join-Path $sys 'token_user.jsonl'), 'user', $true) }
    Invoke-Step 'A3 whoami /all (unelevated)' { Save-Text 'probe\whoami_all_user.txt' { whoami.exe /all } }
    Invoke-Step 'A6 environment' {
        [Ui]::DumpEnvironmentBlock((Join-Path $sys 'environment.jsonl'), 'user')
        Save-Json 'sysinfo\environment_scopes_user.json' {
            foreach ($scope in @('Process', 'User', 'Machine')) {
                $vars = [Environment]::GetEnvironmentVariables($scope)
                [pscustomobject]@{ scope = $scope; vars = @($vars.Keys | Sort-Object | ForEach-Object { [pscustomobject]@{ name = $_; value = $vars[$_] } }) }
            }
        }
    }
    Invoke-Step 'A2 process / thread information (unelevated)' { [Nt]::DumpProcessThreadInformation((Join-Path $sys 'process_user.jsonl'), 'user') }
    Invoke-Step 'A10 locale' { [Ui]::DumpLocale((Join-Path $sys 'locale.jsonl'), 'user') }
    Invoke-Step 'A21 display / GDI / message font' { [Ui]::DumpDisplay((Join-Path $sys 'display.jsonl'), 'user') }
    Invoke-Step 'A14 fonts' { [Ui]::DumpFonts((Join-Path $sys 'fonts.jsonl'), 'user') }
    Invoke-Step 'A14 sound' { [Ui]::DumpSound((Join-Path $sys 'sound.jsonl'), 'user') }
    Invoke-Step 'A22 window station / desktop / power' { [Ui]::DumpSession((Join-Path $sys 'session.jsonl'), 'user') }
    Invoke-Step 'A7 top-level windows (user)' { [Ui]::DumpWindows((Join-Path $sys 'windows_user.jsonl'), 'user') }
    Invoke-Step 'api sets / mitigation (user)' { [Nt]::DumpApiSetsAndMitigation((Join-Path $sys 'apiset_mitigation.jsonl'), 'user', [string[]]$ApiSets) }
    Invoke-Step 'P6 clipboard / FindWindow / wait' { [Ui]::ProbeUser((Join-Path $script:Root 'probe\p6_user.jsonl'), 'user') }
    [IO.File]::WriteAllText((Join-Path $script:Root 'user_part.done'), ((Get-Date).ToUniversalTime().ToString('o') + "`n"), $Utf8NoBom)
    Say ("user part done in {0:N0} s" -f $sw.Elapsed.TotalSeconds)
    exit 0
}

# ======================================================================================
if ($Mode -eq 'Net') {
    if (-not $NetTag) { Write-Error "Net mode needs -NetTag unplugged or -NetTag plugged."; exit 1 }
    $rel = "sysinfo\net_$NetTag"
    New-Item -ItemType Directory -Force -Path (Join-Path $script:Root $rel) | Out-Null
    Invoke-Step "A11 network ($NetTag)" {
        Save-Text "$rel\ipconfig_all.txt" { ipconfig.exe /all }
        Save-Text "$rel\route_print.txt" { route.exe print }
        Save-Text "$rel\arp_a.txt" { arp.exe -a }
        Save-Text "$rel\netsh_ipv4_config.txt" { netsh.exe interface ipv4 show config }
        Save-Text "$rel\netsh_ipv6_addresses.txt" { netsh.exe interface ipv6 show addresses }
        Save-Text "$rel\nslookup_example.txt" { nslookup.exe example.com }
        Save-Json "$rel\netadapter.json" { Get-NetAdapter -IncludeHidden | Select-Object * -ExcludeProperty Cim* }
        Save-Json "$rel\netipaddress.json" { Get-NetIPAddress | Select-Object * -ExcludeProperty Cim* }
        Save-Json "$rel\netipinterface.json" { Get-NetIPInterface | Select-Object * -ExcludeProperty Cim* }
        Save-Json "$rel\netroute.json" { Get-NetRoute | Select-Object * -ExcludeProperty Cim* }
        Save-Json "$rel\netneighbor.json" { Get-NetNeighbor | Select-Object * -ExcludeProperty Cim* }
        Save-Json "$rel\dnsclient.json" { Get-DnsClientServerAddress | Select-Object * -ExcludeProperty Cim* }
        Save-Json "$rel\connectionprofile.json" { Get-NetConnectionProfile | Select-Object * -ExcludeProperty Cim* }
    }
    Say ("net part '{0}' done in {1:N0} s" -f $NetTag, $sw.Elapsed.TotalSeconds)
    exit 0
}

# ======================================================================================
if (-not $elevated) { Write-Error "$Mode mode must run in an elevated PowerShell ('Run as administrator')."; exit 1 }

if ($Mode -eq 'DryRun') {
    Say "dry run: disk walk (names only, nothing copied)"
    Invoke-DiskWalk $null
    $sum = [Disk]::DryRun($script:TreePaths.ToArray(), $script:TreeSizes.ToArray(), $script:TreeIsDir.ToArray(),
                          (Join-Path $script:Root 'dryrun.csv'), (Join-Path $script:Root 'dryrun_top.csv'))
    $rules = Import-Csv -LiteralPath (Join-Path $script:Root 'dryrun.csv')
    $top   = Import-Csv -LiteralPath (Join-Path $script:Root 'dryrun_top.csv')
    Write-Host ""
    Write-Host "=== per rule (files whose BYTES are collected = Mode content) ==="
    $rules | Sort-Object { [long]$_.Bytes } -Descending | Format-Table Rule, Mode, @{ n = 'Files'; e = { [long]$_.Files } }, @{ n = 'GB'; e = { '{0:N2}' -f ([long]$_.Bytes / 1GB) } }, Pattern -AutoSize | Out-String -Width 200 | Write-Host
    Write-Host "=== biggest content folders (top 25) ==="
    $top | Where-Object { $_.Mode -eq 'content' } | Sort-Object { [long]$_.Bytes } -Descending | Select-Object -First 25 |
        Format-Table Folder, @{ n = 'Files'; e = { [long]$_.Files } }, @{ n = 'GB'; e = { '{0:N2}' -f ([long]$_.Bytes / 1GB) } } -AutoSize | Out-String -Width 200 | Write-Host
    $kinds = $script:DiskKinds
    Write-Host ("tree rows      : dir {0} file {1} link {2} skip {3} denied {4}" -f $kinds['dir'], $kinds['file'], $kinds['link'], $kinds['skip'], $kinds['denied'])
    Write-Host ("content total  : {0:N0} files, {1:N2} GB (unpacked; tar.gz about 40 %)" -f $sum[0], ($sum[1] / 1GB))
    Write-Host ("elapsed        : {0:N0} s" -f $sw.Elapsed.TotalSeconds)
    [IO.File]::WriteAllText((Join-Path $script:Root 'dryrun.done'), ("{0}`ncontent_files {1}`ncontent_bytes {2}`n" -f (Get-Date).ToUniversalTime().ToString('o'), $sum[0], $sum[1]), $Utf8NoBom)
    Write-Host ""
    Write-Host ("next: show {0} and {1} (or this screen) to the analyst, then run -Mode Full." -f (Join-Path $script:Root 'dryrun.csv'), (Join-Path $script:Root 'dryrun_top.csv'))
    exit 0
}

# ======================================================================================
# Full
$guards = @()
if (-not (Test-Path -LiteralPath (Join-Path $script:Root 'dryrun.done'))) { $guards += 'no dry run (run -Mode DryRun first and have it checked)' }
if (-not (Test-Path -LiteralPath (Join-Path $script:Root 'user_part.done'))) { $guards += 'no user part (run -Mode User in a normal PowerShell)' }
if (-not (Test-Path -LiteralPath (Join-Path $script:Root 'sysinfo\net_unplugged'))) { $guards += 'no net part "unplugged"' }
if (-not (Test-Path -LiteralPath (Join-Path $script:Root 'sysinfo\net_plugged'))) { $guards += 'no net part "plugged"' }
if (-not $script:Tar) { $guards += 'tar.exe not found' }
try { $free = (New-Object IO.DriveInfo([IO.Path]::GetPathRoot($destFull))).AvailableFreeSpace; if ($free -lt $MinFreeFull) { $guards += ("free space {0:N1} GB < {1:N0} GB" -f ($free / 1GB), ($MinFreeFull / 1GB)) } } catch { }
if ($guards.Count -gt 0 -and -not $Force) { Write-Error ("STOP: " + ($guards -join '; ') + ". Fix it or add -Force."); exit 1 }
foreach ($g in $guards) { Write-Err 'GUARD-FORCED' '' $g }

$startedUtc = (Get-Date).ToUniversalTime().ToString('o')
$regDir = Join-Path $script:Root 'registry'
$sys = Join-Path $script:Root 'sysinfo'
$probe = Join-Path $script:Root 'probe'
[SdTable]::OpenTable((Join-Path $script:Root 'sd\sd_table.jsonl'))

# ---------- identity ----------
$identity = @()
Invoke-Step 'identity (VM-specific values)' {
    $script:identity = @(Get-IdentityRows)
    $script:identity | Export-Csv -LiteralPath (Join-Path $script:Root 'identity.csv') -NoTypeInformation -Encoding UTF8
}
$sid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value

# ---------- registry (old five + .DEFAULT + two service accounts) ----------
$RegCounts = [ordered]@{}
$excludedPath = Join-Path $regDir 'excluded.jsonl'
[IO.File]::WriteAllText($excludedPath, '', $Utf8NoBom)
[IO.File]::WriteAllText((Join-Path $regDir 'key_meta.jsonl'), '', $Utf8NoBom)
[RegWalker]::SetRules([string[]]$RuleK1KeySuffix, [string[]]$RuleK2LeafSuffix, [string[]]$RuleV1Contains, [string[]]$RuleV1Prefix, [string[]]$RuleV1Equals)
foreach ($r in $RegRoots) {
    $sub = $r.Sub.Replace('<SID>', $sid)
    $nt  = $r.Nt.Replace('<SID>', $sid)
    Say "registry $nt"
    try { $RegCounts[$r.File] = [RegWalker]::WalkRoot($r.Hive, $sub, $nt, (Join-Path $regDir $r.File), $excludedPath, $script:ErrLog, (Join-Path $regDir 'key_meta.jsonl'), $r.File) }
    catch { Write-Err 'REGISTRY' $nt $_.Exception.Message; $RegCounts[$r.File] = [long[]](0, 0, 0, 0, 0, 0) }
}
Invoke-Step 'A16 registry roots and closed hives' { [RegWalker]::WriteRoots((Join-Path $regDir 'roots.jsonl')) }

# ---------- disk ----------
Say "disk tree"
Invoke-DiskWalk (Join-Path $script:Root 'disk\tree.csv')
$treeKinds = $script:DiskKinds.Clone()
Say "disk meta (times, ids, short names, links, streams, SD)"
$metaRows = [Disk]::Meta($script:TreePaths.ToArray(), (Join-Path $script:Root 'disk\tree_meta.csv'))
Say "disk content (hash, version, machine, tar list)"
$cPaths = New-Object 'System.Collections.Generic.List[string]'
$cSizes = New-Object 'System.Collections.Generic.List[long]'
$seen = @{}
for ($i = 0; $i -lt $script:TreePaths.Count; $i++) {
    if ($script:TreeIsDir[$i]) { continue }
    $p = $script:TreePaths[$i]
    if ($seen.ContainsKey($p)) { continue }
    $seen[$p] = $true
    if ([Disk]::ModeOf([Disk]::Classify($p, $false)) -eq 'content') { $cPaths.Add($p); $cSizes.Add($script:TreeSizes[$i]) }
}
$seen = $null
$staging = Join-Path $script:Root 'C_nonansi'
$contentCounts = [Disk]::Content($cPaths.ToArray(), $cSizes.ToArray(), (Join-Path $script:Root 'disk\content.csv'), (Join-Path $script:Root 'disk\content_list.txt'), $staging)

# ---------- sysinfo / objects / probes (elevated) ----------
Invoke-Step 'A2 system information classes' { [Nt]::DumpSystemInformation((Join-Path $sys 'system_information.jsonl')) }
Invoke-Step 'A2 process / thread information (elevated)' { [Nt]::DumpProcessThreadInformation((Join-Path $sys 'process_admin.jsonl'), 'admin') }
Invoke-Step 'A2 KUSER_SHARED_DATA' { [Nt]::DumpKusd((Join-Path $sys 'kusd.jsonl')) }
Invoke-Step 'A3 token (elevated + linked)' { [Nt]::DumpToken((Join-Path $sys 'token_admin.jsonl'), 'admin', $true) }
Invoke-Step 'A3 whoami /all (elevated)' { Save-Text 'probe\whoami_all_admin.txt' { whoami.exe /all } }
Invoke-Step 'A4 license values' {
    $policy = (Get-ItemProperty -LiteralPath 'HKLM:\SYSTEM\CurrentControlSet\Control\ProductOptions').ProductPolicy
    [Nt]::DumpLicense((Join-Path $sys 'license.jsonl'), [byte[]]$policy)
}
Invoke-Step 'A4 MUI registry info' { [Nt]::DumpMui((Join-Path $sys 'mui.jsonl')) }
Invoke-Step 'A6 environment (elevated)' { [Ui]::DumpEnvironmentBlock((Join-Path $sys 'environment.jsonl'), 'admin') }
Invoke-Step 'A7 processes / services / drivers' {
    Save-Json 'sysinfo\processes.json' { Get-CimInstance Win32_Process | Select-Object Name, ProcessId, ParentProcessId, SessionId, ExecutablePath, CommandLine, CreationDate, HandleCount, ThreadCount, Priority }
    Save-Json 'sysinfo\services.json' { Get-CimInstance Win32_Service | Select-Object * -ExcludeProperty Cim* }
    Save-Text 'sysinfo\sc_queryex.txt' { sc.exe queryex type= all state= all }
    Save-Text 'sysinfo\driverquery.csv' { driverquery.exe /v /fo csv }
    Save-Text 'sysinfo\tasklist.csv' { tasklist.exe /v /fo csv }
    [Ui]::DumpWindows((Join-Path $sys 'windows_admin.jsonl'), 'admin')
}
Invoke-Step 'A10 locale (elevated)' { [Ui]::DumpLocale((Join-Path $sys 'locale.jsonl'), 'admin') }
Invoke-Step 'A12 volumes / mount manager / disk' { [Nt]::DumpVolumes((Join-Path $sys 'volumes.jsonl'), $env:SystemDrive, (Join-Path $winRoot 'System32\ntdll.dll'), $winRoot) }
Invoke-Step 'A18 WMI classes' {
    foreach ($c in $WmiClasses) { Save-Json ("sysinfo\wmi\{0}.json" -f $c) { Get-CimInstance -ClassName $c | Select-Object * -ExcludeProperty Cim* } }
    Save-Json 'sysinfo\wmi\SecurityCenter2_AntiVirusProduct.json' { Get-CimInstance -Namespace root/SecurityCenter2 -ClassName AntiVirusProduct | Select-Object * -ExcludeProperty Cim* }
}
Invoke-Step 'A20 firmware tables' { [Nt]::DumpFirmware((Join-Path $sys 'firmware.jsonl')) }
Invoke-Step 'A22 printers' { Save-Json 'sysinfo\printers.json' { Get-Printer | Select-Object * -ExcludeProperty Cim* } }
Invoke-Step 'api sets / mitigation (elevated)' { [Nt]::DumpApiSetsAndMitigation((Join-Path $sys 'apiset_mitigation.jsonl'), 'admin', [string[]]$ApiSets) }
Invoke-Step 'misc text (systeminfo, hotfixes, appinfo service)' {
    Save-Text 'sysinfo\systeminfo.txt' { systeminfo.exe }
    Save-Json 'sysinfo\hotfixes.json' { Get-HotFix | Select-Object HotFixID, Description, InstalledOn }
    Save-Text 'probe\sc_qc_appinfo.txt' { sc.exe qc appinfo; sc.exe query appinfo }
}
Invoke-Step 'A1 object name space' { [Nt]::DumpObjects((Join-Path $script:Root 'objects\namespace.jsonl')) }
Invoke-Step 'P1 registry query shapes' { [Nt]::ProbeRegistry((Join-Path $probe 'p1_registry.jsonl')) }
Invoke-Step 'P2 file / volume / object information' { [Nt]::ProbeFiles((Join-Path $probe 'p2_files.jsonl'), $sysDrv, (Join-Path $winRoot 'System32\ntdll.dll')) }
Invoke-Step 'P3 sections / memory' { [Nt]::ProbeSections((Join-Path $probe 'p3_sections.jsonl'), $sessionId) }
Invoke-Step 'P4 pipe' { [Nt]::ProbePipe((Join-Path $probe 'p4_pipe.jsonl')) }
Invoke-Step 'P6 (elevated)' { [Ui]::ProbeUser((Join-Path $probe 'p6_admin.jsonl'), 'admin') }
if (Test-Path -LiteralPath $ProbeExe) {
    foreach ($m in @('msgbox', 'dialog', 'debug')) {
        Invoke-Step "vm_probe.exe $m (windows open and close by themselves)" {
            $out = Join-Path $probe ("vm_probe_{0}.jsonl" -f $m)
            $errOut = Join-Path $probe ("vm_probe_{0}.stderr.txt" -f $m)
            $p = Start-Process -FilePath $ProbeExe -ArgumentList $m -NoNewWindow -PassThru -RedirectStandardOutput $out -RedirectStandardError $errOut
            if (-not $p.WaitForExit(300000)) { $p.Kill(); Write-Err 'PROBE' $m 'timeout 300 s, killed' }
            elseif ($p.ExitCode -ne 0) { Write-Err 'PROBE' $m ("exit {0}" -f $p.ExitCode) }
        }
    }
} else { Write-Err 'PROBE' $ProbeExe 'vm_probe.exe not found - msgbox / dialog / debug probes skipped' }
[SdTable]::CloseTable()
$finishedUtc = (Get-Date).ToUniversalTime().ToString('o')

# ---------- manifest (every file that goes into registry.tar.gz / meta.tar.gz) ----------
Say "manifest"
$manifest = @()
foreach ($sub in @('registry', 'sd', 'disk', 'sysinfo', 'objects', 'probe')) { $manifest += @(Get-FileRows (Join-Path $script:Root $sub)) }
$manifest | Export-Csv -LiteralPath (Join-Path $script:Root 'manifest.csv') -NoTypeInformation -Encoding UTF8

# ---------- info.txt ----------
$info = New-Object System.Collections.Generic.List[string]
function Add-Info([string]$Name, $Value) { $info.Add(("{0,-28}: {1}" -f $Name, $Value)) }
Add-Info 'collected_at_utc' $startedUtc
Add-Info 'finished_at_utc' $finishedUtc
Add-Info 'layout_version' $LayoutVersion
Add-Info 'product_name' ("{0}   (note: may read 'Windows 10' even on Windows 11)" -f $cv.ProductName)
Add-Info 'edition_id' $cv.EditionID
Add-Info 'display_version' $cv.DisplayVersion
Add-Info 'full_build' $fullBuild
Add-Info 'build_lab_ex' $cv.BuildLabEx
Add-Info 'system_locale' ((Get-WinSystemLocale).Name)
Add-Info 'ui_culture' ("{0} / {1}" -f (Get-Culture).Name, (Get-UICulture).Name)
Add-Info 'ansi_codepage' ((Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Nls\CodePage').ACP)
Add-Info 'time_zone' ((Get-TimeZone).Id)
Add-Info 'collector' ("collect_vm.ps1 (Dest={0}, Force={1}, AllowSystemDrive={2})" -f $Dest, $Force, $AllowSystemDrive)
Add-Info 'output_folder' $script:Root
Add-Info 'output_on_system_drive' ("{0}  (if True: that folder is a 'skip' row in tree.csv and must be hidden)" -f $destOnSystem)
Add-Info 'script_user' ("{0}\{1}" -f [Environment]::UserDomainName, [Environment]::UserName)
Add-Info 'windows_dir_disk_name' ("{0}  (C/ uses this spelling, I1)" -f $winRoot)
foreach ($d in $script:DiskRoots) { Add-Info ('disk_root_' + $d.Name) ("{0}  ({1})" -f $d.Path, $d.Mode) }
foreach ($r in $RegRoots) { Add-Info ('reg_' + [IO.Path]::GetFileNameWithoutExtension($r.File)) (Format-RegCounts $RegCounts[$r.File]) }
Add-Info 'disk_kinds' ("dir {0} file {1} link {2} skip {3} denied {4}" -f $treeKinds['dir'], $treeKinds['file'], $treeKinds['link'], $treeKinds['skip'], $treeKinds['denied'])
Add-Info 'disk_meta_rows' $metaRows
Add-Info 'content' ("files {0} bytes {1} unreadable {2} non_ansi {3}" -f $contentCounts[0], $contentCounts[1], $contentCounts[2], $contentCounts[3])
Add-Info 'sd_rows' ([SdTable]::Rows)
Add-Info 'rule_K1' ($RuleK1KeySuffix -join ' | ')
Add-Info 'rule_K2' ($RuleK2LeafSuffix -join ' | ')
Add-Info 'rule_V1_contains' ($RuleV1Contains -join ' | ')
Add-Info 'rule_V1_prefix' ($RuleV1Prefix -join ' | ')
Add-Info 'rule_V1_equals' ($RuleV1Equals -join ' | ')
Add-Info 'rule_D1' ($RuleD1ProfileDirs -join ' | ')
Add-Info 'rule_D2' ($RuleD2ProfileDirs -join ' | ')
foreach ($cr in $ContentRules) { Add-Info ('content_rule ' + $cr[3]) ("{0}  {1}  {2}" -f $cr[2], $cr[1], $cr[0]) }
Add-Info 'content_default' 'name  (tree.csv + tree_meta.csv rows only)'
Add-Info 'vm_unique_kinds' ((@($IdentitySources | ForEach-Object { $_.Kind }) + @($IdentityMulti | ForEach-Object { $_.Kind })) -join ' | ')
Add-Info 'vm_unique_also' 'SMBIOS (sysinfo/firmware.jsonl RSMB), tokens / SD (user and machine SID, logon SID), objects (pid-named, volume GUID, VMware devices), processes / services / drivers / windows (VMware tools), WMI, volume serial and disk model in sysinfo/volumes.jsonl'
$info | Set-Content -LiteralPath (Join-Path $script:Root 'info.txt') -Encoding UTF8

# ---------- pack ----------
Say "packing (content.tar.gz is the long one)"
$tars = @()
$tars += Invoke-Tar 'registry.tar.gz' @('-C', ('"' + $script:Root + '"'), 'registry', 'sd')
$tars += Invoke-Tar 'meta.tar.gz' @('-C', ('"' + $script:Root + '"'), 'disk', 'sysinfo', 'objects', 'probe')
$tars += Invoke-Tar 'content.tar.gz' @('-C', ($env:SystemDrive + '/'), '-T',   # 'C:/' - a quoted "C:\" would eat the quote
    ('"' + (Join-Path $script:Root 'disk\content_list.txt') + '"'))
if (Test-Path -LiteralPath $staging) { $tars += Invoke-Tar 'content_nonansi.tar.gz' @('-C', ('"' + $staging + '"'), '.') }

$info2 = New-Object System.Collections.Generic.List[string]
foreach ($t in $tars) { $info2.Add(("{0,-28}: exit {1}  bytes {2}  sha256 {3}" -f ('archive ' + $t.Name), $t.Exit, $t.Bytes, $t.Sha256)) }
$errCount = (Get-Content -LiteralPath $script:ErrLog | Measure-Object).Count - 1
$info2.Add(("{0,-28}: {1}" -f 'errors', $errCount))
$info2.Add(("{0,-28}: {1}" -f 'elapsed_s', [int]$sw.Elapsed.TotalSeconds))
Add-Content -LiteralPath (Join-Path $script:Root 'info.txt') -Value $info2 -Encoding UTF8

# remove unpacked folders only when their archive is good (content.tar.gz reads C:\ directly - nothing to remove)
$byName = @{}; foreach ($t in $tars) { $byName[$t.Name] = $t }
if ($byName['registry.tar.gz'].Exit -eq 0 -and $byName['meta.tar.gz'].Exit -eq 0) {
    foreach ($sub in @('registry', 'sd', 'disk', 'sysinfo', 'objects', 'probe')) { Remove-Item -LiteralPath (Join-Path $script:Root $sub) -Recurse -Force }
    if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
} else { Write-Warning "registry / meta tar failed: unpacked folders were kept. See errors.log and tar_*.log." }

$sw.Stop()
Write-Host ""
Write-Host "=== summary ==="
Write-Host ("output   : {0}" -f $script:Root)
foreach ($r in $RegRoots) { Write-Host ("  {0,-28} {1}" -f $r.File, (Format-RegCounts $RegCounts[$r.File])) }
Write-Host ("disk     : dir {0} file {1} link {2} skip {3} denied {4}   meta rows {5}" -f $treeKinds['dir'], $treeKinds['file'], $treeKinds['link'], $treeKinds['skip'], $treeKinds['denied'], $metaRows)
Write-Host ("content  : {0} files  {1:N2} GB  unreadable {2}  non-ANSI {3}" -f $contentCounts[0], ($contentCounts[1] / 1GB), $contentCounts[2], $contentCounts[3])
foreach ($t in $tars) { Write-Host ("archive  : {0,-24} exit {1}  {2:N2} GB" -f $t.Name, $t.Exit, ($t.Bytes / 1GB)) }
Write-Host ("errors   : {0}  (errors.log)" -f $errCount)
Write-Host ("time     : {0:N0} min" -f ($sw.Elapsed.TotalMinutes))
Write-Host "next     : copy the whole output folder to the host (see sample\vm-collect\README.md), do not extract on the VM"
