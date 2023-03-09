#!/usr/bin/env bash

# Set up temporary build keychain and insert signing certificate
echo $MACOS_CERTIFICATE | base64 --decode > certificate.p12
security create-keychain -p $MACOS_KEYCHAIN_TEMP_PWD build.keychain
security default-keychain -s build.keychain
security unlock-keychain -p $MACOS_KEYCHAIN_TEMP_PWD build.keychain
security import certificate.p12 -k build.keychain -P $MACOS_CERTIFICATE_PWD -T /usr/bin/codesign
security set-key-partition-list -S apple-tool:,apple:,codesign: -s -k $MACOS_KEYCHAIN_TEMP_PWD build.keychain > /dev/null

FOLDER_TO_SEARCH="$1"
ITEMS_TO_SIGN=()
if [ -d "$FOLDER_TO_SEARCH" ] ; then
    ITEMS=$(find "${FOLDER_TO_SEARCH}" -depth -type d -name "*.framework" -or -name "*.dylib" -or -name "*.so" -or -name "*.a")
    RESULT=$?
    if [[ $RESULT != 0 ]] ; then
        exit 1
    fi

    ITEMS_TO_SIGN+=("${ITEMS}")
fi

# Loop through all items.
for ITEM_TO_SIGN in $ITEMS_TO_SIGN;
do
	echo "Signing '${ITEM_TO_SIGN}'"
	/usr/bin/codesign --force --verbose --verbose --verbose --verbose -s "${IDENTITY_ID}" --timestamp "${ITEM_TO_SIGN}"
	RESULT=$?
	if [[ $RESULT != 0 ]] ; then
    echo "Failed to sign '${ITEM_TO_SIGN}'."
    exit 1
	fi
done
