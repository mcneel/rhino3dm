## rhino3dm.js API Documentation

Version: rhino3dm.js 8.4.0

Published: 2024.02.16

<p><strong>rhino3dm.js</strong> is a javascript library with an associated web assembly (rhino3dm.wasm). rhino3dm.js should run on all major browsers as well as <a href="https://nodejs.org/">node.js</a>.</p>

<pre class="prettyprint source lang-html"><code>
&lt;html>
&lt;!DOCTYPE html>

&lt;body>

  &lt;!-- Import maps polyfill -->
  &lt;!-- Remove this when import maps will be widely supported -->
  &lt;script async src="https://unpkg.com/es-module-shims@1.8.2/dist/es-module-shims.js">&lt;/script>

  &lt;script type="importmap">
      {
          "imports": {
            "rhino3dm":"https://unpkg.com/rhino3dm@8.4.0/rhino3dm.module.min.js"
          }
      }
  &lt;/script>

  &lt;script type="module">

    import rhino3dm from 'rhino3dm'
    const rhino = await rhino3dm()
    const sphere = new rhino.Sphere( [1,2,3,], 12 )
    console.log(sphere.diameter)

  &lt;/script>
&lt;/body>
&lt;/html>
</code></pre>


<p>See <a href="https://github.com/mcneel/rhino3dm/blob/main/docs/javascript/RHINO3DM-BUILD.JS.md">our javascript documentation</a> for details</p>
<p>Looking for samples? Check out the <a href="https://github.com/mcneel/rhino-developer-samples/tree/8/rhino3dm">Rhino Developer Samples for rhino3dm.js</a>

<hr>
<p>The links on the side provide API documentation for what is currently available</p></article>