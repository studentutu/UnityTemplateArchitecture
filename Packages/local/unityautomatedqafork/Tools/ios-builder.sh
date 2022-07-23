#!/bin/sh

APP_PERSISTENT_DATAPATH="$1"
echo "Application.persistentDataPath - $APP_PERSISTENT_DATAPATH"

echo "Archiving"
xcodebuild -allowProvisioningUpdates -project "$APP_PERSISTENT_DATAPATH/TestBuild/Unity-iPhone.xcodeproj" -scheme 'Unity-iPhone' -archivePath "$APP_PERSISTENT_DATAPATH/TestBuild/utf.xcarchive" archive
xcodebuild -exportArchive -archivePath  "$APP_PERSISTENT_DATAPATH/TestBuild/utf.xcarchive" -exportPath "$APP_PERSISTENT_DATAPATH" -exportOptionsPlist "$APP_PERSISTENT_DATAPATH/TestBuild/Info.plist"
echo "Generated IPA Archive at $APP_PERSISTENT_DATAPATH"
