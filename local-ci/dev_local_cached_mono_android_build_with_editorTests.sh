#!/usr/bin/env sh

set -x

cd ..
source ./local-ci/base_app_info.sh
source ./local-ci/fill_unity_directory.sh

export BUILD_APP_BUNDLE=${BUILD_APP_BUNDLE:-"false"}     
export SCRIPTING_BACKEND=${SCRIPTING_BACKEND:-"Mono2x"}  
export BUILD_ADDRESSABLES=${BUILD_ADDRESSABLES:-"YES"}   
export TEST_PLATFORM='editmode'
# export TEST_PLATFORM='playmode'
export TESTING_TYPE='NUNIT'
# Standart Unity    TESTING_TYPE: NUNIT  
# Non-Standart Unity    TESTING_TYPE: JUNIT  


# BUILD_TARGET=StandaloneLinux ./ci/for_local_build.sh
# BUILD_TARGET=StandaloneOSX ./ci/for_local_build.sh
BUILD_TARGET=Android ./ci/for_local_test.sh
