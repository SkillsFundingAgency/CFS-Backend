param (
    [Parameter(Mandatory = $true)][string]$url,
    [Parameter(Mandatory = $true)][string]$apiKey
)

if ([string]::IsNullOrWhiteSpace($url)) {
    Write-Host "##vso[task.logissue type=error;] Null or empty url supplied for authentication check";
}
else {
    Write-Host "Api Authentication Check Url: $url";
}

# Enable TLS 1.2 for powershell
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$correlationId = [guid]::NewGuid().ToString()

$headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]";
$headers.Add("correlationId", $correlationId)
$headers.Add("sfa-userid", "8e9ad33b-9011-43be-a30c-eb6c8a6b7b49");
$headers.Add("sfa-username", "vsts");
$headers.Add("Accept", "application/json");

# No auth header supplied
try {
    $responseData = Invoke-WebRequest -Uri "$url/healthcheck" -Method GET -Headers $headers -DisableKeepAlive -UseBasicParsing

     Write-Host "##vso[task.logissue type=error;] Expected request to fail as no API Key supplied but succeeded" -ForegroundColor Red
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    
    if ($statusCode -eq 401)
    {
        Write-Host "##vso[task.logdetail] # No auth header supplied: $uri received 401 response" -ForegroundColor Green
    }
    else
    {
        Write-Host "##vso[task.logissue type=error;] Expected statuscode 401 but was $_, please check app insights with correlation id $correlationId" -ForegroundColor Red
    }
}
  
# Incorrect API key supplied
$incorrectApiKey = [guid]::NewGuid().ToString()
$headers.Add("Ocp-Apim-Subscription-Key", $incorrectApiKey);

try {
    $responseData = Invoke-WebRequest -Uri "$url/healthcheck" -Method GET -Headers $headers -DisableKeepAlive -UseBasicParsing

    Write-Host "##vso[task.logissue type=error;] Expected request to fail as incorrect ApiKey supplied but succeeded" -ForegroundColor Red
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    
    if ($statusCode -eq 401)
    {
        Write-Host "##vso[task.logdetail] # Incorrect API key supplied: $uri received 401 response" -ForegroundColor Green
    }
    else
    {
        Write-Host "##vso[task.logissue type=error;] Expected statuscode 401 but was $_, please check app insights with correlation id $correlationId" -ForegroundColor Red
    }
}

# Correct API key supplied
$headers["Ocp-Apim-Subscription-Key"] = $apiKey;

try {
    $responseData = Invoke-WebRequest -Uri "$url/healthcheck" -Method GET -Headers $headers -DisableKeepAlive -UseBasicParsing
    
    Write-Host "##vso[task.logdetail] # Correct API key supplied: $uri access granted with the correct ApiKey" -ForegroundColor Green
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    
    if ($statusCode -eq 401)
    {
        Write-Host "##vso[task.logissue type=error;] Expected authentication to succeed as correct ApiKey supplied but reported 401" -ForegroundColor Red
    }
    else
    {
        Write-Host "##vso[task.logissue type=error;] Expected no error but was $_, please check app insights with correlation id $correlationId" -ForegroundColor Red    
    }
}