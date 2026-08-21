$b = [char]::ConvertFromUtf32(0x25cf)
Clear-Host
Write-Output ""
Write-Host -ForegroundColor Red "	 $b "
Write-Host -ForegroundColor Yellow -NoNewLine "	$b "
Write-Host -ForegroundColor Green -NoNewLine "$b   "
Write-Host "markuse arvuti asjad"
Write-Host -ForegroundColor Blue "	 $b "
Write-Output ""
$build_flags = ("-c", "Release",
				"-o", "out",
				"-p:PublishReadyToRun=true",
				"-p:PublishSingleFile=true",
				"-p:PublishTrimmed=true",
				"--self-contained", "true",
				"-p:IncludeNativeLibrariesForSelfExtract=true",
				"-p:DebugSymbols=false")
dotnet publish AvIntegrationSoftware $build_flags
$mas_root = [environment]::getfolderpath("userprofile") + "/.mas"
$platform = [System.Environment]::OSVersion.Platform
$suff = ""
if ($platform -eq "Win32NT")
{
	$suff = ".exe"
}
Copy-Item -Path out/AvIntegrationSoftware$suff -Destination "$mas_root/Markuse asjad/Markuse arvuti integratsioonitarkvara$suff" -Verbose
