""" Cloud Testing Service Account CLI

Usage:
  service-account.py list
  service-account.py create <name>
  service-account.py generate-key <service_account_id>
  service-account.py generate-token <service_account_id> <key_id> <private_key_file>

Options:
  -h --help     Show this screen.

"""

from docopt import docopt
import configparser
import json
import jwt
import requests
import time

config = configparser.ConfigParser()
config.read('config.ini')
conf = config['DEFAULT']

username = conf["username"]
password = conf["password"]
project_id = conf["project_id"]

device_test_url = 'https://device-testing.prd.gamesimulation.unity3d.com'


def get_token():
    payload = {
        'username': username,
        'password': password,
        'grant_type': 'PASSWORD'
    }
    response = requests.post('https://api.unity.com/v1/core/api/login', json=payload)
    response_json = json.loads(response.text.encode("utf8"))
    return response_json['access_token']


def list_service_accounts(token):
    headers = {"Authorization": "Bearer %s" % token}
    response = requests.get('%s/v1/service-accounts/list?projectId=%s' % (device_test_url, project_id), headers=headers)
    response_json = json.loads(response.text.encode("utf8"))
    print(json.dumps(response_json, indent=4))


def create_service_account(token, name):
    headers = {"Authorization": "Bearer %s" % token}
    payload = {
        "name": name,
        "description": "Device testing service account",
        "projectId": project_id
    }
    response = requests.post('%s/v1/service-accounts?projectId=%s' % (device_test_url, project_id), headers=headers, json=payload)
    response_json = json.loads(response.text.encode("utf8"))
    print(json.dumps(response_json, indent=4))


def generate_service_account_key(token, service_account_id):
    headers = {"Authorization": "Bearer %s" % token}
    payload = {"description": "Game Simulation private key"}
    response = requests.post('%s/v1/service-accounts/%s/keys?projectId=%s' % (device_test_url, service_account_id, project_id), headers=headers, json=payload)
    response_json = json.loads(response.text.encode("utf8"))
    key = [x for x in response_json["keys"] if x["privateKey"] is not None][0]
    print("Service Account ID: " + service_account_id)
    print("Key ID: " + key["keyId"] + "\n")
    print("Public key: \n" + key["publicKey"] + "\n")
    print("Private key (please store in a local file): \n" + key["privateKey"] + "\n")
    print("Access token:")
    generate_service_account_token(service_account_id, key["keyId"], key["privateKey"])


def generate_service_account_token(service_account_id, key_id, private_key):
    if not private_key.startswith("-----BEGIN PRIVATE KEY-----"):
        private_key = "-----BEGIN PRIVATE KEY-----\n" + private_key + "\n-----END PRIVATE KEY-----"
    now = int(time.time())
    claim = {
        'sub': service_account_id,
        'iss': service_account_id,
        'iat': now,
        'exp': now + int(conf["token_expiration_seconds"]),
        'aud': 'genesis',
        'scope': 'genesis.generateAccessToken',
    }
    token = jwt.encode(claim, private_key, algorithm='RS256', headers={'kid' : key_id})
    print(token)


def route_command(args):
    if args['list'] is True:
        token = get_token()
        list_service_accounts(token)
    elif args['create'] is True:
        token = get_token()
        create_service_account(token, args["<name>"])
    elif args['generate-key'] is True:
        token = get_token()
        generate_service_account_key(token, args["<service_account_id>"])
    elif args['generate-token'] is True:
        with open(args["<private_key_file>"], 'r') as file:
            private_key = file.read().strip()
        generate_service_account_token(args["<service_account_id>"], args["<key_id>"], private_key)


if __name__ == '__main__':
    arguments = docopt(__doc__)
    route_command(arguments)
