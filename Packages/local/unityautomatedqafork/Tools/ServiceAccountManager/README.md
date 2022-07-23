This script can be used by an organization owner to setup a service account and credentials for CI access.

First update the config.ini file with your Unity configurations.

To view a list of existing service accounts for your project:

`python service-account.py list`

First create a service account:

`service-account.py create <name>`

Next copy the service account id from the previous command and generate a new private key. This key cannot be fetched again after creating so store it in a file somewhere secure.

`service-account.py generate-key <service_account_id>`

Finally create a token that can be used to fetch a jwt for authenticating with the Device Testing API endpoints. The expiration time of this token is configurable in the config.ini file.

`service-account.py generate-token <service_account_id> <key_id> <private_key_file>`

To fetch a jwt for integrating with the APIs (not required if using the upload.sh script):

`curl --header "Content-Type: application/json" --request POST --data '{"jwtToken":"'$SERVICE_ACCOUNT_TOKEN'"}' https://device-testing.prd.gamesimulation.unity3d.com/v1/service-accounts/$SERVICE_ACCOUNT_ID/token?projectId=$UNITY_PROJECT_ID`
