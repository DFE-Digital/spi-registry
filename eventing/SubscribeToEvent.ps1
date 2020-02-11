param (
    [string] $MiddlewareBaseUrl,
    [string] $MiddlewareFunctionsKey,
    [string] $SubscriptionId,
    [string] $Publisher,
    [string] $EventType,
    [string] $EndpointUrl
)

$headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
$headers.Add("Content-Type", 'application/json')
$headers.Add("x-functions-key", $MiddlewareFunctionsKey)

$Body = "{""subscriptionId"":""$($SubscriptionId)"",""publisher"":""$($Publisher)"",""eventType"":""$($EventType)"",""endpointUrl"":""$($EndpointUrl)""}"

$response = Invoke-RestMethod -TimeoutSec 10000 "$($MiddlewareBaseUrl)subscriptions" -Method Post -Headers $headers -Body $Body
Write-Host $response