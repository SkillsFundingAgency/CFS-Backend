param(
    [Parameter(Position = 0, mandatory = $true)]
    [string]$apiKey,

    [Parameter(Position = 1, mandatory = $true)]
    [string]$environmentKey,
    
    [Parameter(Position = 2, mandatory = $true)]
    [String[]] $indexers = @()
)
$apiKeyObj = $apiKey | convertfrom-json

$ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path

$rootFolder = "$ScriptDir\search-indexers"

Write-Host "Updating indexers on: $searchServiceUrl";

$headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]";
$headers.Add("api-key", $apiKeyObj.azureSearchKey.value);

$indexers | ForEach-Object {
    
    $schema = Get-Content -Raw -Path "$rootFolder\$_\$_.json" | ConvertFrom-Json

    $indexerName = $schema.name

    $schema = $schema | ConvertTo-Json

    $searchServiceUrl = "https://ss-$environmentKey-cfs.search.windows.net/indexers/$($indexerName)?api-version=2019-05-06"

    $stopTrying = $false

     do {
            try {
                $responseData = Invoke-WebRequest -Uri $searchServiceUrl -Method PUT -Headers $headers -Body $schema -ContentType "application/json; charset=utf-8" -DisableKeepAlive;
                Write-Host "$_ saved successfully" -ForegroundColor Green
                $stopTrying = $true
            }
            catch {
                $responseData = $_.Exception

                if ($retrycount -gt 3) {
                    Write-Host "##vso[task.logissue type=error;] $_ failed after 3 attempts" -ForegroundColor Red
                    Write-Error $responseData
                    $stopTrying = $true
                }
                else {
                    Write-Host "##vso[task.logissue type=warning;] $_ failed, retrying in 3 seconds." -ForegroundColor Yellow
                    Start-Sleep -Seconds 3
                    $retrycount = $retrycount + 1
                }

            }
        } 
        While ($stopTrying -eq $false)

}


