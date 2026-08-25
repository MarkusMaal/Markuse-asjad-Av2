#!/usr/bin/env bash

# Bash conversion of build.ps1 (AI-assisted \w ratio B80/H20)

# Display text version of markus' computer stuff logo
b="\u25cf"
clear
echo ""
echo -e "\t \e[31m$b\e[0m "
echo -ne "\t\e[33m$b\e[0m "
echo -ne "\e[32m$b\e[0m   "
echo "markuse arvuti asjad"
echo -e "\t \e[34m$b\e[0m "
echo ""

# Will get passed to dotnet publish
build_flags=(
    "-c" "Release"
    "-o" "out"
    "-p:PublishReadyToRun=true"
    "-p:PublishSingleFile=true"
    "-p:PublishTrimmed=true"
    "--self-contained" "true"
    "-p:IncludeNativeLibrariesForSelfExtract=true"
    "-p:DebugSymbols=false"
)

# Exclusions array
exclusions=('.vscode' '.git' 'MasCommon' 'out' '.img' '.idea' '.vs')

is_excluded() {
    local dir="$1"
    for ex in "${exclusions[@]}"; do
        if [[ "$dir" == "$ex" ]]; then
            return 0
        fi
    done
    return 1
}

# Publish directories
for dir in */; do
    pn="${dir%/}"
    if is_excluded "$pn"; then
        continue
    fi
    dotnet publish "$pn" "${build_flags[@]}"
done

# If this is not Markus' computer, do not try to copy the output files
mas_root="$HOME/.mas"
if [[ ! -d "$mas_root" ]]; then
    echo "Will not update Markus' computer stuff. Reason: Directory '$mas_root' does not exist!"
    exit 0
fi

# Create '<mas_root>/Markuse asjad' if it doesn't exist
if [[ ! -d "$mas_root/Markuse asjad" ]]; then
    echo "Created Markus' stuff directory"
    mkdir -p "$mas_root/Markuse asjad"
fi

# Determine platform-specific extension
suff=""
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" || "$OSTYPE" == "cygwin" ]]; then
    suff=".exe"
fi

for dir in */; do
    pn="${dir%/}"
    if is_excluded "$pn"; then
        continue
    fi
    
    op="$pn"
    if [[ "$op" == "AvIntegrationSoftware" ]]; then
        op="Markuse arvuti integratsioonitarkvara"
    fi
    wasrunning="false"
    # Check if process is running and stop it
    if pgrep -f "$op" >/dev/null; then
        echo "- Killing $op"
        pkill -f "$op"
        echo "- Waiting 5 seconds"
        sleep 5
        wasrunning="true"
    fi
    
    # Copy output
    if [[ -f "out/$pn$suff" ]]; then
        cp -v "out/$pn$suff" "$mas_root/Markuse asjad/$op$suff"
    fi
    
    if [[ "$wasrunning" == "true" ]]; then
      echo "- Restarting $op"
      # completely detach the terminal from the process we start
      nohup "$mas_root/Markuse asjad/$op$suff" >/dev/null 2>&1 & disown
    fi
    
done