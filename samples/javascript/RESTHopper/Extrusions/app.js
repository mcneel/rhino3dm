let args = {
    algo : null, 
    pointer : null,
    values : []
};

// get slider values
let count = document.getElementById("count").value;
let radius = document.getElementById("radius").value;
let length = document.getElementById("length").value;

let param1 = {
    ParamName : "RH_IN:201:Length",
    InnerTree : {
        "{ 0; }": [
            {
                "type":"",
                "data": length
            }
        ]
    },
    Keys : [
        "{ 0; }"
    ],
    Values : [
        {
        "type":"",
        "data": length
    }]
};

let param2 = {
    ParamName : "RH_IN:201:Radius",
    InnerTree : {
        "{ 0; }": [
            {
                "type":"",
                "data": radius
            }
        ]
    },
    Keys : [
        "{ 0; }"
    ],
    Values : [
        {
        "type":"",
        "data": radius
    }]
};

let param3 = {
    ParamName : "RH_IN:201:Count",
    InnerTree : {
        "{ 0; }": [
            {
                "type":"",
                "data": count
            }
        ]
    },
    Keys : [
        "{ 0; }"
    ],
    Values : [
        {
        "type":"",
        "data": count
    }]
};



rhino3dm().then(function(m) {
    console.log('Loaded rhino3dm.');
    rhino = m; // global

    // authenticate
    RhinoCompute.authToken = RhinoCompute.getAuthToken();

    // if you have a different Rhino.Compute server, add the URL here:
    //RhinoCompute.url = "";

    fetch('BranchNodeRnd.ghx')
    .then(response => response.text())
    .then(text => {
        args.algo = btoa(text);
        init();
        compute();
    });
});

function compute(){

    // clear meshes from scene

    scene.traverse(child => {
        if(child.type === 'Mesh'){
            scene.remove(child);
        }
    });

    // clear values
    args.values = [];
    
    args.values.push(param1);
    args.values.push(param2);
    args.values.push(param3);

    // console.log(args);

    RhinoCompute.computeFetch("grasshopper", args).then(result => {
        console.log(result);

        let data = JSON.parse(result.values[0].Values[0][0].data);
        let mesh = rhino.CommonObject.decode(data);

        let material = new THREE.MeshStandardMaterial( {wireframe: false, color: 0xffffff, roughness: 1.00 } );
        let threeMesh = meshToThreejs(mesh, material);

        scene.add(threeMesh);
        

    });
}

function onSliderChange(){

    // get slider values
    count = document.getElementById("count").value;
    radius = document.getElementById("radius").value;
    length = document.getElementById("length").value;

    param1 = {
        ParamName : "RH_IN:201:Length",
        InnerTree : {
            "{ 0; }": [
                {
                    "type":"",
                    "data": length
                }
            ]
        },
        Keys : [
            "{ 0; }"
        ],
        Values : [
            {
            "type":"",
            "data": length
        }]
    };

    param2 = {
        ParamName : "RH_IN:201:Radius",
        InnerTree : {
            "{ 0; }": [
                {
                    "type":"",
                    "data": radius
                }
            ]
        },
        Keys : [
            "{ 0; }"
        ],
        Values : [
            {
            "type":"",
            "data": radius
        }]
    };


    param3 = {
        ParamName : "RH_IN:201:Count",
        InnerTree : {
            "{ 0; }": [
                {
                    "type":"",
                    "data": count
                }
            ]
        },
        Keys : [
            "{ 0; }"
        ],
        Values : [
            {
            "type":"",
            "data": count
        }]
    };

    compute();

}

// BOILERPLATE //

var scene, camera, renderer, controls, composer;

function init(){
    scene = new THREE.Scene();
    scene.background = new THREE.Color(1,1,1);
    camera = new THREE.PerspectiveCamera( 75, window.innerWidth/window.innerHeight, 1, 10000 );

    renderer = new THREE.WebGLRenderer({antialias: true});
    renderer.setPixelRatio( window.devicePixelRatio );
    renderer.setSize( window.innerWidth, window.innerHeight );
    var canvas = document.getElementById("canvas");
    canvas.appendChild( renderer.domElement );

    controls = new THREE.OrbitControls( camera, renderer.domElement  );

    var ambientLight = new THREE.AmbientLight( 0x404040 ); // soft white light
    scene.add( ambientLight );

    var light1 = new THREE.PointLight( 0xffffff, 3, 100 );
    light1.position.set( 50, 50, 50 );
    scene.add( light1 );

    var light2 = new THREE.PointLight( 0xffffff, 3, 100 );
    light2.position.set( -50, -50, -50 );
    scene.add( light2 );

    var light3 = new THREE.PointLight( 0xffffff, 3, 100 );
    light3.position.set( -50, 0, -50 );
    scene.add( light3 );

    var light4 = new THREE.PointLight( 0xffffff, 3, 100 );
    light4.position.set( 50, 0, 50 );
    scene.add( light3 );

    camera.position.z = 50;

    window.addEventListener( 'resize', onWindowResize, false );

    animate();
}

var animate = function () {
    requestAnimationFrame( animate );
    controls.update();
    renderer.render( scene, camera );
};
  
function onWindowResize() {
    camera.aspect = window.innerWidth / window.innerHeight;
    camera.updateProjectionMatrix();
    renderer.setSize( window.innerWidth, window.innerHeight );
    composer.setSize( window.innerWidth, window.innerHeight);
    animate();
}

function meshToThreejs(mesh, material) {
    var geometry = new THREE.BufferGeometry();
    var vertices = mesh.vertices();
    var vertexbuffer = new Float32Array(3 * vertices.count);
    for( var i=0; i<vertices.count; i++) {
      pt = vertices.get(i);
      vertexbuffer[i*3] = pt[0];
      vertexbuffer[i*3+1] = pt[1];
      vertexbuffer[i*3+2] = pt[2];
    }
    // itemSize = 3 because there are 3 values (components) per vertex
    geometry.addAttribute( 'position', new THREE.BufferAttribute( vertexbuffer, 3 ) );
  
    indices = [];
    var faces = mesh.faces();
    for( var i=0; i<faces.count; i++) {
      face = faces.get(i);
      indices.push(face[0], face[1], face[2]);
      if( face[2] != face[3] ) {
        indices.push(face[2], face[3], face[0]);
      }
    }
    geometry.setIndex(indices);
  
    var normals = mesh.normals();
    var normalBuffer = new Float32Array(3*normals.count);
    for( var i=0; i<normals.count; i++) {
      pt = normals.get(i);
      normalBuffer[i*3] = pt[0];
      normalBuffer[i*3+1] = pt[1];
      normalBuffer[i*3+2] = pt[1];
    }
    geometry.addAttribute( 'normal', new THREE.BufferAttribute( normalBuffer, 3 ) );
    return new THREE.Mesh( geometry, material );
}