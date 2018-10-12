param(
    [Parameter(Position = 0, mandatory = $true)]
    [string]$apiKey,

    [Parameter(Position = 1, mandatory = $true)]
    [string]$environmentKey,
    
    [Parameter(Position = 2, mandatory = $true)]
    [String[]] $indexes = @()
)

$ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path

$rootFolder = "$ScriptDir\search-indexes"

Write-Host "Updating indexes on: $searchServiceUrl";

$headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]";
$headers.Add("api-key", $apiKey);

$indexes | ForEach-Object {
    
    $schema = Get-Content -Raw -Path "$rootFolder\$_\$_.json" | ConvertFrom-Json

    $indexName = $schema.name

    $schema = $schema | ConvertTo-Json

    $searchServiceUrl = "https://ss-$environmentKey-cfs.search.windows.net/indexes/$($indexName)?api-version=2016-09-01"

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


