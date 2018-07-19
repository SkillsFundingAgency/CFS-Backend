param (
    [Parameter(Mandatory=$true)][string]$storageAccountName,
    [Parameter(Mandatory=$true)][string]$resourceGroup
)

Write-Host "Checking CORS rule for storage account '$storageAccountName' in resource group '$resourceGroup'"

$storageAccount = Get-AzureRmStorageAccount -Name $storageAccountName -ResourceGroupName $resourceGroup

if($storageAccountName -ne $null)
{
    $existingCorsRule = Get-AzureStorageCorsRule -ServiceType Blob -Context $storageAccount.Context

    $CorsRules = (@{
        AllowedHeaders=@("*");
        AllowedOrigins=@("*");
        MaxAgeInSeconds=3600;
        AllowedMethods=@("Get","POST", "PUT")
        ExposedHeaders=@("*"); 
    });

    if($existingCorsRule.Length -eq 0)
    {
        Set-AzureStorageCORSRule -ServiceType Blob -CorsRules $CorsRules -Context $storageAccount.Context;
        Write-Host "Created CORS rule";
    }
    else{
        Write-Host "CORS rule already exists, skipping adding new rule";
    }
}
else
{
    Write-Error "Unable to find storage account";
}