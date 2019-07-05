param ([string]$project, [string]$output)

Write-Output "CopyRef will be using the following directories:"
Write-Output "$($project)ref\*"
Write-Output "$($project)$($output)ref"

robocopy "$($project)ref " "$($project)$($output)ref " /MIR

Write-Output "Directory 'ref' has been copied over!"