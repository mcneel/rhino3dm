
FILE_TO_NOTARIZE="$1"
ZIP_PATH=FILE_TO_NOTARIZE + ".zip"

# zip dylib
/usr/bin/ditto -c -k --keepParent $FILE_TO_NOTARIZE "$ZIP_PATH"

# notarize - NOTE: --verbose can be added as a switch to get more logging
/usr/bin/notarytool submit --wait --apple-id "${APPLE_ID}" --password "${MACDEV_PW}" --team-id "${APPLE_TEAM_ID}" $ZIP_PATH --verbose
notarizeStatus=$?

if test $notarizeStatus -ne 0; then
    echo "FAILED: notarization failed (see log for details)."
    exit 1
fi

# staple for offline validation
/usr/bin/stapler staple "${FILE_TO_NOTARIZE}"
stapleStatus=$?

if test $stapleStatus -ne 0; then
    echo "FAILED: failed to staple (see log for details)."
    exit 1
fi