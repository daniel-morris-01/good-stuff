$longName = 'Sync Avatar'
$shortName = 'SyncAvatar'
$artifactUrl = 'https://artifacts'

New-Item -ItemType Directory -Force -Path "C:\Program Files\AIMSTools"
Invoke-WebRequest $artifactUrl/$shortName/$shortName.zip -OutFile "C:\Program Files\AIMSTools\$shortName.zip"
Expand-Archive "C:\Program Files\AIMSTools\$shortName.zip" -DestinationPath "C:\Program Files\AIMSTools\" -Force
Remove-Item "C:\Program Files\AIMSTools\$shortName.zip"
$DesktopPath = [Environment]::GetFolderPath("Desktop")
Copy-Item -Force "C:\Program Files\AIMSTools\$shortName\$longName.lnk" -Destination "$DesktopPath\$longName.lnk"