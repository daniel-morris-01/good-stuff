$environment = '.development'
$longName = 'SuperPharm POS Desktop Bridge Service'
$shortName = 'SuperPharmBridge'
$artifactUrl = 'http://localhost:8270'

if (Get-Service "$longName$environment" -ErrorAction SilentlyContinue)
{ 
	sc.exe stop $longName$environment
	sc.exe delete $longName$environment
}

Invoke-WebRequest http://5.199.161.200:9999/CA2000.cer  -OutFile C:\cert\CA2000.cer
New-Item -ItemType Directory -Force -Path "C:\Program Files\$shortName$environment"
Invoke-WebRequest $artifactUrl/$shortName.zip -OutFile "C:\Program Files\AimsDesktop$environment\$shortName\$shortName.zip"
Expand-Archive "C:\Program Files\AimsDesktop$environment\$shortName\$shortName.zip" -DestinationPath "C:\Program Files\AimsDesktop$environment\$shortName" -Force
Remove-Item "C:\Program Files\AimsDesktop$environment\$shortName\$shortName.zip"

sc.exe create "$longName$environment" binpath="C:\Program Files\$shortName$environment\$shortName.exe" start=auto
sc.exe start "$longName$environment"
