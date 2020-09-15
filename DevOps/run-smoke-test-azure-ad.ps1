param (
    [Parameter(Mandatory = $true)][string]$url,
    [Parameter(Mandatory = $true)][string]$tokenServiceUrl,
    [Parameter(Mandatory = $true)][string]$scope,
    [Parameter(Mandatory = $true)][string]$clientId,
    [Parameter(Mandatory = $true)][string]$clientSecret,
    [Parameter(Mandatory = $true)][string]$httpVerb = "POST"
)

#TODO; alter this to smoke test the external api but use azure ad instead of api key auth

if ([string]::IsNullOrWhiteSpace($url)) {
    Write-Host "##vso[task.logissue type=error;] Null or empty url";
}
else {
    Write-Host "Smoke test Url: $url";
}

Start-Sleep -s 30

$correlationId = [guid]::NewGuid().ToString()

# Enable TLS 1.2 for powershell
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# get azure ad access token for the supplied credentials
$azureAdFormData = New-Object "System.Collections.Generic.Dictionary[[String],[String]]";
$azureAdFormData.Add("scope", $scope);
$azureAdFormData.Add("client_id", $clientId);
$azureAdFormData.Add("client_secret", $clientSecret);
$azureAdFormData.Add("grant_type", "client_credentials");

$azureAdHeaders = New-Object "System.Collections.Generic.Dictionary[[String],[String]]";
$azureAdHeaders.Add("Accept", "application/json");
$azureAdHeaders.Add("Content-Type", "application/x-www-form-urlencoded");

do {
    try {
        $responseData = Invoke-RestMethod -Uri $tokenServiceUrl -Method $httpVerb -Headers $headers -Body $azureAdFormData -DisableKeepAlive -UseBasicParsing
        $accessToken = $responseData.access_token;
        Write-Host "Succcessfully got AzureAD access token" -ForegroundColor Green
        $stopTrying = $true
    }
    catch {
        $responseData = $_.Exception

        if ($retrycount -gt 3) {
            Write-Host "##vso[task.logissue type=error;] $_ failed after 3 attempts, please check app insights with correlation id $correlationId" -ForegroundColor Red
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

if ($null -eq $accessToken) {
    exit;
}

$stopTrying = $false;

$headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]";
$headers.Add("sfa-userid", "8e9ad33b-9011-43be-a30c-eb6c8a6b7b49");
$headers.Add("sfa-username", "vsts");
$headers.Add("Accept", "application/json");
$headers.Add("Authorization", "Bearer $accessToken")

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
        if ($retrycount -gt 3) {
            Write-Host "##vso[task.logissue type=error;] $fullUrl failed after 3 attempts, please check app insights with correlation id $correlationId" -ForegroundColor Red
            Write-Error $_.Exception
            $stopTrying = $true
        }
        else {
            Write-Host "##vso[task.logissue type=warning;] $fullUrl failed, retrying in 3 seconds." -ForegroundColor Yellow
            Start-Sleep -Seconds 3
            $retrycount = $retrycount + 1
        }

    }
}
While ($stopTrying -eq $false)
