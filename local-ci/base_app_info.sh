#!/usr/bin/env sh

set -x


export BUILD_NAME=${BUILD_NAME:-"customApp"}
export UNITY_DIR=${UNITY_DIR:-"./0_unity_project"}

export BACK_END_BASE_URI=${BACK_END_BASE_URI:-"https://qa_stage_custom.com"}
export BACK_END_AUTH_CLIENT_ID=${BACK_END_AUTH_CLIENT_ID:-"auth0_auth_id"}
export BACK_END_AUTH_CLIENT_SECRET=${BACK_END_AUTH_CLIENT_SECRET:-"Auth0_client_secret_for_stage"}
export DB_CONNECTION_TO_BACK_END='Username-Password-Authentication'