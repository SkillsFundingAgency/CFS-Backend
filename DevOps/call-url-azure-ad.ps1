param (
    [Parameter(Mandatory = $true)][string]$tokenServiceUrl,
    [Parameter(Mandatory = $true)][string]$scope,
    [Parameter(Mandatory = $true)][string]$clientId,
    [Parameter(Mandatory = $true)][string]$clientSecret,
    [Parameter(Mandatory = $true)][string]$url,
    [Parameter(Mandatory = $true)][string]$apiKey
)

Write-Host "Url to be called: $url"

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
        $responseData = Invoke-RestMethod -Uri $tokenServiceUrl -Method "post" -Headers $headers -Body $azureAdFormData -DisableKeepAlive -UseBasicParsing
        $accessToken = $responseData.access_token;
        Write-Host "Succcessfully got AzureAD access token" -ForegroundColor Green
        $stopTrying = $true
    }
    catch {
        $responseData = $_.Exception

        if ($retrycount -gt 4) {
            Write-Host "##vso[task.logissue type=error;] $_ failed after 5 attempts, please check app insights with correlation id $correlationId" -ForegroundColor Red
            Write-Error $responseData
            $stopTrying = $true
        }
        else {
            Write-Host "##vso[task.logissue type=warning;] $_ failed, retrying in 10 seconds." -ForegroundColor Yellow
            Start-Sleep -Seconds 10
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
$headers.Add("Ocp-Apim-Subscription-Key", "$apiKey");
$headers.Add("Authorization", "Bearer $accessToken")

do {
    try {
         $webcall = Invoke-webrequest -uri $url -Headers $headers -Method GET -ContentType "application/json; charset=utf-8" -DisableKeepAlive -UseBasicParsing

        if ($webcall.StatusCode -eq 200) {
            Write-Host "Call to $url was successful" -ForegroundColor Green
        }
        else {
             Write-Host "##vso[task.logissue type=error;] Call to $url failed" -ForegroundColor Red
             Write-Error $webcall
        }
        $stopTrying = $true;
}
catch {
        if ($retrycount -gt 2) {
            Write-Host "##vso[task.logissue type=error;] Call to $url failed after 3 attempts" -ForegroundColor Red
            Write-Error $_.Exception
        $stopTrying = $true
    }
        else {
            Write-Host "##vso[task.logissue type=warning;] Call to $url failed, retrying in 10 seconds." -ForegroundColor Yellow
            Start-Sleep -Seconds 10
            $retrycount = $retrycount + 1
            }
    }
}
While ($stopTrying -eq $false)
