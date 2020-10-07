param (
  [Parameter(Mandatory = $true)][string]$ARMOutput
)

$json = $ARMOutput | ConvertFrom-Json

[string]$connectionString = $json.cosmosDbConnectionString.value;

[string]$key;

$firstIndex = $connectionString.IndexOf("AccountKey=");
if ($firstIndex -gt -1) {
  $key = $connectionString.Substring($firstIndex + 11, $connectionString.Length - $firstIndex - 11);

  $firstSemicolon = $key.IndexOf(";");
  if ($firstSemicolon -gt 0) {
      $key = $key.Substring(0, $firstSemicolon);
  }
}

Write-Host '##vso[task.setvariable variable=CosmosDbSettings__ConnectionString;issecret=false]'$connectionString