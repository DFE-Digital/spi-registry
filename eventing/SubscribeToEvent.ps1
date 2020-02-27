param (
    [string] $MiddlewareBaseUrl,
    [string] $MiddlewareSubscriptionKey,
    [string] $SubscriptionId,
    [string] $Publisher,
    [string] $EventType,
    [string] $EndpointUrl,
    [string] $AuthTokenEndpoint,
    [string] $AuthClientId,
    [string] $AuthClientSecret,
    [string] $AuthResource
)

Add-Type -AssemblyName System.Web;

# Get an OAuth token.
$AuthClientId = [System.Web.HttpUtility]::UrlEncode($AuthClientId);
$AuthClientSecret = [System.Web.HttpUtility]::UrlEncode($AuthClientSecret);
$AuthResource = [System.Web.HttpUtility]::UrlEncode($AuthResource);

$oauthPayload = "grant_type=client_credentials&client_id=$AuthClientId&client_secret=$AuthClientSecret&resource=$AuthResource";

$headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]";
$headers.Add("Content-Type", "application/x-www-form-urlencoded");

$response = Invoke-RestMethod $AuthTokenEndpoint -Method Post -Headers $Headers -Body $oauthPayload;
$token = $response.access_token;

# Then do the do.
$headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]";
$headers.Add("Authorization", "Bearer $token");
$headers.Add("Content-Type", 'application/json');
$headers.Add("Ocp-Apim-Subscription-Key", $MiddlewareSubscriptionKey);

$Body = "{""subscriptionId"":""$($SubscriptionId)"",""publisher"":""$($Publisher)"",""eventType"":""$($EventType)"",""endpointUrl"":""$($EndpointUrl)""}";

$response = Invoke-RestMethod -TimeoutSec 10000 "$($MiddlewareBaseUrl)subscriptions" -Method Post -Headers $headers -Body $Body;
Write-Host $response;