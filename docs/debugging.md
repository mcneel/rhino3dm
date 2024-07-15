# Debugging rhino3dm

## JavaScript

updated 2023.07.25
### Requirements
- Chrome (v115 at the time of writing, though it was supported in earlier versions)
- [C/C++ DevTools Support (DWARF) Chrome extension](https://chrome.google.com/webstore/detail/cc%20%20-devtools-support-dwa/pdcpmagijalfljmkmjngeonclgbbannb)
- A rhino3dm.js debug build (TODO Docs)

### Setup
- Install the required tools.
- Enable WebAssembly Debugging in Chrome.
  - Open the dev tools in Chrome.
  - Click on the gear icon, or press F1 to get to the settings.
  - Select _Experiments_ and ensure that _WebAssembly Debugging: Enable DWARF support_ is checked.

### Debugging
- Once the setup is in place, you should have some code you'd like to debug. This is typically an html with a js script that calls rhino3dm.js.
- The debug wasm build is a much bigger binary that what we use for release and when we compile it, it includes hardcoded paths to the root. Since we might build this debug build on a number of machines or containers, we need to pay special attention to map the source hardcoded in the wasm binary to where the source is on our local machine.
  - navigate in Chrome to `chrome://extensions`
  - here you should see the _C/C++ DevTools Support (DWARF)_ extension. Click on the _Details_ button.
  - Click on the _Extension Options_ button.
  - You will be directed to the _C/C++ DevTools Support Options_ page.
  - Once there, you can add Path substitutions. For example, if we build rhino3dm on a docker vm, we might have mapped the rhino3dm src as a volume which might be in `/src/src/` while the source might be somewhere like `/Users/username/dev/rhino3dm/src/`. In this case the left text box should contain `/src/src/` and the right textbox should contain `/Users/username/dev/rhino3dm/src/`
- Run a local server from your html/js code source
- Open the dev tools, navigate to _Sources_
- Here you should see a tree of sources, including a branch for the files hosted locally (localhost or 127.0.0.1...), and a file:// branch. This is where you should see the rhino3dm source.
- Navigate to your js script source in Dev Tools and put a breakpoint on a line that uses rhino3dm. Refresh the page. 
- Dev tools should pause the debugger on that line. You can step into the code, and eventually you will step into the rhino3dm bindings, and if you keep going, into OpenNURBS.

### References
- [emcc debug levels](https://emscripten.org/docs/tools_reference/emcc.html#emcc-gn) - we use the `-g` flag in `CMakeLists.txt` which is the same as `-g3`. This preserves all debug info, function names and DWARF
- [Debugging WebAssembly with modern tools](https://developer.chrome.com/blog/wasm-debugging-2020/)

## Python
TODO

## dotnet
TODO