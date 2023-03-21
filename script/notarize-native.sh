FILE_TO_NOTARIZE="$1"
ZIP_PATH="${FILE_TO_NOTARIZE}.zip"

# zip dylib
/usr/bin/ditto -c -k --keepParent "${FILE_TO_NOTARIZE}" "${ZIP_PATH}"

echo "Notarizing ${FILE_TO_NOTARIZE}"

# notarize - NOTE: --verbose can be added as a switch to get more logging
xcrun notarytool submit --wait --apple-id "${APPLE_ID}" --password "${MACDEV_PW}" --team-id "${APPLE_TEAM_ID}" "${FILE_TO_NOTARIZE}" --verbose
notarizeStatus=$?

if test $notarizeStatus -ne 0; then
    echo "FAILED: notarization failed (see log for details)."
    exit 1
fi

chmod -R 755 "${FILE_TO_NOTARIZE}"

# staple for offline validation
xcrun stapler staple "${FILE_TO_NOTARIZE}"
stapleStatus=$?

if test $stapleStatus -ne 0; then
    echo "FAILED: failed to staple (see log for details)."
    exit 1
fi