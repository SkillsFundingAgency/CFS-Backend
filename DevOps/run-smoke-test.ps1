param (
    [Parameter(Mandatory = $true)][string]$url,
    [Parameter(Mandatory = $true)][string]$apiKey
)

if ([string]::IsNullOrWhiteSpace($url)) {
    Write-Host "##vso[task.logissue type=error;] Null or empty url";
}
else {
    Write-Host "Definitions Url: $url";
}

Start-Sleep -s 30

$correlationId = [guid]::NewGuid().ToString()

# Enable TLS 1.2 for powershell
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]";
$headers.Add("sfa-userid", "8e9ad33b-9011-43be-a30c-eb6c8a6b7b49");
$headers.Add("sfa-username", "vsts");
$headers.Add("Accept", "application/json");
$headers.Add("Ocp-Apim-Subscription-Key", $apiKey);
$headers.Add("sfa-correlationId", $correlationId);

$stopTrying = $false
$retrycount = 1

$responseData = New-Object PSObject;

$fullUrl = "$url/healthcheck";
Write-Host "Calling smoke test $fullUrl" -ForegroundColor Yellow

do {
    try {
        $responseData = Invoke-WebRequest -Uri $fullUrl -Method GET -Headers $headers -ContentType "application/json; charset=utf-8" -DisableKeepAlive -UseBasicParsing

        $jsonResponse = $responseData | ConvertFrom-Json;
        $overallHealth = $jsonResponse.OverallHealthOk;
        if ($overallHealth) {
            Write-Host "Smoke test was successful" -ForegroundColor Green
        }
        else {
            Write-Host "##vso[task.logissue type=error;] $fullUrl failed" -ForegroundColor Red
            Write-Error $responseData
        }

        $stopTrying = $true
    }
    catch {
        if ($retrycount -gt 5) {
            Write-Host "##vso[task.logissue type=error;] $fullUrl failed after 5 attempts, please check app insights with correlation id $correlationId" -ForegroundColor Red
            Write-Error $_.Exception
            $stopTrying = $true
        }
        else {
            Write-Host "##vso[task.logissue type=warning;] $fullUrl failed, retrying in 10 seconds." -ForegroundColor Yellow
            Start-Sleep -Seconds 10
            $retrycount = $retrycount + 1
        }

    }
}
While ($stopTrying -eq $false)
