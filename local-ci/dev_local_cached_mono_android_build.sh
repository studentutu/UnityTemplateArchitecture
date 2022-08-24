#!/usr/bin/env sh

set -x

cd ..
source ./local-ci/base_app_info.sh
source ./local-ci/fill_unity_directory.sh


export BUILD_APP_BUNDLE=${BUILD_APP_BUNDLE:-"false"}     
export SCRIPTING_BACKEND=${SCRIPTING_BACKEND:-"Mono2x"}  
export BUILD_ADDRESSABLES=${BUILD_ADDRESSABLES:-"YES"}   


# BUILD_TARGET=StandaloneLinux ./ci/for_local_build.sh
# BUILD_TARGET=StandaloneOSX ./ci/for_local_build.sh
BUILD_TARGET=Android ./ci/for_local_build.sh
# BUILD_TARGET=WebGL ./ci/for_local_build.sh
