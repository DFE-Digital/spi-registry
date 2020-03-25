param (
    #[string] $ResourceGroup,
    [string] $ServiceName,
    [string] $AdminKey,
    [string] $IndexName,
    [string] $SchemaPath
)

# does not currently seem to be supported in devops
# $acsKeys = Get-AzSearchAdminKeyPair -ResourceGroup $ResourceGroup -ServiceName $ServiceName

$headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
$headers.Add("Content-Type", 'application/json')
$headers.Add("api-key", $AdminKey)

$response = Invoke-RestMethod -TimeoutSec 10000 "https://$($ServiceName).search.windows.net/indexes/$($IndexName)?api-version=2019-05-06" -Method Put -Headers $headers -Body "$(get-content $schemaPath)"
Write-Host $response