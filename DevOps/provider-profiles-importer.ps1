Write-Host "Waiting 30 seconds for service to restart";

Start-Sleep -s 30

$rootFolder = "$(System.DefaultWorkingDirectory)/CalculateFunding-Metadata/metadata/providers"

$version = [guid]::NewGuid().ToString()

$url = "$(ImportProvidersUrl)$version";

if([string]::IsNullOrWhiteSpace($url))
{
    Write-Host "##vso[task.logissue type=error;] Null or empty url";
}
else
{
    Write-Host "Import Providers Url: $url";
}

$headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]";
$headers.Add("sfa-userid", "8e9ad33b-9011-43be-a30c-eb6c8a6b7b49");
$headers.Add("sfa-username", "vsts");
$headers.Add("Accept", "application/json");
$headers.Add("Ocp-Apim-Subscription-Key", "$(svcapiresults)");

Get-ChildItem "$rootFolder" -Filter *.csv |

    ForEach-Object {
        
        $stopTrying = $false
        $retrycount = 1

        $responseData = New-Object PSObject;
        
        $columns = 'UKPRN','URN','LA (code)','LA (name)','OpenDate', 'CloseDate','EstablishmentTypeGroup (name)','TypeOfEstablishment (name)','PhaseOfEducation (code)','EstablishmentStatus (name)','EstablishmentName','EstablishmentNumber'

        $providers = @();

        $importedData = import-csv $_.FullName | Select $columns

        foreach ($item in $importedData) {
            $openDate = $null
            $closedDate = $null

            if([string]::IsNullOrWhiteSpace($item.'OpenDate') -eq $false)
            {
                $openDate = get-date($item.'OpenDate')
            }

            if([string]::IsNullOrWhiteSpace($item.'CloseDate') -eq $false)
            {
                $closedDate = get-date($item.'CloseDate')
            }

            if([string]::IsNullOrWhiteSpace($item.UKPRN) -eq $false) { 

                $provider = @{
                    name = $item.'EstablishmentName'
                    urn = $item.URN
                    ukPrn = $item.UKPRN
                    upin = ""
                    establishmentNumber = $item.'EstablishmentNumber'
                    dfeEstablishmentNumber = $item.'LA (code)' + "" + $item.'EstablishmentNumber'
                    authority = $item.'LA (name)'
                    providerType = $item.'TypeOfEstablishment (name)'
                    providerSubType = $item.'EstablishmentTypeGroup (name)'
                    dateOpened = $openDate
                    dateClosed = $closedDate
                    providerProfileIdType = ""
                    laCode = $item.'LA (code)'
                    navVendorNo = ""
                    crmAccountId = ""
                    legalName = ""
                    status = $item.'EstablishmentStatus (name)'
                    phaseOfEducation = $item.'PhaseOfEducation (code)'
                    reasonEstablishmentOpened = ""
                    reasonEstablishmentClosed = ""
                    successor = ""
                    trustStatus = $item.'TrustSchoolFlag (name)'
                    trustName = $item.'Trusts (name)'
                    trustCode = $item.'Trusts (code)'
                }

                $providers += $provider
            }
        }

        $versionProviders = @{
                       name = $version
                       versionType = "SystemImported"
                       description = "string"
                       providers = $providers
                    }
       
        $correlationId = [guid]::NewGuid().ToString()

        if($headers.ContainsKey("sfa-correlationId") -eq $false) {
            $headers.Add("sfa-correlationId", $correlationId);
        }
        else {
            $headers["sfa-correlationId"] = $correlationId
        }

        Write-Host "Importing$_ with correlation id $correlationId" -ForegroundColor Yellow
        
        $jsonVersionProviders = $versionProviders | ConvertTo-Json

        Write-Host $jsonVersionProviders

        do
        {
            try {
                $responseData = Invoke-WebRequest -Uri $url -Method POST -Headers $headers -Body  $jsonVersionProviders -ContentType "application/json; charset=utf-8" -DisableKeepAlive -UseBasicParsing;
                Write-Host "$_ saved successfully" -ForegroundColor Green
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
    }