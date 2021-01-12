# Fusion O365 Proxy
Proxy api for O365, rocking app access to dedicated accounts.

This application has been set up in an attempt to isolate elevated application access granted, to be able to access specific o365 mailboxes using the graph api with application token.

The application is configured against the Azure AD app [Fusion O365 Proxy (60bb6683-d737-40fc-8024-0ed77b8348cb)](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/Overview/appId/60bb6683-d737-40fc-8024-0ed77b8348cb/isMSAApp/).

## Development
To get up and running:
- Create new user secret on the ad app
- Update user secret for project

> To test endpoints access tokens with the correct roles must be used, or the authorization logic could be temporarily disabled or stepped over while debugging.

## Dispensation
The app permissions has been discussed in the disp [#215098](https://disp.equinor.com/DispensationView/215098). This highlights the need for `Calendar.ReadWrite` and `Mail.Read`. These permissions are needed in order to create calendar events and the ability to read incoming reply mails.

## Technical
The api is a simple proxy api, replicating a selection of the graph api endpoints; `/{version}/users/{userUpn}/{*}` and `/{version}/subscriptions`. 

To forward the requests we use the `YARP` library from the `Microsoft.ReverseProxy` NuGet package. This library allows us to simply forward requests and responses, while having the ability to inspect and or change the URI / request payload to execute logic like auth.

> The main logic we do as the proxy is to change the host of the requests the `https://graph.microsoft.com` and change the `Authorization: Bearer [jwt-token]` to an app token for the `Fusion O365 Proxy` ad application.


## Auth(entication|orization)
To use the proxy api, the user must have the `Users.ReadWrite` and/or `Subscriptions.Write` role. 

> This is only defined as application app roles in the `Fusion O365 Proxy` manifest. This means only app tokens are granted access.

Next, the application that created the access token, must be granted access to the `UPN` it tries to access. The targeted `UPN` is identified by inspecting URI or payload (in subscriptions). 

> The applications are granted access to the UPNs in the json file `mailboxAccess.json`. 

The calling app id is extracted from the `appid` claim.

## Configuration
The api is configured to use the app `Fusion O365 Proxy (60bb6683-d737-40fc-8024-0ed77b8348cb)` and Equinor tenant `3aa4a235-b6e2-48d5-9195-7fcf05b459b0`.

The secret is fetched from `AzureAd:ClientSecret`, which should be set using `user secrets` in development and hosting secrets when deployed.

**appsettings.json**:
```json
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/3aa4a235-b6e2-48d5-9195-7fcf05b459b0",
    "TenantId": "3aa4a235-b6e2-48d5-9195-7fcf05b459b0",
    "ClientId": "60bb6683-d737-40fc-8024-0ed77b8348cb"
  }
```

**User secrets**
```json
{
  "AzureAd:ClientSecret": "******"
}
```

# Deployment
The api is built as a container and pushed to the `fusioncr` container registry. 

Api is deployed as test and prod environment. Hosting is done using the fusion kubernetes cluster, where test is targeting the test cluster and prod the production cluster.

Endpoints are `o365-proxy.test.fusion-dev.net` and `o365-proxy.prod.fusion-dev.net`. These are specified in the deployment manifest yaml [`deployment.template.yml`](k8s/deployment.template.yml).

The deployment creates an environment config which specifies the client id and tenant id. This is mounted to `/app/static/config`, which is loaded as an optional json config file in `Program.cs`.
Containers are only deployed with 1 replica but a max 2 surge, which should ensure continuous operation.
Service and ingress are also created to expose the api to the internet.

## Pipelines

There are two pipelines relevant for this solution. 

**Deployment**
The deployment pipeline builds the container using the build number as tag, pushes to the `fusioncr` and runs deployment for test and prod.

Deployment jobs target the `fusion-o365-proxy` namespace, which have both the test and prod cluster added as resources.

> The deployment job **does not** ensure the cluster secret which is mounted as the client secret.

**Key rotation**
The rotation pipeline is set up to run on a monthly schedule and consist of 3 jobs:
- One which removes expired key credentials on the ad app
- One that ensures a valid secret in the **test** namespace
- One that ensures a valid secret in the **prod** namespace

Each run the job will try and fetch the current secret in the cluster, use the label to check with app credentials on the azure ad app, to see if the secret is still valid. 

When there are less than 35 days until the secret expires, a new secret is generated, added to the azure ad app and updated in the cluster.
When the new key is created, the deployments in the namespace are restarted, which should ensure the new secret is loaded by the pods.

The old keys are left until they expire. They will then be picked up by the cleanup job.
