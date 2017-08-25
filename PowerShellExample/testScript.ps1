Param(
	[string] $text
)

# Sleep for up to 5 seconds.
$sleepTimeInMs = Get-Random -Minimum 1 -Maximum 5000
Start-Sleep -m $sleepTimeInMs

Write-Output "$text | Slept: $sleepTimeInMs ms"