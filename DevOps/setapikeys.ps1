param(
    [Parameter(Position = 0, mandatory = $true)]
    [string]$apis,
    [Parameter(Position = 1, mandatory = $true)]
    [string]$vaultName
)

# Functions from http://activedirectoryfaq.com/2017/08/creating-individual-random-passwords/
function Get-RandomCharacters($length, $characters) {
    $random = 1..$length | ForEach-Object { Get-Random -Maximum $characters.length }
    $private:ofs = ""
    return [String]$characters[$random]
}
 
function Scramble-String([string]$inputString) {
    $characterArray = $inputString.ToCharArray()   
    $scrambledStringArray = $characterArray | Get-Random -Count $characterArray.Length
    $outputString = -join $scrambledStringArray
    return $outputString 
}
 
[string[]]$serviceList = $apis.Split(",");

$existingSecrets = @{};

$existingKvSecrets = Get-AzureKeyVaultSecret -VaultName $vaultName
Write-Host "Found $($existingKvSecrets.Length) existing secrets"

foreach ($secret in $existingKvSecrets) {
    if ($secret.Name.StartsWith("svcapi")) {
        $existingSecrets.Add($secret.Name.Substring(6, $secret.Name.Length - 6).ToLower(), $secret);
    }
}

Write-Host "Found $($existingSecrets.Keys.Count) existing API secrets"

foreach ($serviceName in $serviceList) {
    $serviceName = $serviceName.trim().ToLower();

    $secretVaultKey = "svcapi" + $serviceName.ToLower();

    Write-Host "Checking for $serviceName"

    if ($existingSecrets.ContainsKey($serviceName.toLower()) -eq $false) {

        $password = Get-RandomCharacters -length 8 -characters 'abcdefghiklmnoprstuvwxyz'
        $password += Get-RandomCharacters -length 8 -characters 'ABCDEFGHKLMNOPRSTUVWXYZ'
        $password += Get-RandomCharacters -length 8 -characters '1234567890'
        $password += Get-RandomCharacters -length 3 -characters '_-'
        
        $password = Scramble-String $password

        $secretValue = ConvertTo-SecureString -String $password -AsPlainText -Force
        $createdKey = Set-AzureKeyVaultSecret -VaultName $vaultName -Name $secretVaultKey -SecretValue $secretValue
        Write-Host "`tCreated secret for $serviceName"
    }
    else {
        Write-Host "`tSecret for $serviceName already exists";
    }
}