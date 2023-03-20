
FILE_TO_NOTARIZE="$1"

# notarize - NOTE: --verbose can be added as a switch to get more logging
xcrun notarytool submit --wait --apple-id $APPLE_ID --password $MACDEV_PW --team-id $APPLE_TEAM_ID $FILE_TO_NOTARIZE --verbose
notarizeStatus=$?

if test $notarizeStatus -ne 0; then
    echo "FAILED: notarization failed (see log for details)."
    exit 1
fi

# staple for offline validation
xcrun stapler staple $FILE_TO_NOTARIZE
stapleStatus=$?

if test $stapleStatus -ne 0; then
    echo "FAILED: failed to staple (see log for details)."
    exit 1
fi