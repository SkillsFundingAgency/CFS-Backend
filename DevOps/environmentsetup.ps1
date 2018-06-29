param (
    [Parameter(Mandatory=$true)][string]$ARMOutput
    )


$json = $ARMOutput | convertfrom-json

Write-Host $json


$url = $json.EnvironmentSetupFunctionUrl.value;

if([string]::IsNullOrWhiteSpace($url))
{
    Write-Error "URL is empty string or null";
}
else
{
    Write-Host "URL: $url";
}

$executionCount = 0;
$setupSuccess = $false;
while($executionCount -lt 3){

    try{
        Write-Host "Calling Environment Setup Function"
        $response = Invoke-WebRequest -Uri $url -Method Get -UseBasicParsing
        $statusCode = $response.StatusCode;
        if($statusCode -eq 200)
        {
            $setupSuccess = $true;
            Write-Host "Function returned success";
            break;
        }
        else{
            Write-Host "Returned $statusCode from function";
        }
       
    }
    catch{
        $statusCode = $_.Exception.Response.StatusCode.Value__
        Write-Host $_.Exception;
        Write-Host "Returned error code $statusCode"
    }
    sleep -Seconds 2
    $executionCount++;
}
if($setupSuccess -eq $false)
{
    Write-Error "Unable to call Environment Setup Function";
}