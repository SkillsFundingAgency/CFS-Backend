Write-Host "Waiting 30 seconds for service to restart";

Start-Sleep -s 30

$rootFolder = "$(System.DefaultWorkingDirectory)/CalculateFunding-Metadata/metadata/providers"

$url = "$(ImportProvidersUrl)";

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

            if([string]::IsNullOrWhiteSpace($item.UKPRN) -eq $false) { 

                $provider = @{

                    MasterCRMAccountId = ""
                    MasterNavendorNo = ""
                    MasterDateClosed = $item.'CloseDate'
                    MasterDateOpened = $item.'OpenDate'
                    MasterDfEEstabNo  = $item.'EstablishmentNumber'
                    MasterDfELAEstabNo = $item.'LA (code)' + "" + $item.'EstablishmentNumber'
                    MasterLocalAuthorityCode = $item.'LA (code)'
                    MasterLocalAuthorityName = $item.'LA (name)'
                    MasterProviderLegalName = ""
                    MasterProviderName = $item.'EstablishmentName'
                    MasterPhaseOfEducation = $item.'PhaseOfEducation (code)'
                    MasterProviderStatusName = $item.'EstablishmentStatus (name)'
                    MasterProviderTypeGroupName = $item.'EstablishmentTypeGroup (name)'
                    MasterProviderTypeName = $item.'TypeOfEstablishment (name)'
                    MasterUKPRN = $item.UKPRN
                    MasterUPIN = ""
                    MasterURN = $item.URN
                }

                $providers += $provider
            }
        }
       
        $correlationId = [guid]::NewGuid().ToString()

        $counter = [pscustomobject] @{ Value = 0 }
        $groupSize = 1000

        $groups =$providers | Group-Object -Property { [math]::Floor($counter.Value++ / $groupSize) }
        
       
        foreach($group in $groups){

        $partitionedProviders = @();

        foreach($providerGroupItem in $group.Group){
            $partitionedProviders += $providerGroupItem;
        }

        if($headers.ContainsKey("sfa-correlationId") -eq $false) {
            $headers.Add("sfa-correlationId", $correlationId);
        }
        else {
            $headers["sfa-correlationId"] = $correlationId
        }

        Write-Host "Importing$_ with correlation id $correlationId" -ForegroundColor Yellow
        
        $jsonProviders = $partitionedProviders | ConvertTo-Json

        do
        {
            try {
                $responseData = Invoke-WebRequest -Uri $url -Method POST -Headers $headers -Body  $jsonProviders -ContentType "application/json; charset=utf-8" -DisableKeepAlive -UseBasicParsing;
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
    }