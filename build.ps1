# display text version of markus' computer stuff logo
$b = [char]::ConvertFromUtf32(0x25cf) # for legacy PowerShell compatbility and avoiding encoding problems
Clear-Host
Write-Output ""
Write-Host -ForegroundColor Red "	 $b "
Write-Host -ForegroundColor Yellow -NoNewLine "	$b "
Write-Host -ForegroundColor Green -NoNewLine "$b   "
Write-Host "markuse arvuti asjad"
Write-Host -ForegroundColor Blue "	 $b "
Write-Output ""
# will get passed to dotnet publish
$build_flags = ("-c", "Release",                                # configuration (Debug or Release)
				"-o", "out",                                    # output directory (do not change or the rest of this script will break)
				"-p:PublishReadyToRun=true",                    # improves startup time
				"-p:PublishSingleFile=true",                    # tries to avoid creating extra files
				"-p:PublishTrimmed=true",                       # trims out the unneeded stuff to reduce file sizes (NOTE: can cause issues in certain edge-cases, check compiler warnings)
				"--self-contained", "true",                     # contains .NET runtime within the output executable
				"-p:IncludeNativeLibrariesForSelfExtract=true", # includes .so/.dll files inside the executable for extraction at runtime
				"-p:DebugSymbols=false")                        # doesn't create .pdb files
# MasCommon is a library, so it'll be included with whatever needs it
# out is output directory  and the dot directories are temp files for
# various IDEs, images for readme and Git version control
$exclusions = @('.vscode', '.git', 'MasCommon', 'out', '.img', '.idea', '.vs')
Get-ChildItem . -Directory -Exclude $exclusions | Foreach-Object {
    dotnet publish $_.Name $build_flags
}

# if this is not Markus' computer, do not try to copy the output files
$mas_root = [environment]::getfolderpath("userprofile") + "/.mas"
if (-Not (Test-Path -Path "$mas_root")) {
    Write-Host "Will not update Markus' computer stuff. Reason: Directory '$mas_root' does not exist!"
    exit
}
# create '<mas_root>/Markuse asjad' if it doesn't exist
if (-Not (Test-Path -Path "$mas_root/Markuse asjad")) {
    Write-Host "Created Markus' stuff directory"
    New-Item -Path "$mas_root" -Name "Markuse asjad" -ItemType Directory
}

# Windows has .exe file extension for executables
$platform = [System.Environment]::OSVersion.Platform
$suff = ""
if ($platform -eq "Win32NT")
{
	$suff = ".exe"
}
Get-ChildItem . -Directory -Exclude $exclusions | Foreach-Object {
	# this stores just the directory name, not the full path
	$pn = $_.Name
	$op = $pn
	if ($op -eq "AvIntegrationSoftware") {
	    $op = "Markuse arvuti integratsioonitarkvara"
	}
	# we pass SilentlyContinue, otherwise PS spit errors at us if the process doesn't exist
	$p = Get-Process -Name $op -ErrorAction SilentlyContinue
	# check if process is running, if it is then stop it
	if ($p) {
		Write-Output "- Killing $op"
		Stop-Process -InputObject $p -Force
		Write-Output "- Waiting 5 seconds" # prevent file access violations
	    Start-Sleep -Seconds 5.0
	} 
    Copy-Item -Path out/$pn$suff -Destination "$mas_root/Markuse asjad/$op$suff" -Verbose
    # restart the process if it was running
	if ($p) {
		Write-Output "- Restarting $op"
		Start-Process -FilePath "$mas_root/Markuse asjad/$op$suff"
	} 
}
