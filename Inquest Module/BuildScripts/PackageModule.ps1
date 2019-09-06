param ([string]$project, [string]$output, [string]$name)

$noModuleName = $name -replace " Module", "" -replace " module", ""

$docs = [Environment]::GetFolderPath("MyDocuments")

$modulesDest = "$($docs)\Guild Wars 2\addons\blishhud\modules\"

Write-Output "PackageModule will be using the following paths:"
Write-Output "$($project)obj\$($noModuleName).zip"
Write-Output "$($modulesDest)"

Write-Output "Building $($noModuleName).bhm..."

Remove-Item -Path "$($project)obj\$($noModuleName).zip" -Force
Compress-Archive -Path "$($project)$($output)*" -DestinationPath "$($project)obj\$($noModuleName).zip" -Update
Copy-Item "$($project)obj\$($noModuleName).zip" "$($modulesDest)$($noModuleName).bhm" -Force

Write-Output "$($modulesDest)$($noModuleName).bhm was built!"