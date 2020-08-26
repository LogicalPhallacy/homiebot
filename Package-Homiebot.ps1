[CmdletBinding()]
param (
    [string] $Path = "homiebot.zip",
    [switch] $Force
)
BEGIN {
    if($Path -notlike "*.zip"){
        Write-Warning "This cmdlet packages homiebot as a zip file, your zip file doesn't end in .zip and may not be recognized"
        if(Test-Path $Path){
            if($Force.IsPresent){
                Write-Warning "File $Path already exists. Since you specified -Force it will be overwritten"
            }else{
                Write-Error "File $Path already exists. If you want to overwrite it please re-run with the -force parameter"
                exit 0
            }
        }
    }
    $PubDir = ($env:TEMP + [System.IO.Path]::GetRandomFileName())
    Write-Verbose "Set temp path to $pubdir"
}
PROCESS {
    Write-Verbose "Publishing dot net application"
    dotnet publish $PSScriptRoot -o $PubDir | Write-Verbose
    $LASTEXITCODE
    Write-Verbose "Creating Zip file"
    Compress-Archive -Path $PubDir -DestinationPath $Path -Force
}
END {

}