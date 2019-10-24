var RhinoCompute = {
    version: "0.5.1",

    url: "https://compute.rhino3d.com/",

    authToken: null,

    getAuthToken: function(useLocalStorage=true) {
        var auth = null;
        if( useLocalStorage )
            auth = localStorage["compute_auth"];
        if (auth == null) {
            auth = window.prompt("Rhino Accounts auth token\nVisit https://www.rhino3d.com/compute/login");
            if (auth != null && auth.length>20) {
                auth = "Bearer " + auth;
                localStorage.setItem("compute_auth", auth);
            }
        }
        return auth;
    },

    computeFetch: function(endpoint, arglist) {
        return fetch(RhinoCompute.url+endpoint, {
                "method":"POST",
                "body": JSON.stringify(arglist),
                "headers": {
                    "Authorization": RhinoCompute.authToken,
                    "User-Agent": `compute.rhino3d.js/${RhinoCompute.version}`
                },
        }).then(r=>r.json());
    },

    zipArgs: function(multiple, ...args) {
        if(!multiple)
            return args;

        if(args.length==1)
            return args[0].map(function(_,i) { return [args[0][i]]; });
        if(args.length==2)
            return args[0].map(function(_,i) {
                return [args[0][i],args[1][i]]; }
            );
        if(args.length==3)
            return args[0].map(function(_,i) {
                return [args[0][i],args[1][i],args[2][i]]; }
            );
        if(args.length==4)
            return args[0].map(function(_,i) {
                return [args[0][i],args[1][i],args[2][i],args[3][i]]; }
            );
        if(args.length==5)
            return args[0].map(function(_,i) {
                return [args[0][i],args[1][i],args[2][i],args[3][i],args[4][i]]; }
            );
        if(args.length==6)
            return args[0].map(function(_,i) {
                return [args[0][i],args[1][i],args[2][i],args[3][i],args[4][i],args[5][i]]; }
            );
        if(args.length==7)
            return args[0].map(function(_,i) {
                return [args[0][i],args[1][i],args[2][i],args[3][i],args[4][i],args[5][i],args[6][i]]; }
            );
        return args[0].map(function(_,i) {
            return [args[0][i],args[1][i],args[2][i],args[3][i],args[4][i],args[5][i],args[6][i],args[7][i]]; }
        );
    },

    Extrusion : {
        getWireframe : function(extrusion, multiple=false) {
            let url="rhino/geometry/extrusion/getwireframe-extrusion";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, extrusion);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },
    },

    BezierCurve : {
        createCubicBeziers : function(sourceCurve, distanceTolerance, kinkTolerance, multiple=false) {
            let url="rhino/geometry/beziercurve/createcubicbeziers-curve_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, sourceCurve, distanceTolerance, kinkTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBeziers : function(sourceCurve, multiple=false) {
            let url="rhino/geometry/beziercurve/createbeziers-curve";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, sourceCurve);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },
    },

    Brep : {
        changeSeam : function(face, direction, parameter, tolerance, multiple=false) {
            let url="rhino/geometry/brep/changeseam-brepface_int_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, face, direction, parameter, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        copyTrimCurves : function(trimSource, surfaceSource, tolerance, multiple=false) {
            let url="rhino/geometry/brep/copytrimcurves-brepface_surface_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, trimSource, surfaceSource, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBaseballSphere : function(center, radius, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createbaseballsphere-point3d_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, center, radius, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createDevelopableLoft : function(crv0, crv1, reverse0, reverse1, density, multiple=false) {
            let url="rhino/geometry/brep/createdevelopableloft-curve_curve_bool_bool_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, crv0, crv1, reverse0, reverse1, density);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createDevelopableLoft1 : function(rail0, rail1, fixedRulings, multiple=false) {
            let url="rhino/geometry/brep/createdevelopableloft-nurbscurve_nurbscurve_point2darray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, rail0, rail1, fixedRulings);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createPlanarBreps : function(inputLoops, multiple=false) {
            let url="rhino/geometry/brep/createplanarbreps-curvearray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, inputLoops);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createPlanarBreps1 : function(inputLoops, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createplanarbreps-curvearray_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, inputLoops, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createPlanarBreps2 : function(inputLoop, multiple=false) {
            let url="rhino/geometry/brep/createplanarbreps-curve";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, inputLoop);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createPlanarBreps3 : function(inputLoop, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createplanarbreps-curve_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, inputLoop, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createTrimmedSurface : function(trimSource, surfaceSource, multiple=false) {
            let url="rhino/geometry/brep/createtrimmedsurface-brepface_surface";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, trimSource, surfaceSource);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createTrimmedSurface1 : function(trimSource, surfaceSource, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createtrimmedsurface-brepface_surface_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, trimSource, surfaceSource, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromCornerPoints : function(corner1, corner2, corner3, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createfromcornerpoints-point3d_point3d_point3d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, corner1, corner2, corner3, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromCornerPoints1 : function(corner1, corner2, corner3, corner4, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createfromcornerpoints-point3d_point3d_point3d_point3d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, corner1, corner2, corner3, corner4, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createEdgeSurface : function(curves, multiple=false) {
            let url="rhino/geometry/brep/createedgesurface-curvearray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curves);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createPlanarBreps4 : function(inputLoops, multiple=false) {
            let url="rhino/geometry/brep/createplanarbreps-rhino.collections.curvelist";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, inputLoops);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createPlanarBreps5 : function(inputLoops, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createplanarbreps-rhino.collections.curvelist_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, inputLoops, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromOffsetFace : function(face, offsetDistance, offsetTolerance, bothSides, createSolid, multiple=false) {
            let url="rhino/geometry/brep/createfromoffsetface-brepface_double_double_bool_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, face, offsetDistance, offsetTolerance, bothSides, createSolid);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createSolid : function(breps, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createsolid-breparray_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, breps, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        mergeSurfaces : function(surface0, surface1, tolerance, angleToleranceRadians, multiple=false) {
            let url="rhino/geometry/brep/mergesurfaces-surface_surface_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface0, surface1, tolerance, angleToleranceRadians);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        mergeSurfaces1 : function(brep0, brep1, tolerance, angleToleranceRadians, multiple=false) {
            let url="rhino/geometry/brep/mergesurfaces-brep_brep_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep0, brep1, tolerance, angleToleranceRadians);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        mergeSurfaces2 : function(brep0, brep1, tolerance, angleToleranceRadians, point0, point1, roundness, smooth, multiple=false) {
            let url="rhino/geometry/brep/mergesurfaces-brep_brep_double_double_point2d_point2d_double_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep0, brep1, tolerance, angleToleranceRadians, point0, point1, roundness, smooth);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createPatch : function(geometry, startingSurface, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createpatch-geometrybasearray_surface_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, geometry, startingSurface, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createPatch1 : function(geometry, uSpans, vSpans, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createpatch-geometrybasearray_int_int_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, geometry, uSpans, vSpans, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createPatch2 : function(geometry, startingSurface, uSpans, vSpans, trim, tangency, pointSpacing, flexibility, surfacePull, fixEdges, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createpatch-geometrybasearray_surface_int_int_bool_bool_double_double_double_boolarray_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, geometry, startingSurface, uSpans, vSpans, trim, tangency, pointSpacing, flexibility, surfacePull, fixEdges, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createPipe : function(rail, radius, localBlending, cap, fitRail, absoluteTolerance, angleToleranceRadians, multiple=false) {
            let url="rhino/geometry/brep/createpipe-curve_double_bool_pipecapmode_bool_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, rail, radius, localBlending, cap, fitRail, absoluteTolerance, angleToleranceRadians);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createPipe1 : function(rail, railRadiiParameters, radii, localBlending, cap, fitRail, absoluteTolerance, angleToleranceRadians, multiple=false) {
            let url="rhino/geometry/brep/createpipe-curve_doublearray_doublearray_bool_pipecapmode_bool_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, rail, railRadiiParameters, radii, localBlending, cap, fitRail, absoluteTolerance, angleToleranceRadians);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromSweep : function(rail, shape, closed, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createfromsweep-curve_curve_bool_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, rail, shape, closed, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromSweep1 : function(rail, shapes, closed, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createfromsweep-curve_curvearray_bool_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, rail, shapes, closed, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromSweep2 : function(rail1, rail2, shape, closed, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createfromsweep-curve_curve_curve_bool_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, rail1, rail2, shape, closed, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromSweep3 : function(rail1, rail2, shapes, closed, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createfromsweep-curve_curve_curvearray_bool_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, rail1, rail2, shapes, closed, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromSweepSegmented : function(rail, shape, closed, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createfromsweepsegmented-curve_curve_bool_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, rail, shape, closed, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromSweepSegmented1 : function(rail, shapes, closed, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createfromsweepsegmented-curve_curvearray_bool_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, rail, shapes, closed, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromSweepInParts : function(rail1, rail2, shapes, rail_params, closed, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createfromsweepinparts-curve_curve_curvearray_point2darray_bool_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, rail1, rail2, shapes, rail_params, closed, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromTaperedExtrude : function(curveToExtrude, distance, direction, basePoint, draftAngleRadians, cornerType, tolerance, angleToleranceRadians, multiple=false) {
            let url="rhino/geometry/brep/createfromtaperedextrude-curve_double_vector3d_point3d_double_extrudecornertype_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curveToExtrude, distance, direction, basePoint, draftAngleRadians, cornerType, tolerance, angleToleranceRadians);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromTaperedExtrude1 : function(curveToExtrude, distance, direction, basePoint, draftAngleRadians, cornerType, multiple=false) {
            let url="rhino/geometry/brep/createfromtaperedextrude-curve_double_vector3d_point3d_double_extrudecornertype";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curveToExtrude, distance, direction, basePoint, draftAngleRadians, cornerType);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBlendSurface : function(face0, edge0, domain0, rev0, continuity0, face1, edge1, domain1, rev1, continuity1, multiple=false) {
            let url="rhino/geometry/brep/createblendsurface-brepface_brepedge_interval_bool_blendcontinuity_brepface_brepedge_interval_bool_blendcontinuity";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, face0, edge0, domain0, rev0, continuity0, face1, edge1, domain1, rev1, continuity1);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBlendShape : function(face0, edge0, t0, rev0, continuity0, face1, edge1, t1, rev1, continuity1, multiple=false) {
            let url="rhino/geometry/brep/createblendshape-brepface_brepedge_double_bool_blendcontinuity_brepface_brepedge_double_bool_blendcontinuity";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, face0, edge0, t0, rev0, continuity0, face1, edge1, t1, rev1, continuity1);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFilletSurface : function(face0, uv0, face1, uv1, radius, extend, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createfilletsurface-brepface_point2d_brepface_point2d_double_bool_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, face0, uv0, face1, uv1, radius, extend, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFilletSurface1 : function(face0, uv0, face1, uv1, radius, trim, extend, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createfilletsurface-brepface_point2d_brepface_point2d_double_bool_bool_double_breparray_breparray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, face0, uv0, face1, uv1, radius, trim, extend, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createChamferSurface : function(face0, uv0, radius0, face1, uv1, radius1, extend, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createchamfersurface-brepface_point2d_double_brepface_point2d_double_bool_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, face0, uv0, radius0, face1, uv1, radius1, extend, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createChamferSurface1 : function(face0, uv0, radius0, face1, uv1, radius1, trim, extend, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createchamfersurface-brepface_point2d_double_brepface_point2d_double_bool_bool_double_breparray_breparray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, face0, uv0, radius0, face1, uv1, radius1, trim, extend, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFilletEdges : function(brep, edgeIndices, startRadii, endRadii, blendType, railType, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createfilletedges-brep_intarray_doublearray_doublearray_blendtype_railtype_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, edgeIndices, startRadii, endRadii, blendType, railType, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createOffsetBrep : function(brep, distance, solid, extend, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createoffsetbrep-brep_double_bool_bool_double_breparray_breparray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, distance, solid, extend, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromJoinedEdges : function(brep0, edgeIndex0, brep1, edgeIndex1, joinTolerance, multiple=false) {
            let url="rhino/geometry/brep/createfromjoinededges-brep_int_brep_int_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep0, edgeIndex0, brep1, edgeIndex1, joinTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromLoft : function(curves, start, end, loftType, closed, multiple=false) {
            let url="rhino/geometry/brep/createfromloft-curvearray_point3d_point3d_lofttype_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curves, start, end, loftType, closed);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromLoftRebuild : function(curves, start, end, loftType, closed, rebuildPointCount, multiple=false) {
            let url="rhino/geometry/brep/createfromloftrebuild-curvearray_point3d_point3d_lofttype_bool_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curves, start, end, loftType, closed, rebuildPointCount);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromLoftRefit : function(curves, start, end, loftType, closed, refitTolerance, multiple=false) {
            let url="rhino/geometry/brep/createfromloftrefit-curvearray_point3d_point3d_lofttype_bool_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curves, start, end, loftType, closed, refitTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBooleanUnion : function(breps, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createbooleanunion-breparray_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, breps, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBooleanUnion1 : function(breps, tolerance, manifoldOnly, multiple=false) {
            let url="rhino/geometry/brep/createbooleanunion-breparray_double_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, breps, tolerance, manifoldOnly);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBooleanIntersection : function(firstSet, secondSet, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createbooleanintersection-breparray_breparray_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, firstSet, secondSet, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBooleanIntersection1 : function(firstSet, secondSet, tolerance, manifoldOnly, multiple=false) {
            let url="rhino/geometry/brep/createbooleanintersection-breparray_breparray_double_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, firstSet, secondSet, tolerance, manifoldOnly);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBooleanIntersection2 : function(firstBrep, secondBrep, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createbooleanintersection-brep_brep_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, firstBrep, secondBrep, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBooleanIntersection3 : function(firstBrep, secondBrep, tolerance, manifoldOnly, multiple=false) {
            let url="rhino/geometry/brep/createbooleanintersection-brep_brep_double_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, firstBrep, secondBrep, tolerance, manifoldOnly);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBooleanDifference : function(firstSet, secondSet, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createbooleandifference-breparray_breparray_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, firstSet, secondSet, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBooleanDifference1 : function(firstSet, secondSet, tolerance, manifoldOnly, multiple=false) {
            let url="rhino/geometry/brep/createbooleandifference-breparray_breparray_double_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, firstSet, secondSet, tolerance, manifoldOnly);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBooleanDifference2 : function(firstBrep, secondBrep, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createbooleandifference-brep_brep_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, firstBrep, secondBrep, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBooleanDifference3 : function(firstBrep, secondBrep, tolerance, manifoldOnly, multiple=false) {
            let url="rhino/geometry/brep/createbooleandifference-brep_brep_double_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, firstBrep, secondBrep, tolerance, manifoldOnly);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createShell : function(brep, facesToRemove, distance, tolerance, multiple=false) {
            let url="rhino/geometry/brep/createshell-brep_intarray_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, facesToRemove, distance, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        joinBreps : function(brepsToJoin, tolerance, multiple=false) {
            let url="rhino/geometry/brep/joinbreps-breparray_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brepsToJoin, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        mergeBreps : function(brepsToMerge, tolerance, multiple=false) {
            let url="rhino/geometry/brep/mergebreps-breparray_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brepsToMerge, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createContourCurves : function(brepToContour, contourStart, contourEnd, interval, multiple=false) {
            let url="rhino/geometry/brep/createcontourcurves-brep_point3d_point3d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brepToContour, contourStart, contourEnd, interval);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createContourCurves1 : function(brepToContour, sectionPlane, multiple=false) {
            let url="rhino/geometry/brep/createcontourcurves-brep_plane";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brepToContour, sectionPlane);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createCurvatureAnalysisMesh : function(brep, state, multiple=false) {
            let url="rhino/geometry/brep/createcurvatureanalysismesh-brep_rhino.applicationsettings.curvatureanalysissettingsstate";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, state);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        getRegions : function(brep, multiple=false) {
            let url="rhino/geometry/brep/getregions-brep";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        getWireframe : function(brep, density, multiple=false) {
            let url="rhino/geometry/brep/getwireframe-brep_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, density);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        closestPoint : function(brep, testPoint, multiple=false) {
            let url="rhino/geometry/brep/closestpoint-brep_point3d";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, testPoint);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        isPointInside : function(brep, point, tolerance, strictlyIn, multiple=false) {
            let url="rhino/geometry/brep/ispointinside-brep_point3d_double_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, point, tolerance, strictlyIn);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        capPlanarHoles : function(brep, tolerance, multiple=false) {
            let url="rhino/geometry/brep/capplanarholes-brep_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        join : function(brep, otherBrep, tolerance, compact, multiple=false) {
            let url="rhino/geometry/brep/join-brep_brep_double_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, otherBrep, tolerance, compact);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        joinNakedEdges : function(brep, tolerance, multiple=false) {
            let url="rhino/geometry/brep/joinnakededges-brep_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        mergeCoplanarFaces : function(brep, tolerance, multiple=false) {
            let url="rhino/geometry/brep/mergecoplanarfaces-brep_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        mergeCoplanarFaces1 : function(brep, tolerance, angleTolerance, multiple=false) {
            let url="rhino/geometry/brep/mergecoplanarfaces-brep_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, tolerance, angleTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        split : function(brep, cutter, intersectionTolerance, multiple=false) {
            let url="rhino/geometry/brep/split-brep_brep_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, cutter, intersectionTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        split1 : function(brep, cutter, intersectionTolerance, multiple=false) {
            let url="rhino/geometry/brep/split-brep_brep_double_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, cutter, intersectionTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        split2 : function(brep, cutters, intersectionTolerance, multiple=false) {
            let url="rhino/geometry/brep/split-brep_breparray_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, cutters, intersectionTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        split3 : function(brep, cutters, intersectionTolerance, multiple=false) {
            let url="rhino/geometry/brep/split-brep_curvearray_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, cutters, intersectionTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        split4 : function(brep, cutters, normal, planView, intersectionTolerance, multiple=false) {
            let url="rhino/geometry/brep/split-brep_geometrybasearray_vector3d_bool_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, cutters, normal, planView, intersectionTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        trim : function(brep, cutter, intersectionTolerance, multiple=false) {
            let url="rhino/geometry/brep/trim-brep_brep_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, cutter, intersectionTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        trim1 : function(brep, cutter, intersectionTolerance, multiple=false) {
            let url="rhino/geometry/brep/trim-brep_plane_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, cutter, intersectionTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        unjoinEdges : function(brep, edgesToUnjoin, multiple=false) {
            let url="rhino/geometry/brep/unjoinedges-brep_intarray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, edgesToUnjoin);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        joinEdges : function(brep, edgeIndex0, edgeIndex1, joinTolerance, compact, multiple=false) {
            let url="rhino/geometry/brep/joinedges-brep_int_int_double_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, edgeIndex0, edgeIndex1, joinTolerance, compact);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        transformComponent : function(brep, components, xform, tolerance, timeLimit, useMultipleThreads, multiple=false) {
            let url="rhino/geometry/brep/transformcomponent-brep_componentindexarray_transform_double_double_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, components, xform, tolerance, timeLimit, useMultipleThreads);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        getArea : function(brep, multiple=false) {
            let url="rhino/geometry/brep/getarea-brep";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        getArea1 : function(brep, relativeTolerance, absoluteTolerance, multiple=false) {
            let url="rhino/geometry/brep/getarea-brep_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, relativeTolerance, absoluteTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        getVolume : function(brep, multiple=false) {
            let url="rhino/geometry/brep/getvolume-brep";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        getVolume1 : function(brep, relativeTolerance, absoluteTolerance, multiple=false) {
            let url="rhino/geometry/brep/getvolume-brep_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, relativeTolerance, absoluteTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        rebuildTrimsForV2 : function(brep, face, nurbsSurface, multiple=false) {
            let url="rhino/geometry/brep/rebuildtrimsforv2-brep_brepface_nurbssurface";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, face, nurbsSurface);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        makeValidForV2 : function(brep, multiple=false) {
            let url="rhino/geometry/brep/makevalidforv2-brep";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        repair : function(brep, tolerance, multiple=false) {
            let url="rhino/geometry/brep/repair-brep_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        removeHoles : function(brep, tolerance, multiple=false) {
            let url="rhino/geometry/brep/removeholes-brep_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        removeHoles1 : function(brep, loops, tolerance, multiple=false) {
            let url="rhino/geometry/brep/removeholes-brep_componentindexarray_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, loops, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },
    },

    BrepFace : {
        pullPointsToFace : function(brepface, points, tolerance, multiple=false) {
            let url="rhino/geometry/brepface/pullpointstoface-brepface_point3darray_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brepface, points, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        draftAnglePoint : function(brepface, testPoint, testAngle, pullDirection, edge, multiple=false) {
            let url="rhino/geometry/brepface/draftanglepoint-brepface_point2d_double_vector3d_bool_point3d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brepface, testPoint, testAngle, pullDirection, edge);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        removeHoles : function(brepface, tolerance, multiple=false) {
            let url="rhino/geometry/brepface/removeholes-brepface_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brepface, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        split : function(brepface, curves, tolerance, multiple=false) {
            let url="rhino/geometry/brepface/split-brepface_curvearray_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brepface, curves, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        isPointOnFace : function(brepface, u, v, multiple=false) {
            let url="rhino/geometry/brepface/ispointonface-brepface_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brepface, u, v);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        trimAwareIsoIntervals : function(brepface, direction, constantParameter, multiple=false) {
            let url="rhino/geometry/brepface/trimawareisointervals-brepface_int_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brepface, direction, constantParameter);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        trimAwareIsoCurve : function(brepface, direction, constantParameter, multiple=false) {
            let url="rhino/geometry/brepface/trimawareisocurve-brepface_int_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brepface, direction, constantParameter);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        changeSurface : function(brepface, surfaceIndex, multiple=false) {
            let url="rhino/geometry/brepface/changesurface-brepface_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brepface, surfaceIndex);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        rebuildEdges : function(brepface, tolerance, rebuildSharedEdges, rebuildVertices, multiple=false) {
            let url="rhino/geometry/brepface/rebuildedges-brepface_double_bool_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brepface, tolerance, rebuildSharedEdges, rebuildVertices);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },
    },

    Curve : {
        getConicSectionType : function(curve, multiple=false) {
            let url="rhino/geometry/curve/getconicsectiontype-curve";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createInterpolatedCurve : function(points, degree, multiple=false) {
            let url="rhino/geometry/curve/createinterpolatedcurve-point3darray_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, points, degree);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createInterpolatedCurve1 : function(points, degree, knots, multiple=false) {
            let url="rhino/geometry/curve/createinterpolatedcurve-point3darray_int_curveknotstyle";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, points, degree, knots);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createInterpolatedCurve2 : function(points, degree, knots, startTangent, endTangent, multiple=false) {
            let url="rhino/geometry/curve/createinterpolatedcurve-point3darray_int_curveknotstyle_vector3d_vector3d";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, points, degree, knots, startTangent, endTangent);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createSoftEditCurve : function(curve, t, delta, length, fixEnds, multiple=false) {
            let url="rhino/geometry/curve/createsofteditcurve-curve_double_vector3d_double_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, t, delta, length, fixEnds);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFilletCornersCurve : function(curve, radius, tolerance, angleTolerance, multiple=false) {
            let url="rhino/geometry/curve/createfilletcornerscurve-curve_double_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, radius, tolerance, angleTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createArcBlend : function(startPt, startDir, endPt, endDir, controlPointLengthRatio, multiple=false) {
            let url="rhino/geometry/curve/createarcblend-point3d_vector3d_point3d_vector3d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, startPt, startDir, endPt, endDir, controlPointLengthRatio);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createMeanCurve : function(curveA, curveB, angleToleranceRadians, multiple=false) {
            let url="rhino/geometry/curve/createmeancurve-curve_curve_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curveA, curveB, angleToleranceRadians);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createMeanCurve1 : function(curveA, curveB, multiple=false) {
            let url="rhino/geometry/curve/createmeancurve-curve_curve";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curveA, curveB);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBlendCurve : function(curveA, curveB, continuity, multiple=false) {
            let url="rhino/geometry/curve/createblendcurve-curve_curve_blendcontinuity";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curveA, curveB, continuity);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBlendCurve1 : function(curveA, curveB, continuity, bulgeA, bulgeB, multiple=false) {
            let url="rhino/geometry/curve/createblendcurve-curve_curve_blendcontinuity_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curveA, curveB, continuity, bulgeA, bulgeB);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBlendCurve2 : function(curve0, t0, reverse0, continuity0, curve1, t1, reverse1, continuity1, multiple=false) {
            let url="rhino/geometry/curve/createblendcurve-curve_double_bool_blendcontinuity_curve_double_bool_blendcontinuity";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve0, t0, reverse0, continuity0, curve1, t1, reverse1, continuity1);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createTweenCurves : function(curve0, curve1, numCurves, multiple=false) {
            let url="rhino/geometry/curve/createtweencurves-curve_curve_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve0, curve1, numCurves);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createTweenCurves1 : function(curve0, curve1, numCurves, tolerance, multiple=false) {
            let url="rhino/geometry/curve/createtweencurves-curve_curve_int_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve0, curve1, numCurves, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createTweenCurvesWithMatching : function(curve0, curve1, numCurves, multiple=false) {
            let url="rhino/geometry/curve/createtweencurveswithmatching-curve_curve_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve0, curve1, numCurves);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createTweenCurvesWithMatching1 : function(curve0, curve1, numCurves, tolerance, multiple=false) {
            let url="rhino/geometry/curve/createtweencurveswithmatching-curve_curve_int_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve0, curve1, numCurves, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createTweenCurvesWithSampling : function(curve0, curve1, numCurves, numSamples, multiple=false) {
            let url="rhino/geometry/curve/createtweencurveswithsampling-curve_curve_int_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve0, curve1, numCurves, numSamples);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createTweenCurvesWithSampling1 : function(curve0, curve1, numCurves, numSamples, tolerance, multiple=false) {
            let url="rhino/geometry/curve/createtweencurveswithsampling-curve_curve_int_int_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve0, curve1, numCurves, numSamples, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        joinCurves : function(inputCurves, multiple=false) {
            let url="rhino/geometry/curve/joincurves-curvearray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, inputCurves);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        joinCurves1 : function(inputCurves, joinTolerance, multiple=false) {
            let url="rhino/geometry/curve/joincurves-curvearray_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, inputCurves, joinTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        joinCurves2 : function(inputCurves, joinTolerance, preserveDirection, multiple=false) {
            let url="rhino/geometry/curve/joincurves-curvearray_double_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, inputCurves, joinTolerance, preserveDirection);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        makeEndsMeet : function(curveA, adjustStartCurveA, curveB, adjustStartCurveB, multiple=false) {
            let url="rhino/geometry/curve/makeendsmeet-curve_bool_curve_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curveA, adjustStartCurveA, curveB, adjustStartCurveB);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFillet : function(curve0, curve1, radius, t0Base, t1Base, multiple=false) {
            let url="rhino/geometry/curve/createfillet-curve_curve_double_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve0, curve1, radius, t0Base, t1Base);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFilletCurves : function(curve0, point0, curve1, point1, radius, join, trim, arcExtension, tolerance, angleTolerance, multiple=false) {
            let url="rhino/geometry/curve/createfilletcurves-curve_point3d_curve_point3d_double_bool_bool_bool_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve0, point0, curve1, point1, radius, join, trim, arcExtension, tolerance, angleTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBooleanUnion : function(curves, multiple=false) {
            let url="rhino/geometry/curve/createbooleanunion-curvearray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curves);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBooleanUnion1 : function(curves, tolerance, multiple=false) {
            let url="rhino/geometry/curve/createbooleanunion-curvearray_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curves, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBooleanIntersection : function(curveA, curveB, multiple=false) {
            let url="rhino/geometry/curve/createbooleanintersection-curve_curve";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curveA, curveB);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBooleanIntersection1 : function(curveA, curveB, tolerance, multiple=false) {
            let url="rhino/geometry/curve/createbooleanintersection-curve_curve_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curveA, curveB, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBooleanDifference : function(curveA, curveB, multiple=false) {
            let url="rhino/geometry/curve/createbooleandifference-curve_curve";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curveA, curveB);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBooleanDifference1 : function(curveA, curveB, tolerance, multiple=false) {
            let url="rhino/geometry/curve/createbooleandifference-curve_curve_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curveA, curveB, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBooleanDifference2 : function(curveA, subtractors, multiple=false) {
            let url="rhino/geometry/curve/createbooleandifference-curve_curvearray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curveA, subtractors);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBooleanDifference3 : function(curveA, subtractors, tolerance, multiple=false) {
            let url="rhino/geometry/curve/createbooleandifference-curve_curvearray_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curveA, subtractors, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createTextOutlines : function(text, font, textHeight, textStyle, closeLoops, plane, smallCapsScale, tolerance, multiple=false) {
            let url="rhino/geometry/curve/createtextoutlines-string_string_double_int_bool_plane_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, text, font, textHeight, textStyle, closeLoops, plane, smallCapsScale, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createCurve2View : function(curveA, curveB, vectorA, vectorB, tolerance, angleTolerance, multiple=false) {
            let url="rhino/geometry/curve/createcurve2view-curve_curve_vector3d_vector3d_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curveA, curveB, vectorA, vectorB, tolerance, angleTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        doDirectionsMatch : function(curveA, curveB, multiple=false) {
            let url="rhino/geometry/curve/dodirectionsmatch-curve_curve";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curveA, curveB);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        projectToMesh : function(curve, mesh, direction, tolerance, multiple=false) {
            let url="rhino/geometry/curve/projecttomesh-curve_mesh_vector3d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, mesh, direction, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        projectToMesh1 : function(curve, meshes, direction, tolerance, multiple=false) {
            let url="rhino/geometry/curve/projecttomesh-curve_mesharray_vector3d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, meshes, direction, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        projectToMesh2 : function(curves, meshes, direction, tolerance, multiple=false) {
            let url="rhino/geometry/curve/projecttomesh-curvearray_mesharray_vector3d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curves, meshes, direction, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        projectToBrep : function(curve, brep, direction, tolerance, multiple=false) {
            let url="rhino/geometry/curve/projecttobrep-curve_brep_vector3d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, brep, direction, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        projectToBrep1 : function(curve, breps, direction, tolerance, multiple=false) {
            let url="rhino/geometry/curve/projecttobrep-curve_breparray_vector3d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, breps, direction, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        projectToBrep2 : function(curve, breps, direction, tolerance, multiple=false) {
            let url="rhino/geometry/curve/projecttobrep-curve_breparray_vector3d_double_intarray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, breps, direction, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        projectToBrep3 : function(curves, breps, direction, tolerance, multiple=false) {
            let url="rhino/geometry/curve/projecttobrep-curvearray_breparray_vector3d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curves, breps, direction, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        projectToBrep4 : function(curves, breps, direction, tolerance, multiple=false) {
            let url="rhino/geometry/curve/projecttobrep-curvearray_breparray_vector3d_double_intarray_intarray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curves, breps, direction, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        projectToPlane : function(curve, plane, multiple=false) {
            let url="rhino/geometry/curve/projecttoplane-curve_plane";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, plane);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        pullToBrepFace : function(curve, face, tolerance, multiple=false) {
            let url="rhino/geometry/curve/pulltobrepface-curve_brepface_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, face, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        planarClosedCurveRelationship : function(curveA, curveB, testPlane, tolerance, multiple=false) {
            let url="rhino/geometry/curve/planarclosedcurverelationship-curve_curve_plane_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curveA, curveB, testPlane, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        planarCurveCollision : function(curveA, curveB, testPlane, tolerance, multiple=false) {
            let url="rhino/geometry/curve/planarcurvecollision-curve_curve_plane_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curveA, curveB, testPlane, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        duplicateSegments : function(curve, multiple=false) {
            let url="rhino/geometry/curve/duplicatesegments-curve";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        smooth : function(curve, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem, multiple=false) {
            let url="rhino/geometry/curve/smooth-curve_double_bool_bool_bool_bool_smoothingcoordinatesystem";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        smooth1 : function(curve, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem, plane, multiple=false) {
            let url="rhino/geometry/curve/smooth-curve_double_bool_bool_bool_bool_smoothingcoordinatesystem_plane";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem, plane);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        getLocalPerpPoint : function(curve, testPoint, seedParmameter, multiple=false) {
            let url="rhino/geometry/curve/getlocalperppoint-curve_point3d_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, testPoint, seedParmameter);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        getLocalPerpPoint1 : function(curve, testPoint, seedParmameter, subDomain, multiple=false) {
            let url="rhino/geometry/curve/getlocalperppoint-curve_point3d_double_interval_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, testPoint, seedParmameter, subDomain);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        getLocalTangentPoint : function(curve, testPoint, seedParmameter, multiple=false) {
            let url="rhino/geometry/curve/getlocaltangentpoint-curve_point3d_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, testPoint, seedParmameter);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        getLocalTangentPoint1 : function(curve, testPoint, seedParmameter, subDomain, multiple=false) {
            let url="rhino/geometry/curve/getlocaltangentpoint-curve_point3d_double_interval_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, testPoint, seedParmameter, subDomain);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        makeClosed : function(curve, tolerance, multiple=false) {
            let url="rhino/geometry/curve/makeclosed-curve_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        lcoalClosestPoint : function(curve, testPoint, seed, multiple=false) {
            let url="rhino/geometry/curve/lcoalclosestpoint-curve_point3d_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, testPoint, seed);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        closestPoint : function(curve, testPoint, multiple=false) {
            let url="rhino/geometry/curve/closestpoint-curve_point3d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, testPoint);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        closestPoint1 : function(curve, testPoint, maximumDistance, multiple=false) {
            let url="rhino/geometry/curve/closestpoint-curve_point3d_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, testPoint, maximumDistance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        closestPoints : function(curve, otherCurve, multiple=false) {
            let url="rhino/geometry/curve/closestpoints-curve_curve_point3d_point3d";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, otherCurve);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        contains : function(curve, testPoint, multiple=false) {
            let url="rhino/geometry/curve/contains-curve_point3d";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, testPoint);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        contains1 : function(curve, testPoint, plane, multiple=false) {
            let url="rhino/geometry/curve/contains-curve_point3d_plane";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, testPoint, plane);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        contains2 : function(curve, testPoint, plane, tolerance, multiple=false) {
            let url="rhino/geometry/curve/contains-curve_point3d_plane_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, testPoint, plane, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        extremeParameters : function(curve, direction, multiple=false) {
            let url="rhino/geometry/curve/extremeparameters-curve_vector3d";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, direction);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createPeriodicCurve : function(curve, multiple=false) {
            let url="rhino/geometry/curve/createperiodiccurve-curve";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createPeriodicCurve1 : function(curve, smooth, multiple=false) {
            let url="rhino/geometry/curve/createperiodiccurve-curve_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, smooth);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        pointAtLength : function(curve, length, multiple=false) {
            let url="rhino/geometry/curve/pointatlength-curve_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, length);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        pointAtNormalizedLength : function(curve, length, multiple=false) {
            let url="rhino/geometry/curve/pointatnormalizedlength-curve_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, length);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        perpendicularFrameAt : function(curve, t, multiple=false) {
            let url="rhino/geometry/curve/perpendicularframeat-curve_double_plane";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, t);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        getPerpendicularFrames : function(curve, parameters, multiple=false) {
            let url="rhino/geometry/curve/getperpendicularframes-curve_doublearray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, parameters);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        getLength : function(curve, multiple=false) {
            let url="rhino/geometry/curve/getlength-curve";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        getLength1 : function(curve, fractionalTolerance, multiple=false) {
            let url="rhino/geometry/curve/getlength-curve_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, fractionalTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        getLength2 : function(curve, subdomain, multiple=false) {
            let url="rhino/geometry/curve/getlength-curve_interval";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, subdomain);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        getLength3 : function(curve, fractionalTolerance, subdomain, multiple=false) {
            let url="rhino/geometry/curve/getlength-curve_double_interval";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, fractionalTolerance, subdomain);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        isShort : function(curve, tolerance, multiple=false) {
            let url="rhino/geometry/curve/isshort-curve_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        isShort1 : function(curve, tolerance, subdomain, multiple=false) {
            let url="rhino/geometry/curve/isshort-curve_double_interval";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, tolerance, subdomain);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        removeShortSegments : function(curve, tolerance, multiple=false) {
            let url="rhino/geometry/curve/removeshortsegments-curve_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        lengthParameter : function(curve, segmentLength, multiple=false) {
            let url="rhino/geometry/curve/lengthparameter-curve_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, segmentLength);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        lengthParameter1 : function(curve, segmentLength, fractionalTolerance, multiple=false) {
            let url="rhino/geometry/curve/lengthparameter-curve_double_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, segmentLength, fractionalTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        lengthParameter2 : function(curve, segmentLength, subdomain, multiple=false) {
            let url="rhino/geometry/curve/lengthparameter-curve_double_double_interval";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, segmentLength, subdomain);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        lengthParameter3 : function(curve, segmentLength, fractionalTolerance, subdomain, multiple=false) {
            let url="rhino/geometry/curve/lengthparameter-curve_double_double_double_interval";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, segmentLength, fractionalTolerance, subdomain);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        normalizedLengthParameter : function(curve, s, multiple=false) {
            let url="rhino/geometry/curve/normalizedlengthparameter-curve_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, s);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        normalizedLengthParameter1 : function(curve, s, fractionalTolerance, multiple=false) {
            let url="rhino/geometry/curve/normalizedlengthparameter-curve_double_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, s, fractionalTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        normalizedLengthParameter2 : function(curve, s, subdomain, multiple=false) {
            let url="rhino/geometry/curve/normalizedlengthparameter-curve_double_double_interval";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, s, subdomain);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        normalizedLengthParameter3 : function(curve, s, fractionalTolerance, subdomain, multiple=false) {
            let url="rhino/geometry/curve/normalizedlengthparameter-curve_double_double_double_interval";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, s, fractionalTolerance, subdomain);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        normalizedLengthParameters : function(curve, s, absoluteTolerance, multiple=false) {
            let url="rhino/geometry/curve/normalizedlengthparameters-curve_doublearray_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, s, absoluteTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        normalizedLengthParameters1 : function(curve, s, absoluteTolerance, fractionalTolerance, multiple=false) {
            let url="rhino/geometry/curve/normalizedlengthparameters-curve_doublearray_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, s, absoluteTolerance, fractionalTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        normalizedLengthParameters2 : function(curve, s, absoluteTolerance, subdomain, multiple=false) {
            let url="rhino/geometry/curve/normalizedlengthparameters-curve_doublearray_double_interval";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, s, absoluteTolerance, subdomain);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        normalizedLengthParameters3 : function(curve, s, absoluteTolerance, fractionalTolerance, subdomain, multiple=false) {
            let url="rhino/geometry/curve/normalizedlengthparameters-curve_doublearray_double_double_interval";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, s, absoluteTolerance, fractionalTolerance, subdomain);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        divideByCount : function(curve, segmentCount, includeEnds, multiple=false) {
            let url="rhino/geometry/curve/dividebycount-curve_int_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, segmentCount, includeEnds);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        divideByCount1 : function(curve, segmentCount, includeEnds, multiple=false) {
            let url="rhino/geometry/curve/dividebycount-curve_int_bool_point3darray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, segmentCount, includeEnds);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        divideByLength : function(curve, segmentLength, includeEnds, multiple=false) {
            let url="rhino/geometry/curve/dividebylength-curve_double_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, segmentLength, includeEnds);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        divideByLength1 : function(curve, segmentLength, includeEnds, reverse, multiple=false) {
            let url="rhino/geometry/curve/dividebylength-curve_double_bool_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, segmentLength, includeEnds, reverse);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        divideByLength2 : function(curve, segmentLength, includeEnds, multiple=false) {
            let url="rhino/geometry/curve/dividebylength-curve_double_bool_point3darray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, segmentLength, includeEnds);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        divideByLength3 : function(curve, segmentLength, includeEnds, reverse, multiple=false) {
            let url="rhino/geometry/curve/dividebylength-curve_double_bool_bool_point3darray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, segmentLength, includeEnds, reverse);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        divideEquidistant : function(curve, distance, multiple=false) {
            let url="rhino/geometry/curve/divideequidistant-curve_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, distance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        divideAsContour : function(curve, contourStart, contourEnd, interval, multiple=false) {
            let url="rhino/geometry/curve/divideascontour-curve_point3d_point3d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, contourStart, contourEnd, interval);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        trim : function(curve, side, length, multiple=false) {
            let url="rhino/geometry/curve/trim-curve_curveend_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, side, length);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        split : function(curve, cutter, tolerance, multiple=false) {
            let url="rhino/geometry/curve/split-curve_brep_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, cutter, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        split1 : function(curve, cutter, tolerance, angleToleranceRadians, multiple=false) {
            let url="rhino/geometry/curve/split-curve_brep_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, cutter, tolerance, angleToleranceRadians);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        split2 : function(curve, cutter, tolerance, multiple=false) {
            let url="rhino/geometry/curve/split-curve_surface_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, cutter, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        split3 : function(curve, cutter, tolerance, angleToleranceRadians, multiple=false) {
            let url="rhino/geometry/curve/split-curve_surface_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, cutter, tolerance, angleToleranceRadians);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        extend : function(curve, t0, t1, multiple=false) {
            let url="rhino/geometry/curve/extend-curve_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, t0, t1);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        extend1 : function(curve, domain, multiple=false) {
            let url="rhino/geometry/curve/extend-curve_interval";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, domain);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        extend2 : function(curve, side, length, style, multiple=false) {
            let url="rhino/geometry/curve/extend-curve_curveend_double_curveextensionstyle";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, side, length, style);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        extend3 : function(curve, side, style, geometry, multiple=false) {
            let url="rhino/geometry/curve/extend-curve_curveend_curveextensionstyle_geometrybasearray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, side, style, geometry);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        extend4 : function(curve, side, style, endPoint, multiple=false) {
            let url="rhino/geometry/curve/extend-curve_curveend_curveextensionstyle_point3d";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, side, style, endPoint);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        extendOnSurface : function(curve, side, surface, multiple=false) {
            let url="rhino/geometry/curve/extendonsurface-curve_curveend_surface";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, side, surface);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        extendOnSurface1 : function(curve, side, face, multiple=false) {
            let url="rhino/geometry/curve/extendonsurface-curve_curveend_brepface";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, side, face);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        extendByLine : function(curve, side, geometry, multiple=false) {
            let url="rhino/geometry/curve/extendbyline-curve_curveend_geometrybasearray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, side, geometry);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        extendByArc : function(curve, side, geometry, multiple=false) {
            let url="rhino/geometry/curve/extendbyarc-curve_curveend_geometrybasearray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, side, geometry);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        simplify : function(curve, options, distanceTolerance, angleToleranceRadians, multiple=false) {
            let url="rhino/geometry/curve/simplify-curve_curvesimplifyoptions_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, options, distanceTolerance, angleToleranceRadians);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        simplifyEnd : function(curve, end, options, distanceTolerance, angleToleranceRadians, multiple=false) {
            let url="rhino/geometry/curve/simplifyend-curve_curveend_curvesimplifyoptions_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, end, options, distanceTolerance, angleToleranceRadians);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        fair : function(curve, distanceTolerance, angleTolerance, clampStart, clampEnd, iterations, multiple=false) {
            let url="rhino/geometry/curve/fair-curve_double_double_int_int_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, distanceTolerance, angleTolerance, clampStart, clampEnd, iterations);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        fit : function(curve, degree, fitTolerance, angleTolerance, multiple=false) {
            let url="rhino/geometry/curve/fit-curve_int_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, degree, fitTolerance, angleTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        rebuild : function(curve, pointCount, degree, preserveTangents, multiple=false) {
            let url="rhino/geometry/curve/rebuild-curve_int_int_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, pointCount, degree, preserveTangents);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        toPolyline : function(curve, mainSegmentCount, subSegmentCount, maxAngleRadians, maxChordLengthRatio, maxAspectRatio, tolerance, minEdgeLength, maxEdgeLength, keepStartPoint, multiple=false) {
            let url="rhino/geometry/curve/topolyline-curve_int_int_double_double_double_double_double_double_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, mainSegmentCount, subSegmentCount, maxAngleRadians, maxChordLengthRatio, maxAspectRatio, tolerance, minEdgeLength, maxEdgeLength, keepStartPoint);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        toPolyline1 : function(curve, mainSegmentCount, subSegmentCount, maxAngleRadians, maxChordLengthRatio, maxAspectRatio, tolerance, minEdgeLength, maxEdgeLength, keepStartPoint, curveDomain, multiple=false) {
            let url="rhino/geometry/curve/topolyline-curve_int_int_double_double_double_double_double_double_bool_interval";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, mainSegmentCount, subSegmentCount, maxAngleRadians, maxChordLengthRatio, maxAspectRatio, tolerance, minEdgeLength, maxEdgeLength, keepStartPoint, curveDomain);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        toPolyline2 : function(curve, tolerance, angleTolerance, minimumLength, maximumLength, multiple=false) {
            let url="rhino/geometry/curve/topolyline-curve_double_double_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, tolerance, angleTolerance, minimumLength, maximumLength);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        toArcsAndLines : function(curve, tolerance, angleTolerance, minimumLength, maximumLength, multiple=false) {
            let url="rhino/geometry/curve/toarcsandlines-curve_double_double_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, tolerance, angleTolerance, minimumLength, maximumLength);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        pullToMesh : function(curve, mesh, tolerance, multiple=false) {
            let url="rhino/geometry/curve/pulltomesh-curve_mesh_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, mesh, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        offset : function(curve, plane, distance, tolerance, cornerStyle, multiple=false) {
            let url="rhino/geometry/curve/offset-curve_plane_double_double_curveoffsetcornerstyle";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, plane, distance, tolerance, cornerStyle);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        offset1 : function(curve, directionPoint, normal, distance, tolerance, cornerStyle, multiple=false) {
            let url="rhino/geometry/curve/offset-curve_point3d_vector3d_double_double_curveoffsetcornerstyle";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, directionPoint, normal, distance, tolerance, cornerStyle);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        ribbonOffset : function(curve, distance, blendRadius, directionPoint, normal, tolerance, multiple=false) {
            let url="rhino/geometry/curve/ribbonoffset-curve_double_double_point3d_vector3d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, distance, blendRadius, directionPoint, normal, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        offsetOnSurface : function(curve, face, distance, fittingTolerance, multiple=false) {
            let url="rhino/geometry/curve/offsetonsurface-curve_brepface_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, face, distance, fittingTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        offsetOnSurface1 : function(curve, face, throughPoint, fittingTolerance, multiple=false) {
            let url="rhino/geometry/curve/offsetonsurface-curve_brepface_point2d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, face, throughPoint, fittingTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        offsetOnSurface2 : function(curve, face, curveParameters, offsetDistances, fittingTolerance, multiple=false) {
            let url="rhino/geometry/curve/offsetonsurface-curve_brepface_doublearray_doublearray_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, face, curveParameters, offsetDistances, fittingTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        offsetOnSurface3 : function(curve, surface, distance, fittingTolerance, multiple=false) {
            let url="rhino/geometry/curve/offsetonsurface-curve_surface_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, surface, distance, fittingTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        offsetOnSurface4 : function(curve, surface, throughPoint, fittingTolerance, multiple=false) {
            let url="rhino/geometry/curve/offsetonsurface-curve_surface_point2d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, surface, throughPoint, fittingTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        offsetOnSurface5 : function(curve, surface, curveParameters, offsetDistances, fittingTolerance, multiple=false) {
            let url="rhino/geometry/curve/offsetonsurface-curve_surface_doublearray_doublearray_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, surface, curveParameters, offsetDistances, fittingTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        pullToBrepFace1 : function(curve, face, tolerance, multiple=false) {
            let url="rhino/geometry/curve/pulltobrepface-curve_brepface_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, face, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        offsetNormalToSurface : function(curve, surface, height, multiple=false) {
            let url="rhino/geometry/curve/offsetnormaltosurface-curve_surface_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, surface, height);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },
    },

    AreaMassProperties : {
        compute : function(closedPlanarCurve, multiple=false) {
            let url="rhino/geometry/areamassproperties/compute-curve";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, closedPlanarCurve);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        compute1 : function(closedPlanarCurve, planarTolerance, multiple=false) {
            let url="rhino/geometry/areamassproperties/compute-curve_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, closedPlanarCurve, planarTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        compute2 : function(hatch, multiple=false) {
            let url="rhino/geometry/areamassproperties/compute-hatch";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, hatch);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        compute3 : function(mesh, multiple=false) {
            let url="rhino/geometry/areamassproperties/compute-mesh";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        compute4 : function(mesh, area, firstMoments, secondMoments, productMoments, multiple=false) {
            let url="rhino/geometry/areamassproperties/compute-mesh_bool_bool_bool_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, area, firstMoments, secondMoments, productMoments);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        compute5 : function(brep, multiple=false) {
            let url="rhino/geometry/areamassproperties/compute-brep";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        compute6 : function(brep, area, firstMoments, secondMoments, productMoments, multiple=false) {
            let url="rhino/geometry/areamassproperties/compute-brep_bool_bool_bool_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, area, firstMoments, secondMoments, productMoments);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        compute7 : function(surface, multiple=false) {
            let url="rhino/geometry/areamassproperties/compute-surface";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        compute8 : function(surface, area, firstMoments, secondMoments, productMoments, multiple=false) {
            let url="rhino/geometry/areamassproperties/compute-surface_bool_bool_bool_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, area, firstMoments, secondMoments, productMoments);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        compute9 : function(geometry, multiple=false) {
            let url="rhino/geometry/areamassproperties/compute-geometrybasearray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, geometry);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        compute10 : function(geometry, area, firstMoments, secondMoments, productMoments, multiple=false) {
            let url="rhino/geometry/areamassproperties/compute-geometrybasearray_bool_bool_bool_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, geometry, area, firstMoments, secondMoments, productMoments);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

    },

    VolumeMassProperties : {
        compute : function(mesh, multiple=false) {
            let url="rhino/geometry/volumemassproperties/compute-mesh";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        compute1 : function(mesh, volume, firstMoments, secondMoments, productMoments, multiple=false) {
            let url="rhino/geometry/volumemassproperties/compute-mesh_bool_bool_bool_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, volume, firstMoments, secondMoments, productMoments);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        compute2 : function(brep, multiple=false) {
            let url="rhino/geometry/volumemassproperties/compute-brep";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        compute3 : function(brep, volume, firstMoments, secondMoments, productMoments, multiple=false) {
            let url="rhino/geometry/volumemassproperties/compute-brep_bool_bool_bool_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, volume, firstMoments, secondMoments, productMoments);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        compute4 : function(surface, multiple=false) {
            let url="rhino/geometry/volumemassproperties/compute-surface";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        compute5 : function(surface, volume, firstMoments, secondMoments, productMoments, multiple=false) {
            let url="rhino/geometry/volumemassproperties/compute-surface_bool_bool_bool_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, volume, firstMoments, secondMoments, productMoments);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        compute6 : function(geometry, multiple=false) {
            let url="rhino/geometry/volumemassproperties/compute-geometrybasearray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, geometry);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        compute7 : function(geometry, volume, firstMoments, secondMoments, productMoments, multiple=false) {
            let url="rhino/geometry/volumemassproperties/compute-geometrybasearray_bool_bool_bool_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, geometry, volume, firstMoments, secondMoments, productMoments);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        sum : function(volumemassproperties, summand, multiple=false) {
            let url="rhino/geometry/volumemassproperties/sum-volumemassproperties_volumemassproperties";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, volumemassproperties, summand);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

    },

    Mesh : {
        createFromPlane : function(plane, xInterval, yInterval, xCount, yCount, multiple=false) {
            let url="rhino/geometry/mesh/createfromplane-plane_interval_interval_int_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, plane, xInterval, yInterval, xCount, yCount);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromBox : function(box, xCount, yCount, zCount, multiple=false) {
            let url="rhino/geometry/mesh/createfrombox-boundingbox_int_int_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, box, xCount, yCount, zCount);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromBox1 : function(box, xCount, yCount, zCount, multiple=false) {
            let url="rhino/geometry/mesh/createfrombox-box_int_int_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, box, xCount, yCount, zCount);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromBox2 : function(corners, xCount, yCount, zCount, multiple=false) {
            let url="rhino/geometry/mesh/createfrombox-point3darray_int_int_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, corners, xCount, yCount, zCount);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromSphere : function(sphere, xCount, yCount, multiple=false) {
            let url="rhino/geometry/mesh/createfromsphere-sphere_int_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, sphere, xCount, yCount);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createIcoSphere : function(sphere, subdivisions, multiple=false) {
            let url="rhino/geometry/mesh/createicosphere-sphere_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, sphere, subdivisions);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createQuadSphere : function(sphere, subdivisions, multiple=false) {
            let url="rhino/geometry/mesh/createquadsphere-sphere_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, sphere, subdivisions);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromCylinder : function(cylinder, vertical, around, multiple=false) {
            let url="rhino/geometry/mesh/createfromcylinder-cylinder_int_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, cylinder, vertical, around);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromCone : function(cone, vertical, around, multiple=false) {
            let url="rhino/geometry/mesh/createfromcone-cone_int_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, cone, vertical, around);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromCone1 : function(cone, vertical, around, solid, multiple=false) {
            let url="rhino/geometry/mesh/createfromcone-cone_int_int_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, cone, vertical, around, solid);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromPlanarBoundary : function(boundary, parameters, multiple=false) {
            let url="rhino/geometry/mesh/createfromplanarboundary-curve_meshingparameters";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, boundary, parameters);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromPlanarBoundary1 : function(boundary, parameters, tolerance, multiple=false) {
            let url="rhino/geometry/mesh/createfromplanarboundary-curve_meshingparameters_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, boundary, parameters, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromClosedPolyline : function(polyline, multiple=false) {
            let url="rhino/geometry/mesh/createfromclosedpolyline-polyline";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, polyline);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromTessellation : function(points, edges, plane, allowNewVertices, multiple=false) {
            let url="rhino/geometry/mesh/createfromtessellation-point3darray_ienumerable<point3d>array_plane_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, points, edges, plane, allowNewVertices);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromBrep : function(brep, multiple=false) {
            let url="rhino/geometry/mesh/createfrombrep-brep";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromBrep1 : function(brep, meshingParameters, multiple=false) {
            let url="rhino/geometry/mesh/createfrombrep-brep_meshingparameters";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, meshingParameters);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromSurface : function(surface, multiple=false) {
            let url="rhino/geometry/mesh/createfromsurface-surface";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromSurface1 : function(surface, meshingParameters, multiple=false) {
            let url="rhino/geometry/mesh/createfromsurface-surface_meshingparameters";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, meshingParameters);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createPatch : function(outerBoundary, angleToleranceRadians, pullbackSurface, innerBoundaryCurves, innerBothSideCurves, innerPoints, trimback, divisions, multiple=false) {
            let url="rhino/geometry/mesh/createpatch-polyline_double_surface_curvearray_curvearray_point3darray_bool_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, outerBoundary, angleToleranceRadians, pullbackSurface, innerBoundaryCurves, innerBothSideCurves, innerPoints, trimback, divisions);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBooleanUnion : function(meshes, multiple=false) {
            let url="rhino/geometry/mesh/createbooleanunion-mesharray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, meshes);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBooleanDifference : function(firstSet, secondSet, multiple=false) {
            let url="rhino/geometry/mesh/createbooleandifference-mesharray_mesharray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, firstSet, secondSet);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBooleanIntersection : function(firstSet, secondSet, multiple=false) {
            let url="rhino/geometry/mesh/createbooleanintersection-mesharray_mesharray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, firstSet, secondSet);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createBooleanSplit : function(meshesToSplit, meshSplitters, multiple=false) {
            let url="rhino/geometry/mesh/createbooleansplit-mesharray_mesharray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, meshesToSplit, meshSplitters);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromCurvePipe : function(curve, radius, segments, accuracy, capType, faceted, intervals, multiple=false) {
            let url="rhino/geometry/mesh/createfromcurvepipe-curve_double_int_int_meshpipecapstyle_bool_intervalarray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, radius, segments, accuracy, capType, faceted, intervals);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        volume : function(mesh, multiple=false) {
            let url="rhino/geometry/mesh/volume-mesh";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        smooth : function(mesh, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem, multiple=false) {
            let url="rhino/geometry/mesh/smooth-mesh_double_bool_bool_bool_bool_smoothingcoordinatesystem";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        smooth1 : function(mesh, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem, plane, multiple=false) {
            let url="rhino/geometry/mesh/smooth-mesh_double_bool_bool_bool_bool_smoothingcoordinatesystem_plane";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem, plane);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        smooth2 : function(mesh, vertexIndices, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem, plane, multiple=false) {
            let url="rhino/geometry/mesh/smooth-mesh_intarray_double_bool_bool_bool_bool_smoothingcoordinatesystem_plane";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, vertexIndices, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem, plane);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        unweld : function(mesh, angleToleranceRadians, modifyNormals, multiple=false) {
            let url="rhino/geometry/mesh/unweld-mesh_double_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, angleToleranceRadians, modifyNormals);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        unweldEdge : function(mesh, edgeIndices, modifyNormals, multiple=false) {
            let url="rhino/geometry/mesh/unweldedge-mesh_intarray_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, edgeIndices, modifyNormals);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        weld : function(mesh, angleToleranceRadians, multiple=false) {
            let url="rhino/geometry/mesh/weld-mesh_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, angleToleranceRadians);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        rebuildNormals : function(mesh, multiple=false) {
            let url="rhino/geometry/mesh/rebuildnormals-mesh";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        extractNonManifoldEdges : function(mesh, selective, multiple=false) {
            let url="rhino/geometry/mesh/extractnonmanifoldedges-mesh_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, selective);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        healNakedEdges : function(mesh, distance, multiple=false) {
            let url="rhino/geometry/mesh/healnakededges-mesh_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, distance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        fillHoles : function(mesh, multiple=false) {
            let url="rhino/geometry/mesh/fillholes-mesh";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        fileHole : function(mesh, topologyEdgeIndex, multiple=false) {
            let url="rhino/geometry/mesh/filehole-mesh_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, topologyEdgeIndex);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        unifyNormals : function(mesh, multiple=false) {
            let url="rhino/geometry/mesh/unifynormals-mesh";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        unifyNormals1 : function(mesh, countOnly, multiple=false) {
            let url="rhino/geometry/mesh/unifynormals-mesh_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, countOnly);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        splitDisjointPieces : function(mesh, multiple=false) {
            let url="rhino/geometry/mesh/splitdisjointpieces-mesh";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        split : function(mesh, plane, multiple=false) {
            let url="rhino/geometry/mesh/split-mesh_plane";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, plane);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        split1 : function(mesh, mesh2, multiple=false) {
            let url="rhino/geometry/mesh/split-mesh_mesh";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, mesh2);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        split2 : function(mesh, meshes, multiple=false) {
            let url="rhino/geometry/mesh/split-mesh_mesharray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, meshes);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        getOutlines : function(mesh, plane, multiple=false) {
            let url="rhino/geometry/mesh/getoutlines-mesh_plane";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, plane);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        getOutlines1 : function(mesh, viewport, multiple=false) {
            let url="rhino/geometry/mesh/getoutlines-mesh_display.rhinoviewport";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, viewport);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        getOutlines2 : function(mesh, viewportInfo, plane, multiple=false) {
            let url="rhino/geometry/mesh/getoutlines-mesh_viewportinfo_plane";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, viewportInfo, plane);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        getNakedEdges : function(mesh, multiple=false) {
            let url="rhino/geometry/mesh/getnakededges-mesh";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        explodeAtUnweldedEdges : function(mesh, multiple=false) {
            let url="rhino/geometry/mesh/explodeatunweldededges-mesh";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        closestPoint : function(mesh, testPoint, multiple=false) {
            let url="rhino/geometry/mesh/closestpoint-mesh_point3d";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, testPoint);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        closestMeshPoint : function(mesh, testPoint, maximumDistance, multiple=false) {
            let url="rhino/geometry/mesh/closestmeshpoint-mesh_point3d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, testPoint, maximumDistance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        closestPoint1 : function(mesh, testPoint, maximumDistance, multiple=false) {
            let url="rhino/geometry/mesh/closestpoint-mesh_point3d_point3d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, testPoint, maximumDistance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        closestPoint2 : function(mesh, testPoint, maximumDistance, multiple=false) {
            let url="rhino/geometry/mesh/closestpoint-mesh_point3d_point3d_vector3d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, testPoint, maximumDistance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        pointAt : function(mesh, meshPoint, multiple=false) {
            let url="rhino/geometry/mesh/pointat-mesh_meshpoint";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, meshPoint);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        pointAt1 : function(mesh, faceIndex, t0, t1, t2, t3, multiple=false) {
            let url="rhino/geometry/mesh/pointat-mesh_int_double_double_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, faceIndex, t0, t1, t2, t3);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        normalAt : function(mesh, meshPoint, multiple=false) {
            let url="rhino/geometry/mesh/normalat-mesh_meshpoint";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, meshPoint);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        normalAt1 : function(mesh, faceIndex, t0, t1, t2, t3, multiple=false) {
            let url="rhino/geometry/mesh/normalat-mesh_int_double_double_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, faceIndex, t0, t1, t2, t3);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        colorAt : function(mesh, meshPoint, multiple=false) {
            let url="rhino/geometry/mesh/colorat-mesh_meshpoint";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, meshPoint);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        colorAt1 : function(mesh, faceIndex, t0, t1, t2, t3, multiple=false) {
            let url="rhino/geometry/mesh/colorat-mesh_int_double_double_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, faceIndex, t0, t1, t2, t3);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        pullPointsToMesh : function(mesh, points, multiple=false) {
            let url="rhino/geometry/mesh/pullpointstomesh-mesh_point3darray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, points);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        offset : function(mesh, distance, multiple=false) {
            let url="rhino/geometry/mesh/offset-mesh_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, distance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        offset1 : function(mesh, distance, solidify, multiple=false) {
            let url="rhino/geometry/mesh/offset-mesh_double_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, distance, solidify);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        offset2 : function(mesh, distance, solidify, direction, multiple=false) {
            let url="rhino/geometry/mesh/offset-mesh_double_bool_vector3d";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, distance, solidify, direction);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        collapseFacesByEdgeLength : function(mesh, bGreaterThan, edgeLength, multiple=false) {
            let url="rhino/geometry/mesh/collapsefacesbyedgelength-mesh_bool_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, bGreaterThan, edgeLength);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        collapseFacesByArea : function(mesh, lessThanArea, greaterThanArea, multiple=false) {
            let url="rhino/geometry/mesh/collapsefacesbyarea-mesh_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, lessThanArea, greaterThanArea);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        collapseFacesByByAspectRatio : function(mesh, aspectRatio, multiple=false) {
            let url="rhino/geometry/mesh/collapsefacesbybyaspectratio-mesh_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, aspectRatio);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        getUnsafeLock : function(mesh, writable, multiple=false) {
            let url="rhino/geometry/mesh/getunsafelock-mesh_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, writable);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        releaseUnsafeLock : function(mesh, meshData, multiple=false) {
            let url="rhino/geometry/mesh/releaseunsafelock-mesh_meshunsafelock";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, meshData);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        withShutLining : function(mesh, faceted, tolerance, curves, multiple=false) {
            let url="rhino/geometry/mesh/withshutlining-mesh_bool_double_shutliningcurveinfoarray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, faceted, tolerance, curves);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        withDisplacement : function(mesh, displacement, multiple=false) {
            let url="rhino/geometry/mesh/withdisplacement-mesh_meshdisplacementinfo";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, displacement);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        withEdgeSoftening : function(mesh, softeningRadius, chamfer, faceted, force, angleThreshold, multiple=false) {
            let url="rhino/geometry/mesh/withedgesoftening-mesh_double_bool_bool_bool_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, softeningRadius, chamfer, faceted, force, angleThreshold);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        reduce : function(mesh, desiredPolygonCount, allowDistortion, accuracy, normalizeSize, multiple=false) {
            let url="rhino/geometry/mesh/reduce-mesh_int_bool_int_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, desiredPolygonCount, allowDistortion, accuracy, normalizeSize);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        reduce1 : function(mesh, desiredPolygonCount, allowDistortion, accuracy, normalizeSize, cancelToken, progress, multiple=false) {
            let url="rhino/geometry/mesh/reduce-mesh_int_bool_int_bool_system.threading.cancellationtoken_doublearray_string";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, desiredPolygonCount, allowDistortion, accuracy, normalizeSize, cancelToken, progress);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        reduce2 : function(mesh, parameters, multiple=false) {
            let url="rhino/geometry/mesh/reduce-mesh_reducemeshparameters";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, parameters);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        computeThickness : function(meshes, maximumThickness, multiple=false) {
            let url="rhino/geometry/mesh/computethickness-mesharray_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, meshes, maximumThickness);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        computeThickness1 : function(meshes, maximumThickness, cancelToken, multiple=false) {
            let url="rhino/geometry/mesh/computethickness-mesharray_double_system.threading.cancellationtoken";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, meshes, maximumThickness, cancelToken);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        computeThickness2 : function(meshes, maximumThickness, sharpAngle, cancelToken, multiple=false) {
            let url="rhino/geometry/mesh/computethickness-mesharray_double_double_system.threading.cancellationtoken";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, meshes, maximumThickness, sharpAngle, cancelToken);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createContourCurves : function(meshToContour, contourStart, contourEnd, interval, multiple=false) {
            let url="rhino/geometry/mesh/createcontourcurves-mesh_point3d_point3d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, meshToContour, contourStart, contourEnd, interval);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createContourCurves1 : function(meshToContour, sectionPlane, multiple=false) {
            let url="rhino/geometry/mesh/createcontourcurves-mesh_plane";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, meshToContour, sectionPlane);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },
    },

    NurbsCurve : {
        makeCompatible : function(curves, startPt, endPt, simplifyMethod, numPoints, refitTolerance, angleTolerance, multiple=false) {
            let url="rhino/geometry/nurbscurve/makecompatible-curvearray_point3d_point3d_int_int_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curves, startPt, endPt, simplifyMethod, numPoints, refitTolerance, angleTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createParabolaFromVertex : function(vertex, startPoint, endPoint, multiple=false) {
            let url="rhino/geometry/nurbscurve/createparabolafromvertex-point3d_point3d_point3d";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, vertex, startPoint, endPoint);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createParabolaFromFocus : function(focus, startPoint, endPoint, multiple=false) {
            let url="rhino/geometry/nurbscurve/createparabolafromfocus-point3d_point3d_point3d";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, focus, startPoint, endPoint);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromArc : function(arc, degree, cvCount, multiple=false) {
            let url="rhino/geometry/nurbscurve/createfromarc-arc_int_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, arc, degree, cvCount);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromCircle : function(circle, degree, cvCount, multiple=false) {
            let url="rhino/geometry/nurbscurve/createfromcircle-circle_int_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, circle, degree, cvCount);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        setEndCondition : function(nurbscurve, bSetEnd, continuity, point, tangent, multiple=false) {
            let url="rhino/geometry/nurbscurve/setendcondition-nurbscurve_bool_nurbscurveendconditiontype_point3d_vector3d";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, nurbscurve, bSetEnd, continuity, point, tangent);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        setEndCondition1 : function(nurbscurve, bSetEnd, continuity, point, tangent, curvature, multiple=false) {
            let url="rhino/geometry/nurbscurve/setendcondition-nurbscurve_bool_nurbscurveendconditiontype_point3d_vector3d_vector3d";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, nurbscurve, bSetEnd, continuity, point, tangent, curvature);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        setGrevillePoints : function(nurbscurve, points, multiple=false) {
            let url="rhino/geometry/nurbscurve/setgrevillepoints-nurbscurve_point3darray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, nurbscurve, points);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createSpiral : function(axisStart, axisDir, radiusPoint, pitch, turnCount, radius0, radius1, multiple=false) {
            let url="rhino/geometry/nurbscurve/createspiral-point3d_vector3d_point3d_double_double_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, axisStart, axisDir, radiusPoint, pitch, turnCount, radius0, radius1);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createSpiral1 : function(railCurve, t0, t1, radiusPoint, pitch, turnCount, radius0, radius1, pointsPerTurn, multiple=false) {
            let url="rhino/geometry/nurbscurve/createspiral-curve_double_double_point3d_double_double_double_double_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, railCurve, t0, t1, radiusPoint, pitch, turnCount, radius0, radius1, pointsPerTurn);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },
    },

    NurbsSurface : {
        createCurveOnSurfacePoints : function(surface, fixedPoints, tolerance, periodic, initCount, levels, multiple=false) {
            let url="rhino/geometry/nurbssurface/createcurveonsurfacepoints-surface_point2darray_double_bool_int_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, fixedPoints, tolerance, periodic, initCount, levels);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createCurveOnSurface : function(surface, points, tolerance, periodic, multiple=false) {
            let url="rhino/geometry/nurbssurface/createcurveonsurface-surface_point2darray_double_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, points, tolerance, periodic);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        makeCompatible : function(surface0, surface1, multiple=false) {
            let url="rhino/geometry/nurbssurface/makecompatible-surface_surface_nurbssurface_nurbssurface";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface0, surface1);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromPoints : function(points, uCount, vCount, uDegree, vDegree, multiple=false) {
            let url="rhino/geometry/nurbssurface/createfrompoints-point3darray_int_int_int_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, points, uCount, vCount, uDegree, vDegree);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createThroughPoints : function(points, uCount, vCount, uDegree, vDegree, uClosed, vClosed, multiple=false) {
            let url="rhino/geometry/nurbssurface/createthroughpoints-point3darray_int_int_int_int_bool_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, points, uCount, vCount, uDegree, vDegree, uClosed, vClosed);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromCorners : function(corner1, corner2, corner3, corner4, multiple=false) {
            let url="rhino/geometry/nurbssurface/createfromcorners-point3d_point3d_point3d_point3d";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, corner1, corner2, corner3, corner4);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromCorners1 : function(corner1, corner2, corner3, corner4, tolerance, multiple=false) {
            let url="rhino/geometry/nurbssurface/createfromcorners-point3d_point3d_point3d_point3d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, corner1, corner2, corner3, corner4, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createFromCorners2 : function(corner1, corner2, corner3, multiple=false) {
            let url="rhino/geometry/nurbssurface/createfromcorners-point3d_point3d_point3d";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, corner1, corner2, corner3);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createRailRevolvedSurface : function(profile, rail, axis, scaleHeight, multiple=false) {
            let url="rhino/geometry/nurbssurface/createrailrevolvedsurface-curve_curve_line_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, profile, rail, axis, scaleHeight);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createNetworkSurface : function(uCurves, uContinuityStart, uContinuityEnd, vCurves, vContinuityStart, vContinuityEnd, edgeTolerance, interiorTolerance, angleTolerance, multiple=false) {
            let url="rhino/geometry/nurbssurface/createnetworksurface-curvearray_int_int_curvearray_int_int_double_double_double_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, uCurves, uContinuityStart, uContinuityEnd, vCurves, vContinuityStart, vContinuityEnd, edgeTolerance, interiorTolerance, angleTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createNetworkSurface1 : function(curves, continuity, edgeTolerance, interiorTolerance, angleTolerance, multiple=false) {
            let url="rhino/geometry/nurbssurface/createnetworksurface-curvearray_int_double_double_double_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curves, continuity, edgeTolerance, interiorTolerance, angleTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },
    },

    Surface : {
        createRollingBallFillet : function(surfaceA, surfaceB, radius, tolerance, multiple=false) {
            let url="rhino/geometry/surface/createrollingballfillet-surface_surface_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surfaceA, surfaceB, radius, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createRollingBallFillet1 : function(surfaceA, flipA, surfaceB, flipB, radius, tolerance, multiple=false) {
            let url="rhino/geometry/surface/createrollingballfillet-surface_bool_surface_bool_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surfaceA, flipA, surfaceB, flipB, radius, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createRollingBallFillet2 : function(surfaceA, uvA, surfaceB, uvB, radius, tolerance, multiple=false) {
            let url="rhino/geometry/surface/createrollingballfillet-surface_point2d_surface_point2d_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surfaceA, uvA, surfaceB, uvB, radius, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createExtrusion : function(profile, direction, multiple=false) {
            let url="rhino/geometry/surface/createextrusion-curve_vector3d";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, profile, direction);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createExtrusionToPoint : function(profile, apexPoint, multiple=false) {
            let url="rhino/geometry/surface/createextrusiontopoint-curve_point3d";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, profile, apexPoint);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createPeriodicSurface : function(surface, direction, multiple=false) {
            let url="rhino/geometry/surface/createperiodicsurface-surface_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, direction);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createPeriodicSurface1 : function(surface, direction, bSmooth, multiple=false) {
            let url="rhino/geometry/surface/createperiodicsurface-surface_int_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, direction, bSmooth);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        createSoftEditSurface : function(surface, uv, delta, uLength, vLength, tolerance, fixEnds, multiple=false) {
            let url="rhino/geometry/surface/createsofteditsurface-surface_point2d_vector3d_double_double_double_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, uv, delta, uLength, vLength, tolerance, fixEnds);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        smooth : function(surface, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem, multiple=false) {
            let url="rhino/geometry/surface/smooth-surface_double_bool_bool_bool_bool_smoothingcoordinatesystem";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        smooth1 : function(surface, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem, plane, multiple=false) {
            let url="rhino/geometry/surface/smooth-surface_double_bool_bool_bool_bool_smoothingcoordinatesystem_plane";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem, plane);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        variableOffset : function(surface, uMinvMin, uMinvMax, uMaxvMin, uMaxvMax, tolerance, multiple=false) {
            let url="rhino/geometry/surface/variableoffset-surface_double_double_double_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, uMinvMin, uMinvMax, uMaxvMin, uMaxvMax, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        variableOffset1 : function(surface, uMinvMin, uMinvMax, uMaxvMin, uMaxvMax, interiorParameters, interiorDistances, tolerance, multiple=false) {
            let url="rhino/geometry/surface/variableoffset-surface_double_double_double_double_point2darray_doublearray_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, uMinvMin, uMinvMax, uMaxvMin, uMaxvMax, interiorParameters, interiorDistances, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        getSurfaceSize : function(surface, multiple=false) {
            let url="rhino/geometry/surface/getsurfacesize-surface_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        closestSide : function(surface, u, v, multiple=false) {
            let url="rhino/geometry/surface/closestside-surface_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, u, v);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        extend : function(surface, edge, extensionLength, smooth, multiple=false) {
            let url="rhino/geometry/surface/extend-surface_isostatus_double_bool";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, edge, extensionLength, smooth);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        rebuild : function(surface, uDegree, vDegree, uPointCount, vPointCount, multiple=false) {
            let url="rhino/geometry/surface/rebuild-surface_int_int_int_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, uDegree, vDegree, uPointCount, vPointCount);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        rebuildOneDirection : function(surface, direction, pointCount, loftType, refitTolerance, multiple=false) {
            let url="rhino/geometry/surface/rebuildonedirection-surface_int_int_lofttype_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, direction, pointCount, loftType, refitTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        closestPoint : function(surface, testPoint, multiple=false) {
            let url="rhino/geometry/surface/closestpoint-surface_point3d_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, testPoint);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        localClosestPoint : function(surface, testPoint, seedU, seedV, multiple=false) {
            let url="rhino/geometry/surface/localclosestpoint-surface_point3d_double_double_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, testPoint, seedU, seedV);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        offset : function(surface, distance, tolerance, multiple=false) {
            let url="rhino/geometry/surface/offset-surface_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, distance, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        fit : function(surface, uDegree, vDegree, fitTolerance, multiple=false) {
            let url="rhino/geometry/surface/fit-surface_int_int_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, uDegree, vDegree, fitTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        interpolatedCurveOnSurfaceUV : function(surface, points, tolerance, multiple=false) {
            let url="rhino/geometry/surface/interpolatedcurveonsurfaceuv-surface_point2darray_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, points, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        interpolatedCurveOnSurface : function(surface, points, tolerance, multiple=false) {
            let url="rhino/geometry/surface/interpolatedcurveonsurface-surface_point3darray_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, points, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        shortPath : function(surface, start, end, tolerance, multiple=false) {
            let url="rhino/geometry/surface/shortpath-surface_point2d_point2d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, start, end, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        pushup : function(surface, curve2d, tolerance, curve2dSubdomain, multiple=false) {
            let url="rhino/geometry/surface/pushup-surface_curve_double_interval";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, curve2d, tolerance, curve2dSubdomain);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        pushup1 : function(surface, curve2d, tolerance, multiple=false) {
            let url="rhino/geometry/surface/pushup-surface_curve_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, curve2d, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        pullback : function(surface, curve3d, tolerance, multiple=false) {
            let url="rhino/geometry/surface/pullback-surface_curve_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, curve3d, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        pullback1 : function(surface, curve3d, tolerance, curve3dSubdomain, multiple=false) {
            let url="rhino/geometry/surface/pullback-surface_curve_double_interval";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surface, curve3d, tolerance, curve3dSubdomain);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },
    },

    Intersection : {
        curvePlane : function(curve, plane, tolerance, multiple=false) {
            let url="rhino/geometry/intersect/intersection/curveplane-curve_plane_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, plane, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        meshPlane : function(mesh, plane, multiple=false) {
            let url="rhino/geometry/intersect/intersection/meshplane-mesh_plane";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, plane);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        meshPlane1 : function(mesh, planes, multiple=false) {
            let url="rhino/geometry/intersect/intersection/meshplane-mesh_planearray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, planes);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        brepPlane : function(brep, plane, tolerance, multiple=false) {
            let url="rhino/geometry/intersect/intersection/brepplane-brep_plane_double_curvearray_point3darray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, plane, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        curveSelf : function(curve, tolerance, multiple=false) {
            let url="rhino/geometry/intersect/intersection/curveself-curve_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        curveCurve : function(curveA, curveB, tolerance, overlapTolerance, multiple=false) {
            let url="rhino/geometry/intersect/intersection/curvecurve-curve_curve_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curveA, curveB, tolerance, overlapTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        curveLine : function(curve, line, tolerance, overlapTolerance, multiple=false) {
            let url="rhino/geometry/intersect/intersection/curveline-curve_line_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, line, tolerance, overlapTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        curveSurface : function(curve, surface, tolerance, overlapTolerance, multiple=false) {
            let url="rhino/geometry/intersect/intersection/curvesurface-curve_surface_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, surface, tolerance, overlapTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        curveSurface1 : function(curve, curveDomain, surface, tolerance, overlapTolerance, multiple=false) {
            let url="rhino/geometry/intersect/intersection/curvesurface-curve_interval_surface_double_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, curveDomain, surface, tolerance, overlapTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        curveBrep : function(curve, brep, tolerance, multiple=false) {
            let url="rhino/geometry/intersect/intersection/curvebrep-curve_brep_double_curvearray_point3darray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, brep, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        curveBrep1 : function(curve, brep, tolerance, angleTolerance, multiple=false) {
            let url="rhino/geometry/intersect/intersection/curvebrep-curve_brep_double_double_doublearray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, brep, tolerance, angleTolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        curveBrepFace : function(curve, face, tolerance, multiple=false) {
            let url="rhino/geometry/intersect/intersection/curvebrepface-curve_brepface_double_curvearray_point3darray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, curve, face, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        surfaceSurface : function(surfaceA, surfaceB, tolerance, multiple=false) {
            let url="rhino/geometry/intersect/intersection/surfacesurface-surface_surface_double_curvearray_point3darray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, surfaceA, surfaceB, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        brepBrep : function(brepA, brepB, tolerance, multiple=false) {
            let url="rhino/geometry/intersect/intersection/brepbrep-brep_brep_double_curvearray_point3darray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brepA, brepB, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        brepSurface : function(brep, surface, tolerance, multiple=false) {
            let url="rhino/geometry/intersect/intersection/brepsurface-brep_surface_double_curvearray_point3darray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, brep, surface, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        meshMeshFast : function(meshA, meshB, multiple=false) {
            let url="rhino/geometry/intersect/intersection/meshmeshfast-mesh_mesh";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, meshA, meshB);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        meshMesh : function(meshes, tolerance, mode, multiple=false) {
            let url="rhino/geometry/intersect/intersection/meshmesh-mesharray_double_setscombinations";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, meshes, tolerance, mode);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        meshMeshAccurate : function(meshA, meshB, tolerance, multiple=false) {
            let url="rhino/geometry/intersect/intersection/meshmeshaccurate-mesh_mesh_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, meshA, meshB, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        meshRay : function(mesh, ray, multiple=false) {
            let url="rhino/geometry/intersect/intersection/meshray-mesh_ray3d";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, ray);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        meshRay1 : function(mesh, ray, multiple=false) {
            let url="rhino/geometry/intersect/intersection/meshray-mesh_ray3d_intarray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, ray);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        meshPolyline : function(mesh, curve, multiple=false) {
            let url="rhino/geometry/intersect/intersection/meshpolyline-mesh_polylinecurve_intarray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, curve);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        meshLine : function(mesh, line, multiple=false) {
            let url="rhino/geometry/intersect/intersection/meshline-mesh_line_intarray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, mesh, line);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        rayShoot : function(ray, geometry, maxReflections, multiple=false) {
            let url="rhino/geometry/intersect/intersection/rayshoot-ray3d_geometrybasearray_int";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, ray, geometry, maxReflections);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        projectPointsToMeshes : function(meshes, points, direction, tolerance, multiple=false) {
            let url="rhino/geometry/intersect/intersection/projectpointstomeshes-mesharray_point3darray_vector3d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, meshes, points, direction, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        projectPointsToMeshesEx : function(meshes, points, direction, tolerance, multiple=false) {
            let url="rhino/geometry/intersect/intersection/projectpointstomeshesex-mesharray_point3darray_vector3d_double_intarray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, meshes, points, direction, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        projectPointsToBreps : function(breps, points, direction, tolerance, multiple=false) {
            let url="rhino/geometry/intersect/intersection/projectpointstobreps-breparray_point3darray_vector3d_double";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, breps, points, direction, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },

        projectPointsToBrepsEx : function(breps, points, direction, tolerance, multiple=false) {
            let url="rhino/geometry/intersect/intersection/projectpointstobrepsex-breparray_point3darray_vector3d_double_intarray";
            if(multiple) url = url + "?multiple=true"
            let args = RhinoCompute.zipArgs(multiple, breps, points, direction, tolerance);
            var promise = RhinoCompute.computeFetch(url, args);
            return promise;
        },
    },
};

// export RhinoCompute object if node.js
if (typeof exports === 'object' && typeof module === 'object')
    module.exports = RhinoCompute;