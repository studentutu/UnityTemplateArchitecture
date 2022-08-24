#!/usr/bin/env sh

# breaks exectuion of scenario if error is encountered
# set -e

# prints commands as they are executed
# set -x 
export UNITY_EDITOR_CUSTOM_PATH=${UNITY_EDITOR_CUSTOM_PATH:-'/c/Users/user/Documents/UnityInstalls/2020.3.16f1/Editor'}

unityexedir=/Unity.exe

export UNITY_EXECUTABLE=${UNITY_EXECUTABLE:-$UNITY_EDITOR_CUSTOM_PATH$unityexedir}

#C:\Users\user\Documents\UnityInstalls\2020.3.16f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools
unityadbexedir=/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe

export UNITY_ADB=${UNITY_ADB:-$UNITY_EDITOR_CUSTOM_PATH$unityadbexedir}

echo $UNITY_EDITOR_CUSTOM_PATH
echo $UNITY_EXECUTABLE
echo $UNITY_ADB