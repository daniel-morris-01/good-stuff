iisreset

$projects = @(
    [pscustomobject]@{Name='AimsProfileService';Port=8080;ProjPath='services\AimsProfileService\ProfileService\AimsProfileService.csproj';Type='Site'}
    [pscustomobject]@{Name='AimsAccountService';Port=8085;ProjPath='services\AimsAccountService\AimsAccountService.csproj';Type='Site'}
    [pscustomobject]@{Name='AimsLocationService';Port=8082;ProjPath='services\AimsLocationService\AimsLocationService.csproj';Type='Site'}
    [pscustomobject]@{Name='AimsImageService';Port=8081;ProjPath='services\AimsImageService\AimsImageService\AimsImageService.csproj';Type='Site'}
    [pscustomobject]@{Name='AimsAuthorizationAuthenticationService';Port=8083;ProjPath='services\AimsAuthorizationAuthenticationService\AuthorizationAuthenticationService\AuthorizationAuthenticationService.csproj';Type='Site'}
    [pscustomobject]@{Name='Aims.UI';Port=8086;ProjPath='ux\Aims\aims.ui.csproj';Type='Site'}
	[pscustomobject]@{Name='ProfileActivityService';Port=8088;ProjPath='services\ProfileActivityService\ProfileActivityService.csproj';Type='Site'}
	[pscustomobject]@{Name='ProxyService';Port=8089;ProjPath='services\ProxyService\ProxyService.csproj';Type='Site'}
	[pscustomobject]@{Name='Avatars.TaskAutomation.Manager';Port=8090;ProjPath='automation\Avatars.TaskAutomation.Manager\Avatars.TaskAutomation.Manager.csproj';Type='Site'}
	[pscustomobject]@{Name='Avatars.TaskAutomation.Node';Port=8091;ProjPath='automation\Avatars.TaskAutomation.Node\Avatars.TaskAutomation.Node.csproj';Type='Site'}
	[pscustomobject]@{Name='Avatars.TaskAutomation.Admin.Client';Port=8095;ProjPath='automation\Avatars.TaskAutomation.Admin\Client\Avatars.TaskAutomation.Admin.Client.csproj';Type='Site'}
	[pscustomobject]@{Name='PuppeteerRunner';ProjPath='automation\Avatars.TaskAutomation.PuppeteerRunner\Avatars.TaskAutomation.PuppeteerRunner.csproj';Type='Console';Target='C:\AutomationProcesses\PuppeteerRunner'}
	[pscustomobject]@{Name='ImageInventoryService';Port=8093;ProjPath='services\ImageInventoryService\ImageInventoryService.csproj';Type='Site'}
	[pscustomobject]@{Name='OpenAIService';Port=8094;ProjPath='services\OpenAIService\OpenAIService.csproj';Type='Site'}
)


ForEach ($proj in $projects)
{
	$name = $proj.Name
	$projPath = $proj.ProjPath
	$type = $proj.Type
	$target = "C:\inetpub\sites\$name"
	if($type -ne "Site")
	{
		$target = $proj.Target
	}
	Write-Host "`n`n`n"
	Write-Host "*****************************************************************************************************************"
	Write-Host "*       Publishing $($projPath)"
	Write-Host "*****************************************************************************************************************"
	dotnet publish $projPath --configuration Release --output $target
}

(Get-Content C:\inetpub\sites\Avatars.TaskAutomation.Node\appsettings.json) -Replace 'https://{myhostname}:44376', 'http://{myhostname}:8091' | Set-Content C:\inetpub\sites\Avatars.TaskAutomation.Node\appsettings.json

ForEach ($proj in $projects)
{
	$type = $proj.Type
	if($type -eq "Site")
	{
		$name = $proj.Name
		$port = $proj.Port
		
		if(Get-IISSite $name)
		{
			Remove-WebSite -Name $name -Confirm:$false
		}
		if(Get-IISAppPool $name)
		{
			Remove-WebAppPool -Name $name -Confirm:$false
		}
	}
}

ForEach ($proj in $projects)
{
	$type = $proj.Type
	if($type -eq "Site")
	{
		$name = $proj.Name
		$port = $proj.Port
		
		if (Test-Path -Path (Join-Path C:\inetpub\sites\ $name)) {
		New-WebAppPool $name 
		New-WebSite -Name $name -Port $port -PhysicalPath (Join-Path C:\inetpub\sites\ $name)
		Set-ItemProperty IIS:\Sites\$name -name applicationPool -value $name
		Start-WebAppPool $name
		}
	}
}