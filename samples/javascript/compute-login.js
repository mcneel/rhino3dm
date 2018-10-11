/**
 * Calls to compute.rhino3d.com require an authorization token in the request
 * header in the form of {"Authorization": "Bearer {token}"}
 * tokens can be retrieved with a rhino account from
 * https://www.rhino3d.com/compute/login
 *
 * @param {bool} useLocalStorage - typically once a token is defined, it can be
 *   reused for future calls. In this case, the token is saved locally in your
 *   browser's local storage
 */
function _getAuthToken(useLocalStorage=true) {
  var auth = null;
  if( useLocalStorage )
    auth = localStorage["compute_auth"];
  if (auth == null) {
    auth = window.prompt("Rhino Accounts auth token");
    if (auth != null && auth.length>20) {
      auth = "Bearer " + auth;
      localStorage.setItem("compute_auth", auth);
    }
  }
  return auth;
}
