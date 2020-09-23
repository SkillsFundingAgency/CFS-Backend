param (
    [Parameter(Mandatory = $true)][string]$url,
    [Parameter(Mandatory = $true)][string]$tokenServiceUrl,
    [Parameter(Mandatory = $true)][string]$scope,
    [Parameter(Mandatory = $true)][string]$clientId,
    [Parameter(Mandatory = $true)][string]$clientSecret,
    [Parameter(Mandatory = $true)][string]$httpVerb = "POST"
)

if ([string]::IsNullOrWhiteSpace($url)) {
    Write-Host "##vso[task.logissue type=error;] Null or empty url supplied for authentication check";
}
else {
    Write-Host "Api Authentication Check Url: $url";
}

Start-Sleep -s 30

# Enable TLS 1.2 for powershell
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$correlationId = [guid]::NewGuid().ToString()

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
    Write-Host "##vso[task.logissue type=error;] $_ azure ad token not retrieved after 3 attempts, please check app insights with correlation id $correlationId" -ForegroundColor Red
    exit;
}

# No auth header supplied

$headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]";
$headers.Add("sfa-userid", "8e9ad33b-9011-43be-a30c-eb6c8a6b7b49");
$headers.Add("sfa-username", "vsts");
$headers.Add("Accept", "application/json");

try {
    $responseData = Invoke-WebRequest -Uri "$url/healthcheck" -Method GET -Headers $headers -DisableKeepAlive -UseBasicParsing

     Write-Host "##vso[task.logissue type=error;] Expected request to fail as no API Key supplied but succeeded" -ForegroundColor Red
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    
    if ($statusCode -eq 401)
    {
        Write-Host "##vso[task.logdetail id=$correlationId] # No auth header supplied: $url received 401 response" -ForegroundColor Green
    }
    else
    {
        Write-Host "##vso[task.logissue type=error;] Expected statuscode 401 but was $_, please check app insights with correlation id $correlationId" -ForegroundColor Red
    }
}
  
# Incorrect bearer token supplied
$incorrectBearerToken = [guid]::NewGuid().ToString()
$headers.Add("Authorization", "Bearer $incorrectBearerToken")

try {
    $responseData = Invoke-WebRequest -Uri "$url/healthcheck" -Method GET -Headers $headers -DisableKeepAlive -UseBasicParsing

    Write-Host "##vso[task.logissue type=error;] Expected request to fail as incorrect azure ad bearer token supplied but succeeded" -ForegroundColor Red
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    
    if ($statusCode -eq 401)
    {
        Write-Host "##vso[task.logdetail id=$correlationId] # Incorrect azure ad bearer token supplied: $url received 401 response" -ForegroundColor Green
    }
    else
    {
        Write-Host "##vso[task.logissue type=error;] Expected statuscode 401 but was $_, please check app insights with correlation id $correlationId" -ForegroundColor Red
    }
}

# Correct API key supplied
$headers["Authorization"] = "Bearer $accessToken";

try {
    $responseData = Invoke-WebRequest -Uri "$url/healthcheck" -Method GET -Headers $headers -DisableKeepAlive -UseBasicParsing
    
    Write-Host "##vso[task.logdetail id=$correlationId] # Correct azure ad bearer token supplied: $url access granted with the correct bearer token" -ForegroundColor Green
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    
    if ($statusCode -eq 401)
    {
        Write-Host "##vso[task.logissue type=error;] Expected authentication to succeed as correct azure ad bearer token supplied but reported 401" -ForegroundColor Red
    }
    else
    {
        Write-Host "##vso[task.logissue type=error;] Expected no error but was $_, please check app insights with correlation id $correlationId" -ForegroundColor Red    
    }
}