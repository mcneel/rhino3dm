
var RhinoCompute = {
    url: "https://compute.rhino3d.com/",

    authToken: null,

    getAuthToken: function(useLocalStorage=true) {
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
    },

    computeFetch: function(endpoint, arglist) {
        for (i = 0; i < arglist.length; i++) {
            if (arglist[i].encode != null)
                arglist[i] = arglist[i].encode();
        }
        return fetch(RhinoCompute.url+endpoint, {
                "method":"POST",
                "body": JSON.stringify(arglist),
                "headers": {"Authorization":RhinoCompute.authToken}
        }).then(r=>r.json());
    },

    Brep : {
        changeSeam : function(face, direction, parameter, tolerance) {
            args = [face, direction, parameter, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/changeseam-brepface_int_double_double", args);
            return promise;
        },

        copyTrimCurves : function(trimSource, surfaceSource, tolerance) {
            args = [trimSource, surfaceSource, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/copytrimcurves-brepface_surface_double", args);
            return promise;
        },

        createBaseballSphere : function(center, radius, tolerance) {
            args = [center, radius, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createbaseballsphere-point3d_double_double", args);
            return promise;
        },

        createDevelopableLoft : function(crv0, crv1, reverse0, reverse1, density) {
            args = [crv0, crv1, reverse0, reverse1, density];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createdevelopableloft-curve_curve_bool_bool_int", args);
            return promise;
        },

        createDevelopableLoft1 : function(rail0, rail1, fixedRulings) {
            args = [rail0, rail1, fixedRulings];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createdevelopableloft-nurbscurve_nurbscurve_ienumerable<point2d>", args);
            return promise;
        },

        createPlanarBreps : function(inputLoops) {
            args = [inputLoops];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createplanarbreps-ienumerable<curve>", args);
            return promise;
        },

        createPlanarBreps1 : function(inputLoops, tolerance) {
            args = [inputLoops, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createplanarbreps-ienumerable<curve>_double", args);
            return promise;
        },

        createPlanarBreps2 : function(inputLoop) {
            args = [inputLoop];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createplanarbreps-curve", args);
            return promise;
        },

        createPlanarBreps3 : function(inputLoop, tolerance) {
            args = [inputLoop, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createplanarbreps-curve_double", args);
            return promise;
        },

        createTrimmedSurface : function(trimSource, surfaceSource) {
            args = [trimSource, surfaceSource];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createtrimmedsurface-brepface_surface", args);
            return promise;
        },

        createTrimmedSurface1 : function(trimSource, surfaceSource, tolerance) {
            args = [trimSource, surfaceSource, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createtrimmedsurface-brepface_surface_double", args);
            return promise;
        },

        createFromCornerPoints : function(corner1, corner2, corner3, tolerance) {
            args = [corner1, corner2, corner3, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createfromcornerpoints-point3d_point3d_point3d_double", args);
            return promise;
        },

        createFromCornerPoints1 : function(corner1, corner2, corner3, corner4, tolerance) {
            args = [corner1, corner2, corner3, corner4, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createfromcornerpoints-point3d_point3d_point3d_point3d_double", args);
            return promise;
        },

        createEdgeSurface : function(curves) {
            args = [curves];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createedgesurface-ienumerable<curve>", args);
            return promise;
        },

        createPlanarBreps : function(inputLoops) {
            args = [inputLoops];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createplanarbreps-rhino.collections.curvelist", args);
            return promise;
        },

        createPlanarBreps1 : function(inputLoops, tolerance) {
            args = [inputLoops, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createplanarbreps-rhino.collections.curvelist_double", args);
            return promise;
        },

        createFromOffsetFace : function(face, offsetDistance, offsetTolerance, bothSides, createSolid) {
            args = [face, offsetDistance, offsetTolerance, bothSides, createSolid];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createfromoffsetface-brepface_double_double_bool_bool", args);
            return promise;
        },

        createSolid : function(breps, tolerance) {
            args = [breps, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createsolid-ienumerable<brep>_double", args);
            return promise;
        },

        mergeSurfaces : function(surface0, surface1, tolerance, angleToleranceRadians) {
            args = [surface0, surface1, tolerance, angleToleranceRadians];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/mergesurfaces-surface_surface_double_double", args);
            return promise;
        },

        mergeSurfaces1 : function(brep0, brep1, tolerance, angleToleranceRadians) {
            args = [brep0, brep1, tolerance, angleToleranceRadians];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/mergesurfaces-brep_brep_double_double", args);
            return promise;
        },

        mergeSurfaces2 : function(brep0, brep1, tolerance, angleToleranceRadians, point0, point1, roundness, smooth) {
            args = [brep0, brep1, tolerance, angleToleranceRadians, point0, point1, roundness, smooth];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/mergesurfaces-brep_brep_double_double_point2d_point2d_double_bool", args);
            return promise;
        },

        createPatch : function(geometry, startingSurface, tolerance) {
            args = [geometry, startingSurface, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createpatch-ienumerable<geometrybase>_surface_double", args);
            return promise;
        },

        createPatch1 : function(geometry, uSpans, vSpans, tolerance) {
            args = [geometry, uSpans, vSpans, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createpatch-ienumerable<geometrybase>_int_int_double", args);
            return promise;
        },

        createPatch2 : function(geometry, startingSurface, uSpans, vSpans, trim, tangency, pointSpacing, flexibility, surfacePull, fixEdges, tolerance) {
            args = [geometry, startingSurface, uSpans, vSpans, trim, tangency, pointSpacing, flexibility, surfacePull, fixEdges, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createpatch-ienumerable<geometrybase>_surface_int_int_bool_bool_double_double_double_bool[]_double", args);
            return promise;
        },

        createPipe : function(rail, radius, localBlending, cap, fitRail, absoluteTolerance, angleToleranceRadians) {
            args = [rail, radius, localBlending, cap, fitRail, absoluteTolerance, angleToleranceRadians];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createpipe-curve_double_bool_pipecapmode_bool_double_double", args);
            return promise;
        },

        createPipe1 : function(rail, railRadiiParameters, radii, localBlending, cap, fitRail, absoluteTolerance, angleToleranceRadians) {
            args = [rail, railRadiiParameters, radii, localBlending, cap, fitRail, absoluteTolerance, angleToleranceRadians];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createpipe-curve_ienumerable<double>_ienumerable<double>_bool_pipecapmode_bool_double_double", args);
            return promise;
        },

        createFromSweep : function(rail, shape, closed, tolerance) {
            args = [rail, shape, closed, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createfromsweep-curve_curve_bool_double", args);
            return promise;
        },

        createFromSweep1 : function(rail, shapes, closed, tolerance) {
            args = [rail, shapes, closed, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createfromsweep-curve_ienumerable<curve>_bool_double", args);
            return promise;
        },

        createFromSweep2 : function(rail1, rail2, shape, closed, tolerance) {
            args = [rail1, rail2, shape, closed, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createfromsweep-curve_curve_curve_bool_double", args);
            return promise;
        },

        createFromSweep3 : function(rail1, rail2, shapes, closed, tolerance) {
            args = [rail1, rail2, shapes, closed, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createfromsweep-curve_curve_ienumerable<curve>_bool_double", args);
            return promise;
        },

        createFromSweepInParts : function(rail1, rail2, shapes, rail_params, closed, tolerance) {
            args = [rail1, rail2, shapes, rail_params, closed, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createfromsweepinparts-curve_curve_ienumerable<curve>_ienumerable<point2d>_bool_double", args);
            return promise;
        },

        createFromTaperedExtrude : function(curveToExtrude, distance, direction, basePoint, draftAngleRadians, cornerType, tolerance, angleToleranceRadians) {
            args = [curveToExtrude, distance, direction, basePoint, draftAngleRadians, cornerType, tolerance, angleToleranceRadians];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createfromtaperedextrude-curve_double_vector3d_point3d_double_extrudecornertype_double_double", args);
            return promise;
        },

        createFromTaperedExtrude1 : function(curveToExtrude, distance, direction, basePoint, draftAngleRadians, cornerType) {
            args = [curveToExtrude, distance, direction, basePoint, draftAngleRadians, cornerType];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createfromtaperedextrude-curve_double_vector3d_point3d_double_extrudecornertype", args);
            return promise;
        },

        createBlendSurface : function(face0, edge0, domain0, rev0, continuity0, face1, edge1, domain1, rev1, continuity1) {
            args = [face0, edge0, domain0, rev0, continuity0, face1, edge1, domain1, rev1, continuity1];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createblendsurface-brepface_brepedge_interval_bool_blendcontinuity_brepface_brepedge_interval_bool_blendcontinuity", args);
            return promise;
        },

        createBlendShape : function(face0, edge0, t0, rev0, continuity0, face1, edge1, t1, rev1, continuity1) {
            args = [face0, edge0, t0, rev0, continuity0, face1, edge1, t1, rev1, continuity1];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createblendshape-brepface_brepedge_double_bool_blendcontinuity_brepface_brepedge_double_bool_blendcontinuity", args);
            return promise;
        },

        createFilletSurface : function(face0, uv0, face1, uv1, radius, extend, tolerance) {
            args = [face0, uv0, face1, uv1, radius, extend, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createfilletsurface-brepface_point2d_brepface_point2d_double_bool_double", args);
            return promise;
        },

        createChamferSurface : function(face0, uv0, radius0, face1, uv1, radius1, extend, tolerance) {
            args = [face0, uv0, radius0, face1, uv1, radius1, extend, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createchamfersurface-brepface_point2d_double_brepface_point2d_double_bool_double", args);
            return promise;
        },

        createFilletEdges : function(brep, edgeIndices, startRadii, endRadii, blendType, railType, tolerance) {
            args = [brep, edgeIndices, startRadii, endRadii, blendType, railType, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createfilletedges-brep_ienumerable<int>_ienumerable<double>_ienumerable<double>_blendtype_railtype_double", args);
            return promise;
        },

        createFromJoinedEdges : function(brep0, edgeIndex0, brep1, edgeIndex1, joinTolerance) {
            args = [brep0, edgeIndex0, brep1, edgeIndex1, joinTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createfromjoinededges-brep_int_brep_int_double", args);
            return promise;
        },

        createFromLoft : function(curves, start, end, loftType, closed) {
            args = [curves, start, end, loftType, closed];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createfromloft-ienumerable<curve>_point3d_point3d_lofttype_bool", args);
            return promise;
        },

        createFromLoftRebuild : function(curves, start, end, loftType, closed, rebuildPointCount) {
            args = [curves, start, end, loftType, closed, rebuildPointCount];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createfromloftrebuild-ienumerable<curve>_point3d_point3d_lofttype_bool_int", args);
            return promise;
        },

        createFromLoftRefit : function(curves, start, end, loftType, closed, refitTolerance) {
            args = [curves, start, end, loftType, closed, refitTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createfromloftrefit-ienumerable<curve>_point3d_point3d_lofttype_bool_double", args);
            return promise;
        },

        createBooleanUnion : function(breps, tolerance) {
            args = [breps, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createbooleanunion-ienumerable<brep>_double", args);
            return promise;
        },

        createBooleanUnion1 : function(breps, tolerance, manifoldOnly) {
            args = [breps, tolerance, manifoldOnly];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createbooleanunion-ienumerable<brep>_double_bool", args);
            return promise;
        },

        createBooleanIntersection : function(firstSet, secondSet, tolerance) {
            args = [firstSet, secondSet, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createbooleanintersection-ienumerable<brep>_ienumerable<brep>_double", args);
            return promise;
        },

        createBooleanIntersection1 : function(firstSet, secondSet, tolerance, manifoldOnly) {
            args = [firstSet, secondSet, tolerance, manifoldOnly];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createbooleanintersection-ienumerable<brep>_ienumerable<brep>_double_bool", args);
            return promise;
        },

        createBooleanIntersection2 : function(firstBrep, secondBrep, tolerance) {
            args = [firstBrep, secondBrep, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createbooleanintersection-brep_brep_double", args);
            return promise;
        },

        createBooleanIntersection3 : function(firstBrep, secondBrep, tolerance, manifoldOnly) {
            args = [firstBrep, secondBrep, tolerance, manifoldOnly];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createbooleanintersection-brep_brep_double_bool", args);
            return promise;
        },

        createBooleanDifference : function(firstSet, secondSet, tolerance) {
            args = [firstSet, secondSet, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createbooleandifference-ienumerable<brep>_ienumerable<brep>_double", args);
            return promise;
        },

        createBooleanDifference1 : function(firstSet, secondSet, tolerance, manifoldOnly) {
            args = [firstSet, secondSet, tolerance, manifoldOnly];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createbooleandifference-ienumerable<brep>_ienumerable<brep>_double_bool", args);
            return promise;
        },

        createBooleanDifference2 : function(firstBrep, secondBrep, tolerance) {
            args = [firstBrep, secondBrep, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createbooleandifference-brep_brep_double", args);
            return promise;
        },

        createBooleanDifference3 : function(firstBrep, secondBrep, tolerance, manifoldOnly) {
            args = [firstBrep, secondBrep, tolerance, manifoldOnly];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createbooleandifference-brep_brep_double_bool", args);
            return promise;
        },

        createShell : function(brep, facesToRemove, distance, tolerance) {
            args = [brep, facesToRemove, distance, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createshell-brep_ienumerable<int>_double_double", args);
            return promise;
        },

        joinBreps : function(brepsToJoin, tolerance) {
            args = [brepsToJoin, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/joinbreps-ienumerable<brep>_double", args);
            return promise;
        },

        mergeBreps : function(brepsToMerge, tolerance) {
            args = [brepsToMerge, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/mergebreps-ienumerable<brep>_double", args);
            return promise;
        },

        createContourCurves : function(brepToContour, contourStart, contourEnd, interval) {
            args = [brepToContour, contourStart, contourEnd, interval];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createcontourcurves-brep_point3d_point3d_double", args);
            return promise;
        },

        createContourCurves1 : function(brepToContour, sectionPlane) {
            args = [brepToContour, sectionPlane];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createcontourcurves-brep_plane", args);
            return promise;
        },

        createCurvatureAnalysisMesh : function(brep, state) {
            args = [brep, state];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/createcurvatureanalysismesh-brep_rhino.applicationsettings.curvatureanalysissettingsstate", args);
            return promise;
        },

        getRegions : function(brep) {
            args = [brep];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/getregions-brep", args);
            return promise;
        },

        getWireframe : function(brep, density) {
            args = [brep, density];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/getwireframe-brep_int", args);
            return promise;
        },

        closestPoint : function(brep, testPoint) {
            args = [brep, testPoint];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/closestpoint-brep_point3d", args);
            return promise;
        },

        isPointInside : function(brep, point, tolerance, strictlyIn) {
            args = [brep, point, tolerance, strictlyIn];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/ispointinside-brep_point3d_double_bool", args);
            return promise;
        },

        capPlanarHoles : function(brep, tolerance) {
            args = [brep, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/capplanarholes-brep_double", args);
            return promise;
        },

        join : function(brep, otherBrep, tolerance, compact) {
            args = [brep, otherBrep, tolerance, compact];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/join-brep_brep_double_bool", args);
            return promise;
        },

        joinNakedEdges : function(brep, tolerance) {
            args = [brep, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/joinnakededges-brep_double", args);
            return promise;
        },

        mergeCoplanarFaces : function(brep, tolerance) {
            args = [brep, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/mergecoplanarfaces-brep_double", args);
            return promise;
        },

        mergeCoplanarFaces1 : function(brep, tolerance, angleTolerance) {
            args = [brep, tolerance, angleTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/mergecoplanarfaces-brep_double_double", args);
            return promise;
        },

        split : function(brep, splitter, intersectionTolerance) {
            args = [brep, splitter, intersectionTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/split-brep_brep_double", args);
            return promise;
        },

        split1 : function(brep, splitter, intersectionTolerance, toleranceWasRaised) {
            args = [brep, splitter, intersectionTolerance, toleranceWasRaised];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/split-brep_brep_double_bool", args);
            return promise;
        },

        trim : function(brep, cutter, intersectionTolerance) {
            args = [brep, cutter, intersectionTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/trim-brep_brep_double", args);
            return promise;
        },

        trim1 : function(brep, cutter, intersectionTolerance) {
            args = [brep, cutter, intersectionTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/trim-brep_plane_double", args);
            return promise;
        },

        unjoinEdges : function(brep, edgesToUnjoin) {
            args = [brep, edgesToUnjoin];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/unjoinedges-brep_ienumerable<int>", args);
            return promise;
        },

        joinEdges : function(brep, edgeIndex0, edgeIndex1, joinTolerance, compact) {
            args = [brep, edgeIndex0, edgeIndex1, joinTolerance, compact];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/joinedges-brep_int_int_double_bool", args);
            return promise;
        },

        transformComponent : function(brep, components, xform, tolerance, timeLimit, useMultipleThreads) {
            args = [brep, components, xform, tolerance, timeLimit, useMultipleThreads];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/transformcomponent-brep_ienumerable<componentindex>_transform_double_double_bool", args);
            return promise;
        },

        getArea : function(brep) {
            args = [brep];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/getarea-brep", args);
            return promise;
        },

        getArea1 : function(brep, relativeTolerance, absoluteTolerance) {
            args = [brep, relativeTolerance, absoluteTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/getarea-brep_double_double", args);
            return promise;
        },

        getVolume : function(brep) {
            args = [brep];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/getvolume-brep", args);
            return promise;
        },

        getVolume1 : function(brep, relativeTolerance, absoluteTolerance) {
            args = [brep, relativeTolerance, absoluteTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/getvolume-brep_double_double", args);
            return promise;
        },

        rebuildTrimsForV2 : function(brep, face, nurbsSurface) {
            args = [brep, face, nurbsSurface];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/rebuildtrimsforv2-brep_brepface_nurbssurface", args);
            return promise;
        },

        makeValidForV2 : function(brep) {
            args = [brep];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/makevalidforv2-brep", args);
            return promise;
        },

        repair : function(brep, tolerance) {
            args = [brep, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/repair-brep_double", args);
            return promise;
        },

        removeHoles : function(brep, tolerance) {
            args = [brep, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/removeholes-brep_double", args);
            return promise;
        },

        removeHoles1 : function(brep, loops, tolerance) {
            args = [brep, loops, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/brep/removeholes-brep_ienumerable<componentindex>_double", args);
            return promise;
        },
    },

    Curve : {
        getConicSectionType : function(curve) {
            args = [curve];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/getconicsectiontype-curve", args);
            return promise;
        },

        createInterpolatedCurve : function(points, degree) {
            args = [points, degree];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createinterpolatedcurve-ienumerable<point3d>_int", args);
            return promise;
        },

        createInterpolatedCurve1 : function(points, degree, knots) {
            args = [points, degree, knots];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createinterpolatedcurve-ienumerable<point3d>_int_curveknotstyle", args);
            return promise;
        },

        createInterpolatedCurve2 : function(points, degree, knots, startTangent, endTangent) {
            args = [points, degree, knots, startTangent, endTangent];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createinterpolatedcurve-ienumerable<point3d>_int_curveknotstyle_vector3d_vector3d", args);
            return promise;
        },

        createSoftEditCurve : function(curve, t, delta, length, fixEnds) {
            args = [curve, t, delta, length, fixEnds];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createsofteditcurve-curve_double_vector3d_double_bool", args);
            return promise;
        },

        createFilletCornersCurve : function(curve, radius, tolerance, angleTolerance) {
            args = [curve, radius, tolerance, angleTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createfilletcornerscurve-curve_double_double_double", args);
            return promise;
        },

        createArcBlend : function(startPt, startDir, endPt, endDir, controlPointLengthRatio) {
            args = [startPt, startDir, endPt, endDir, controlPointLengthRatio];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createarcblend-point3d_vector3d_point3d_vector3d_double", args);
            return promise;
        },

        createMeanCurve : function(curveA, curveB, angleToleranceRadians) {
            args = [curveA, curveB, angleToleranceRadians];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createmeancurve-curve_curve_double", args);
            return promise;
        },

        createMeanCurve1 : function(curveA, curveB) {
            args = [curveA, curveB];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createmeancurve-curve_curve", args);
            return promise;
        },

        createBlendCurve : function(curveA, curveB, continuity) {
            args = [curveA, curveB, continuity];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createblendcurve-curve_curve_blendcontinuity", args);
            return promise;
        },

        createBlendCurve1 : function(curveA, curveB, continuity, bulgeA, bulgeB) {
            args = [curveA, curveB, continuity, bulgeA, bulgeB];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createblendcurve-curve_curve_blendcontinuity_double_double", args);
            return promise;
        },

        createBlendCurve2 : function(curve0, t0, reverse0, continuity0, curve1, t1, reverse1, continuity1) {
            args = [curve0, t0, reverse0, continuity0, curve1, t1, reverse1, continuity1];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createblendcurve-curve_double_bool_blendcontinuity_curve_double_bool_blendcontinuity", args);
            return promise;
        },

        createTweenCurves : function(curve0, curve1, numCurves) {
            args = [curve0, curve1, numCurves];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createtweencurves-curve_curve_int", args);
            return promise;
        },

        createTweenCurves1 : function(curve0, curve1, numCurves, tolerance) {
            args = [curve0, curve1, numCurves, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createtweencurves-curve_curve_int_double", args);
            return promise;
        },

        createTweenCurvesWithMatching : function(curve0, curve1, numCurves) {
            args = [curve0, curve1, numCurves];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createtweencurveswithmatching-curve_curve_int", args);
            return promise;
        },

        createTweenCurvesWithMatching1 : function(curve0, curve1, numCurves, tolerance) {
            args = [curve0, curve1, numCurves, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createtweencurveswithmatching-curve_curve_int_double", args);
            return promise;
        },

        createTweenCurvesWithSampling : function(curve0, curve1, numCurves, numSamples) {
            args = [curve0, curve1, numCurves, numSamples];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createtweencurveswithsampling-curve_curve_int_int", args);
            return promise;
        },

        createTweenCurvesWithSampling1 : function(curve0, curve1, numCurves, numSamples, tolerance) {
            args = [curve0, curve1, numCurves, numSamples, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createtweencurveswithsampling-curve_curve_int_int_double", args);
            return promise;
        },

        joinCurves : function(inputCurves) {
            args = [inputCurves];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/joincurves-ienumerable<curve>", args);
            return promise;
        },

        joinCurves1 : function(inputCurves, joinTolerance) {
            args = [inputCurves, joinTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/joincurves-ienumerable<curve>_double", args);
            return promise;
        },

        joinCurves2 : function(inputCurves, joinTolerance, preserveDirection) {
            args = [inputCurves, joinTolerance, preserveDirection];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/joincurves-ienumerable<curve>_double_bool", args);
            return promise;
        },

        makeEndsMeet : function(curveA, adjustStartCurveA, curveB, adjustStartCurveB) {
            args = [curveA, adjustStartCurveA, curveB, adjustStartCurveB];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/makeendsmeet-curve_bool_curve_bool", args);
            return promise;
        },

        createFillet : function(curve0, curve1, radius, t0Base, t1Base) {
            args = [curve0, curve1, radius, t0Base, t1Base];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createfillet-curve_curve_double_double_double", args);
            return promise;
        },

        createFilletCurves : function(curve0, point0, curve1, point1, radius, join, trim, arcExtension, tolerance, angleTolerance) {
            args = [curve0, point0, curve1, point1, radius, join, trim, arcExtension, tolerance, angleTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createfilletcurves-curve_point3d_curve_point3d_double_bool_bool_bool_double_double", args);
            return promise;
        },

        createBooleanUnion : function(curves) {
            args = [curves];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createbooleanunion-ienumerable<curve>", args);
            return promise;
        },

        createBooleanUnion1 : function(curves, tolerance) {
            args = [curves, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createbooleanunion-ienumerable<curve>_double", args);
            return promise;
        },

        createBooleanIntersection : function(curveA, curveB) {
            args = [curveA, curveB];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createbooleanintersection-curve_curve", args);
            return promise;
        },

        createBooleanIntersection1 : function(curveA, curveB, tolerance) {
            args = [curveA, curveB, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createbooleanintersection-curve_curve_double", args);
            return promise;
        },

        createBooleanDifference : function(curveA, curveB) {
            args = [curveA, curveB];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createbooleandifference-curve_curve", args);
            return promise;
        },

        createBooleanDifference1 : function(curveA, curveB, tolerance) {
            args = [curveA, curveB, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createbooleandifference-curve_curve_double", args);
            return promise;
        },

        createBooleanDifference2 : function(curveA, subtractors) {
            args = [curveA, subtractors];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createbooleandifference-curve_ienumerable<curve>", args);
            return promise;
        },

        createBooleanDifference3 : function(curveA, subtractors, tolerance) {
            args = [curveA, subtractors, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createbooleandifference-curve_ienumerable<curve>_double", args);
            return promise;
        },

        createTextOutlines : function(text, font, textHeight, textStyle, closeLoops, plane, smallCapsScale, tolerance) {
            args = [text, font, textHeight, textStyle, closeLoops, plane, smallCapsScale, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createtextoutlines-string_string_double_int_bool_plane_double_double", args);
            return promise;
        },

        createCurve2View : function(curveA, curveB, vectorA, vectorB, tolerance, angleTolerance) {
            args = [curveA, curveB, vectorA, vectorB, tolerance, angleTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createcurve2view-curve_curve_vector3d_vector3d_double_double", args);
            return promise;
        },

        doDirectionsMatch : function(curveA, curveB) {
            args = [curveA, curveB];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/dodirectionsmatch-curve_curve", args);
            return promise;
        },

        projectToMesh : function(curve, mesh, direction, tolerance) {
            args = [curve, mesh, direction, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/projecttomesh-curve_mesh_vector3d_double", args);
            return promise;
        },

        projectToMesh1 : function(curve, meshes, direction, tolerance) {
            args = [curve, meshes, direction, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/projecttomesh-curve_ienumerable<mesh>_vector3d_double", args);
            return promise;
        },

        projectToMesh2 : function(curves, meshes, direction, tolerance) {
            args = [curves, meshes, direction, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/projecttomesh-ienumerable<curve>_ienumerable<mesh>_vector3d_double", args);
            return promise;
        },

        projectToBrep : function(curve, brep, direction, tolerance) {
            args = [curve, brep, direction, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/projecttobrep-curve_brep_vector3d_double", args);
            return promise;
        },

        projectToBrep1 : function(curve, breps, direction, tolerance) {
            args = [curve, breps, direction, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/projecttobrep-curve_ienumerable<brep>_vector3d_double", args);
            return promise;
        },

        projectToBrep2 : function(curve, breps, direction, tolerance, brepIndices) {
            args = [curve, breps, direction, tolerance, brepIndices];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/projecttobrep-curve_ienumerable<brep>_vector3d_double_int[]", args);
            return promise;
        },

        projectToBrep3 : function(curves, breps, direction, tolerance) {
            args = [curves, breps, direction, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/projecttobrep-ienumerable<curve>_ienumerable<brep>_vector3d_double", args);
            return promise;
        },

        projectToPlane : function(curve, plane) {
            args = [curve, plane];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/projecttoplane-curve_plane", args);
            return promise;
        },

        pullToBrepFace : function(curve, face, tolerance) {
            args = [curve, face, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/pulltobrepface-curve_brepface_double", args);
            return promise;
        },

        planarClosedCurveRelationship : function(curveA, curveB, testPlane, tolerance) {
            args = [curveA, curveB, testPlane, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/planarclosedcurverelationship-curve_curve_plane_double", args);
            return promise;
        },

        planarCurveCollision : function(curveA, curveB, testPlane, tolerance) {
            args = [curveA, curveB, testPlane, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/planarcurvecollision-curve_curve_plane_double", args);
            return promise;
        },

        duplicateSegments : function(curve) {
            args = [curve];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/duplicatesegments-curve", args);
            return promise;
        },

        smooth : function(curve, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem) {
            args = [curve, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/smooth-curve_double_bool_bool_bool_bool_smoothingcoordinatesystem", args);
            return promise;
        },

        smooth1 : function(curve, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem, plane) {
            args = [curve, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem, plane];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/smooth-curve_double_bool_bool_bool_bool_smoothingcoordinatesystem_plane", args);
            return promise;
        },

        makeClosed : function(curve, tolerance) {
            args = [curve, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/makeclosed-curve_double", args);
            return promise;
        },

        lcoalClosestPoint : function(curve, testPoint, seed, t) {
            args = [curve, testPoint, seed, t];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/lcoalclosestpoint-curve_point3d_double_double", args);
            return promise;
        },

        closestPoint : function(curve, testPoint, t) {
            args = [curve, testPoint, t];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/closestpoint-curve_point3d_double", args);
            return promise;
        },

        closestPoint1 : function(curve, testPoint, t, maximumDistance) {
            args = [curve, testPoint, t, maximumDistance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/closestpoint-curve_point3d_double_double", args);
            return promise;
        },

        contains : function(curve, testPoint) {
            args = [curve, testPoint];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/contains-curve_point3d", args);
            return promise;
        },

        contains1 : function(curve, testPoint, plane) {
            args = [curve, testPoint, plane];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/contains-curve_point3d_plane", args);
            return promise;
        },

        contains2 : function(curve, testPoint, plane, tolerance) {
            args = [curve, testPoint, plane, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/contains-curve_point3d_plane_double", args);
            return promise;
        },

        extremeParameters : function(curve, direction) {
            args = [curve, direction];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/extremeparameters-curve_vector3d", args);
            return promise;
        },

        createPeriodicCurve : function(curve) {
            args = [curve];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createperiodiccurve-curve", args);
            return promise;
        },

        createPeriodicCurve1 : function(curve, smooth) {
            args = [curve, smooth];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/createperiodiccurve-curve_bool", args);
            return promise;
        },

        pointAtLength : function(curve, length) {
            args = [curve, length];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/pointatlength-curve_double", args);
            return promise;
        },

        pointAtNormalizedLength : function(curve, length) {
            args = [curve, length];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/pointatnormalizedlength-curve_double", args);
            return promise;
        },

        perpendicularFrameAt : function(curve, t, plane) {
            args = [curve, t, plane];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/perpendicularframeat-curve_double_plane", args);
            return promise;
        },

        getPerpendicularFrames : function(curve, parameters) {
            args = [curve, parameters];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/getperpendicularframes-curve_ienumerable<double>", args);
            return promise;
        },

        getLength : function(curve) {
            args = [curve];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/getlength-curve", args);
            return promise;
        },

        getLength1 : function(curve, fractionalTolerance) {
            args = [curve, fractionalTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/getlength-curve_double", args);
            return promise;
        },

        getLength2 : function(curve, subdomain) {
            args = [curve, subdomain];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/getlength-curve_interval", args);
            return promise;
        },

        getLength3 : function(curve, fractionalTolerance, subdomain) {
            args = [curve, fractionalTolerance, subdomain];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/getlength-curve_double_interval", args);
            return promise;
        },

        isShort : function(curve, tolerance) {
            args = [curve, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/isshort-curve_double", args);
            return promise;
        },

        isShort1 : function(curve, tolerance, subdomain) {
            args = [curve, tolerance, subdomain];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/isshort-curve_double_interval", args);
            return promise;
        },

        removeShortSegments : function(curve, tolerance) {
            args = [curve, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/removeshortsegments-curve_double", args);
            return promise;
        },

        lengthParameter : function(curve, segmentLength, t) {
            args = [curve, segmentLength, t];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/lengthparameter-curve_double_double", args);
            return promise;
        },

        lengthParameter1 : function(curve, segmentLength, t, fractionalTolerance) {
            args = [curve, segmentLength, t, fractionalTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/lengthparameter-curve_double_double_double", args);
            return promise;
        },

        lengthParameter2 : function(curve, segmentLength, t, subdomain) {
            args = [curve, segmentLength, t, subdomain];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/lengthparameter-curve_double_double_interval", args);
            return promise;
        },

        lengthParameter3 : function(curve, segmentLength, t, fractionalTolerance, subdomain) {
            args = [curve, segmentLength, t, fractionalTolerance, subdomain];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/lengthparameter-curve_double_double_double_interval", args);
            return promise;
        },

        normalizedLengthParameter : function(curve, s, t) {
            args = [curve, s, t];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/normalizedlengthparameter-curve_double_double", args);
            return promise;
        },

        normalizedLengthParameter1 : function(curve, s, t, fractionalTolerance) {
            args = [curve, s, t, fractionalTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/normalizedlengthparameter-curve_double_double_double", args);
            return promise;
        },

        normalizedLengthParameter2 : function(curve, s, t, subdomain) {
            args = [curve, s, t, subdomain];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/normalizedlengthparameter-curve_double_double_interval", args);
            return promise;
        },

        normalizedLengthParameter3 : function(curve, s, t, fractionalTolerance, subdomain) {
            args = [curve, s, t, fractionalTolerance, subdomain];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/normalizedlengthparameter-curve_double_double_double_interval", args);
            return promise;
        },

        normalizedLengthParameters : function(curve, s, absoluteTolerance) {
            args = [curve, s, absoluteTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/normalizedlengthparameters-curve_double[]_double", args);
            return promise;
        },

        normalizedLengthParameters1 : function(curve, s, absoluteTolerance, fractionalTolerance) {
            args = [curve, s, absoluteTolerance, fractionalTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/normalizedlengthparameters-curve_double[]_double_double", args);
            return promise;
        },

        normalizedLengthParameters2 : function(curve, s, absoluteTolerance, subdomain) {
            args = [curve, s, absoluteTolerance, subdomain];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/normalizedlengthparameters-curve_double[]_double_interval", args);
            return promise;
        },

        normalizedLengthParameters3 : function(curve, s, absoluteTolerance, fractionalTolerance, subdomain) {
            args = [curve, s, absoluteTolerance, fractionalTolerance, subdomain];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/normalizedlengthparameters-curve_double[]_double_double_interval", args);
            return promise;
        },

        divideByCount : function(curve, segmentCount, includeEnds) {
            args = [curve, segmentCount, includeEnds];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/dividebycount-curve_int_bool", args);
            return promise;
        },

        divideByCount1 : function(curve, segmentCount, includeEnds, points) {
            args = [curve, segmentCount, includeEnds, points];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/dividebycount-curve_int_bool_point3d[]", args);
            return promise;
        },

        divideByLength : function(curve, segmentLength, includeEnds) {
            args = [curve, segmentLength, includeEnds];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/dividebylength-curve_double_bool", args);
            return promise;
        },

        divideByLength1 : function(curve, segmentLength, includeEnds, reverse) {
            args = [curve, segmentLength, includeEnds, reverse];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/dividebylength-curve_double_bool_bool", args);
            return promise;
        },

        divideByLength2 : function(curve, segmentLength, includeEnds, points) {
            args = [curve, segmentLength, includeEnds, points];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/dividebylength-curve_double_bool_point3d[]", args);
            return promise;
        },

        divideByLength3 : function(curve, segmentLength, includeEnds, reverse, points) {
            args = [curve, segmentLength, includeEnds, reverse, points];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/dividebylength-curve_double_bool_bool_point3d[]", args);
            return promise;
        },

        divideEquidistant : function(curve, distance) {
            args = [curve, distance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/divideequidistant-curve_double", args);
            return promise;
        },

        divideAsContour : function(curve, contourStart, contourEnd, interval) {
            args = [curve, contourStart, contourEnd, interval];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/divideascontour-curve_point3d_point3d_double", args);
            return promise;
        },

        trim : function(curve, side, length) {
            args = [curve, side, length];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/trim-curve_curveend_double", args);
            return promise;
        },

        split : function(curve, cutter, tolerance) {
            args = [curve, cutter, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/split-curve_brep_double", args);
            return promise;
        },

        split1 : function(curve, cutter, tolerance, angleToleranceRadians) {
            args = [curve, cutter, tolerance, angleToleranceRadians];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/split-curve_brep_double_double", args);
            return promise;
        },

        split2 : function(curve, cutter, tolerance) {
            args = [curve, cutter, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/split-curve_surface_double", args);
            return promise;
        },

        split3 : function(curve, cutter, tolerance, angleToleranceRadians) {
            args = [curve, cutter, tolerance, angleToleranceRadians];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/split-curve_surface_double_double", args);
            return promise;
        },

        extend : function(curve, t0, t1) {
            args = [curve, t0, t1];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/extend-curve_double_double", args);
            return promise;
        },

        extend1 : function(curve, domain) {
            args = [curve, domain];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/extend-curve_interval", args);
            return promise;
        },

        extend2 : function(curve, side, length, style) {
            args = [curve, side, length, style];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/extend-curve_curveend_double_curveextensionstyle", args);
            return promise;
        },

        extend3 : function(curve, side, style, geometry) {
            args = [curve, side, style, geometry];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/extend-curve_curveend_curveextensionstyle_system.collections.generic.ienumerable<geometrybase>", args);
            return promise;
        },

        extend4 : function(curve, side, style, endPoint) {
            args = [curve, side, style, endPoint];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/extend-curve_curveend_curveextensionstyle_point3d", args);
            return promise;
        },

        extendOnSurface : function(curve, side, surface) {
            args = [curve, side, surface];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/extendonsurface-curve_curveend_surface", args);
            return promise;
        },

        extendOnSurface1 : function(curve, side, face) {
            args = [curve, side, face];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/extendonsurface-curve_curveend_brepface", args);
            return promise;
        },

        extendByLine : function(curve, side, geometry) {
            args = [curve, side, geometry];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/extendbyline-curve_curveend_system.collections.generic.ienumerable<geometrybase>", args);
            return promise;
        },

        extendByArc : function(curve, side, geometry) {
            args = [curve, side, geometry];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/extendbyarc-curve_curveend_system.collections.generic.ienumerable<geometrybase>", args);
            return promise;
        },

        simplify : function(curve, options, distanceTolerance, angleToleranceRadians) {
            args = [curve, options, distanceTolerance, angleToleranceRadians];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/simplify-curve_curvesimplifyoptions_double_double", args);
            return promise;
        },

        simplifyEnd : function(curve, end, options, distanceTolerance, angleToleranceRadians) {
            args = [curve, end, options, distanceTolerance, angleToleranceRadians];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/simplifyend-curve_curveend_curvesimplifyoptions_double_double", args);
            return promise;
        },

        fair : function(curve, distanceTolerance, angleTolerance, clampStart, clampEnd, iterations) {
            args = [curve, distanceTolerance, angleTolerance, clampStart, clampEnd, iterations];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/fair-curve_double_double_int_int_int", args);
            return promise;
        },

        fit : function(curve, degree, fitTolerance, angleTolerance) {
            args = [curve, degree, fitTolerance, angleTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/fit-curve_int_double_double", args);
            return promise;
        },

        rebuild : function(curve, pointCount, degree, preserveTangents) {
            args = [curve, pointCount, degree, preserveTangents];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/rebuild-curve_int_int_bool", args);
            return promise;
        },

        toPolyline : function(curve, mainSegmentCount, subSegmentCount, maxAngleRadians, maxChordLengthRatio, maxAspectRatio, tolerance, minEdgeLength, maxEdgeLength, keepStartPoint) {
            args = [curve, mainSegmentCount, subSegmentCount, maxAngleRadians, maxChordLengthRatio, maxAspectRatio, tolerance, minEdgeLength, maxEdgeLength, keepStartPoint];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/topolyline-curve_int_int_double_double_double_double_double_double_bool", args);
            return promise;
        },

        toPolyline1 : function(curve, mainSegmentCount, subSegmentCount, maxAngleRadians, maxChordLengthRatio, maxAspectRatio, tolerance, minEdgeLength, maxEdgeLength, keepStartPoint, curveDomain) {
            args = [curve, mainSegmentCount, subSegmentCount, maxAngleRadians, maxChordLengthRatio, maxAspectRatio, tolerance, minEdgeLength, maxEdgeLength, keepStartPoint, curveDomain];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/topolyline-curve_int_int_double_double_double_double_double_double_bool_interval", args);
            return promise;
        },

        toPolyline2 : function(curve, tolerance, angleTolerance, minimumLength, maximumLength) {
            args = [curve, tolerance, angleTolerance, minimumLength, maximumLength];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/topolyline-curve_double_double_double_double", args);
            return promise;
        },

        toArcsAndLines : function(curve, tolerance, angleTolerance, minimumLength, maximumLength) {
            args = [curve, tolerance, angleTolerance, minimumLength, maximumLength];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/toarcsandlines-curve_double_double_double_double", args);
            return promise;
        },

        pullToMesh : function(curve, mesh, tolerance) {
            args = [curve, mesh, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/pulltomesh-curve_mesh_double", args);
            return promise;
        },

        offset : function(curve, plane, distance, tolerance, cornerStyle) {
            args = [curve, plane, distance, tolerance, cornerStyle];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/offset-curve_plane_double_double_curveoffsetcornerstyle", args);
            return promise;
        },

        offset1 : function(curve, directionPoint, normal, distance, tolerance, cornerStyle) {
            args = [curve, directionPoint, normal, distance, tolerance, cornerStyle];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/offset-curve_point3d_vector3d_double_double_curveoffsetcornerstyle", args);
            return promise;
        },

        offsetOnSurface : function(curve, face, distance, fittingTolerance) {
            args = [curve, face, distance, fittingTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/offsetonsurface-curve_brepface_double_double", args);
            return promise;
        },

        offsetOnSurface1 : function(curve, face, throughPoint, fittingTolerance) {
            args = [curve, face, throughPoint, fittingTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/offsetonsurface-curve_brepface_point2d_double", args);
            return promise;
        },

        offsetOnSurface2 : function(curve, face, curveParameters, offsetDistances, fittingTolerance) {
            args = [curve, face, curveParameters, offsetDistances, fittingTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/offsetonsurface-curve_brepface_double[]_double[]_double", args);
            return promise;
        },

        offsetOnSurface3 : function(curve, surface, distance, fittingTolerance) {
            args = [curve, surface, distance, fittingTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/offsetonsurface-curve_surface_double_double", args);
            return promise;
        },

        offsetOnSurface4 : function(curve, surface, throughPoint, fittingTolerance) {
            args = [curve, surface, throughPoint, fittingTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/offsetonsurface-curve_surface_point2d_double", args);
            return promise;
        },

        offsetOnSurface5 : function(curve, surface, curveParameters, offsetDistances, fittingTolerance) {
            args = [curve, surface, curveParameters, offsetDistances, fittingTolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/offsetonsurface-curve_surface_double[]_double[]_double", args);
            return promise;
        },

        pullToBrepFace : function(curve, face, tolerance) {
            args = [curve, face, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/pulltobrepface-curve_brepface_double", args);
            return promise;
        },

        offsetNormalToSurface : function(curve, surface, height) {
            args = [curve, surface, height];
            var promise = RhinoCompute.computeFetch("rhino/geometry/curve/offsetnormaltosurface-curve_surface_double", args);
            return promise;
        },
    },

    Mesh : {
        createFromPlane : function(plane, xInterval, yInterval, xCount, yCount) {
            args = [plane, xInterval, yInterval, xCount, yCount];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createfromplane-plane_interval_interval_int_int", args);
            return promise;
        },

        createFromBox : function(box, xCount, yCount, zCount) {
            args = [box, xCount, yCount, zCount];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createfrombox-boundingbox_int_int_int", args);
            return promise;
        },

        createFromBox1 : function(box, xCount, yCount, zCount) {
            args = [box, xCount, yCount, zCount];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createfrombox-box_int_int_int", args);
            return promise;
        },

        createFromBox2 : function(corners, xCount, yCount, zCount) {
            args = [corners, xCount, yCount, zCount];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createfrombox-ienumerable<point3d>_int_int_int", args);
            return promise;
        },

        createFromSphere : function(sphere, xCount, yCount) {
            args = [sphere, xCount, yCount];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createfromsphere-sphere_int_int", args);
            return promise;
        },

        createIcoSphere : function(sphere, subdivisions) {
            args = [sphere, subdivisions];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createicosphere-sphere_int", args);
            return promise;
        },

        createQuadSphere : function(sphere, subdivisions) {
            args = [sphere, subdivisions];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createquadsphere-sphere_int", args);
            return promise;
        },

        createFromCylinder : function(cylinder, vertical, around) {
            args = [cylinder, vertical, around];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createfromcylinder-cylinder_int_int", args);
            return promise;
        },

        createFromCone : function(cone, vertical, around) {
            args = [cone, vertical, around];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createfromcone-cone_int_int", args);
            return promise;
        },

        createFromCone1 : function(cone, vertical, around, solid) {
            args = [cone, vertical, around, solid];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createfromcone-cone_int_int_bool", args);
            return promise;
        },

        createFromPlanarBoundary : function(boundary, parameters) {
            args = [boundary, parameters];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createfromplanarboundary-curve_meshingparameters", args);
            return promise;
        },

        createFromPlanarBoundary1 : function(boundary, parameters, tolerance) {
            args = [boundary, parameters, tolerance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createfromplanarboundary-curve_meshingparameters_double", args);
            return promise;
        },

        createFromClosedPolyline : function(polyline) {
            args = [polyline];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createfromclosedpolyline-polyline", args);
            return promise;
        },

        createFromTessellation : function(points, edges, plane, allowNewVertices) {
            args = [points, edges, plane, allowNewVertices];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createfromtessellation-ienumerable<point3d>_ienumerable<ienumerable<point3d>>_plane_bool", args);
            return promise;
        },

        createFromBrep : function(brep) {
            args = [brep];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createfrombrep-brep", args);
            return promise;
        },

        createFromBrep1 : function(brep, meshingParameters) {
            args = [brep, meshingParameters];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createfrombrep-brep_meshingparameters", args);
            return promise;
        },

        createFromSurface : function(surface) {
            args = [surface];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createfromsurface-surface", args);
            return promise;
        },

        createFromSurface1 : function(surface, meshingParameters) {
            args = [surface, meshingParameters];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createfromsurface-surface_meshingparameters", args);
            return promise;
        },

        createPatch : function(outerBoundary, angleToleranceRadians, pullbackSurface, innerBoundaryCurves, innerBothSideCurves, innerPoints, trimback, divisions) {
            args = [outerBoundary, angleToleranceRadians, pullbackSurface, innerBoundaryCurves, innerBothSideCurves, innerPoints, trimback, divisions];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createpatch-polyline_double_surface_ienumerable<curve>_ienumerable<curve>_ienumerable<point3d>_bool_int", args);
            return promise;
        },

        createBooleanUnion : function(meshes) {
            args = [meshes];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createbooleanunion-ienumerable<mesh>", args);
            return promise;
        },

        createBooleanDifference : function(firstSet, secondSet) {
            args = [firstSet, secondSet];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createbooleandifference-ienumerable<mesh>_ienumerable<mesh>", args);
            return promise;
        },

        createBooleanIntersection : function(firstSet, secondSet) {
            args = [firstSet, secondSet];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createbooleanintersection-ienumerable<mesh>_ienumerable<mesh>", args);
            return promise;
        },

        createBooleanSplit : function(meshesToSplit, meshSplitters) {
            args = [meshesToSplit, meshSplitters];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createbooleansplit-ienumerable<mesh>_ienumerable<mesh>", args);
            return promise;
        },

        createFromCurvePipe : function(curve, radius, segments, accuracy, capType, faceted, intervals) {
            args = [curve, radius, segments, accuracy, capType, faceted, intervals];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/createfromcurvepipe-curve_double_int_int_meshpipecapstyle_bool_ienumerable<interval>", args);
            return promise;
        },

        volume : function(mesh) {
            args = [mesh];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/volume-mesh", args);
            return promise;
        },

        smooth : function(mesh, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem) {
            args = [mesh, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/smooth-mesh_double_bool_bool_bool_bool_smoothingcoordinatesystem", args);
            return promise;
        },

        smooth1 : function(mesh, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem, plane) {
            args = [mesh, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem, plane];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/smooth-mesh_double_bool_bool_bool_bool_smoothingcoordinatesystem_plane", args);
            return promise;
        },

        smooth2 : function(mesh, vertexIndices, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem, plane) {
            args = [mesh, vertexIndices, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem, plane];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/smooth-mesh_ienumerable<int>_double_bool_bool_bool_bool_smoothingcoordinatesystem_plane", args);
            return promise;
        },

        unweld : function(mesh, angleToleranceRadians, modifyNormals) {
            args = [mesh, angleToleranceRadians, modifyNormals];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/unweld-mesh_double_bool", args);
            return promise;
        },

        unweldEdge : function(mesh, edgeIndices, modifyNormals) {
            args = [mesh, edgeIndices, modifyNormals];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/unweldedge-mesh_ienumerable<int>_bool", args);
            return promise;
        },

        weld : function(mesh, angleToleranceRadians) {
            args = [mesh, angleToleranceRadians];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/weld-mesh_double", args);
            return promise;
        },

        rebuildNormals : function(mesh) {
            args = [mesh];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/rebuildnormals-mesh", args);
            return promise;
        },

        extractNonManifoldEdges : function(mesh, selective) {
            args = [mesh, selective];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/extractnonmanifoldedges-mesh_bool", args);
            return promise;
        },

        healNakedEdges : function(mesh, distance) {
            args = [mesh, distance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/healnakededges-mesh_double", args);
            return promise;
        },

        fillHoles : function(mesh) {
            args = [mesh];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/fillholes-mesh", args);
            return promise;
        },

        fileHole : function(mesh, topologyEdgeIndex) {
            args = [mesh, topologyEdgeIndex];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/filehole-mesh_int", args);
            return promise;
        },

        unifyNormals : function(mesh) {
            args = [mesh];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/unifynormals-mesh", args);
            return promise;
        },

        unifyNormals1 : function(mesh, countOnly) {
            args = [mesh, countOnly];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/unifynormals-mesh_bool", args);
            return promise;
        },

        splitDisjointPieces : function(mesh) {
            args = [mesh];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/splitdisjointpieces-mesh", args);
            return promise;
        },

        split : function(mesh, plane) {
            args = [mesh, plane];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/split-mesh_plane", args);
            return promise;
        },

        split1 : function(mesh, mesh) {
            args = [mesh, mesh];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/split-mesh_mesh", args);
            return promise;
        },

        split2 : function(mesh, meshes) {
            args = [mesh, meshes];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/split-mesh_ienumerable<mesh>", args);
            return promise;
        },

        getOutlines : function(mesh, plane) {
            args = [mesh, plane];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/getoutlines-mesh_plane", args);
            return promise;
        },

        getOutlines1 : function(mesh, viewport) {
            args = [mesh, viewport];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/getoutlines-mesh_display.rhinoviewport", args);
            return promise;
        },

        getOutlines2 : function(mesh, viewportInfo, plane) {
            args = [mesh, viewportInfo, plane];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/getoutlines-mesh_viewportinfo_plane", args);
            return promise;
        },

        getNakedEdges : function(mesh) {
            args = [mesh];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/getnakededges-mesh", args);
            return promise;
        },

        explodeAtUnweldedEdges : function(mesh) {
            args = [mesh];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/explodeatunweldededges-mesh", args);
            return promise;
        },

        closestPoint : function(mesh, testPoint) {
            args = [mesh, testPoint];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/closestpoint-mesh_point3d", args);
            return promise;
        },

        closestMeshPoint : function(mesh, testPoint, maximumDistance) {
            args = [mesh, testPoint, maximumDistance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/closestmeshpoint-mesh_point3d_double", args);
            return promise;
        },

        closestPoint : function(mesh, testPoint, pointOnMesh, maximumDistance) {
            args = [mesh, testPoint, pointOnMesh, maximumDistance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/closestpoint-mesh_point3d_point3d_double", args);
            return promise;
        },

        pointAt : function(mesh, meshPoint) {
            args = [mesh, meshPoint];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/pointat-mesh_meshpoint", args);
            return promise;
        },

        pointAt1 : function(mesh, faceIndex, t0, t1, t2, t3) {
            args = [mesh, faceIndex, t0, t1, t2, t3];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/pointat-mesh_int_double_double_double_double", args);
            return promise;
        },

        normalAt : function(mesh, meshPoint) {
            args = [mesh, meshPoint];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/normalat-mesh_meshpoint", args);
            return promise;
        },

        normalAt1 : function(mesh, faceIndex, t0, t1, t2, t3) {
            args = [mesh, faceIndex, t0, t1, t2, t3];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/normalat-mesh_int_double_double_double_double", args);
            return promise;
        },

        colorAt : function(mesh, meshPoint) {
            args = [mesh, meshPoint];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/colorat-mesh_meshpoint", args);
            return promise;
        },

        colorAt1 : function(mesh, faceIndex, t0, t1, t2, t3) {
            args = [mesh, faceIndex, t0, t1, t2, t3];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/colorat-mesh_int_double_double_double_double", args);
            return promise;
        },

        pullPointsToMesh : function(mesh, points) {
            args = [mesh, points];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/pullpointstomesh-mesh_ienumerable<point3d>", args);
            return promise;
        },

        offset : function(mesh, distance) {
            args = [mesh, distance];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/offset-mesh_double", args);
            return promise;
        },

        offset1 : function(mesh, distance, solidify) {
            args = [mesh, distance, solidify];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/offset-mesh_double_bool", args);
            return promise;
        },

        offset2 : function(mesh, distance, solidify, direction) {
            args = [mesh, distance, solidify, direction];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/offset-mesh_double_bool_vector3d", args);
            return promise;
        },

        collapseFacesByEdgeLength : function(mesh, bGreaterThan, edgeLength) {
            args = [mesh, bGreaterThan, edgeLength];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/collapsefacesbyedgelength-mesh_bool_double", args);
            return promise;
        },

        collapseFacesByArea : function(mesh, lessThanArea, greaterThanArea) {
            args = [mesh, lessThanArea, greaterThanArea];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/collapsefacesbyarea-mesh_double_double", args);
            return promise;
        },

        collapseFacesByByAspectRatio : function(mesh, aspectRatio) {
            args = [mesh, aspectRatio];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/collapsefacesbybyaspectratio-mesh_double", args);
            return promise;
        },

        getUnsafeLock : function(mesh, writable) {
            args = [mesh, writable];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/getunsafelock-mesh_bool", args);
            return promise;
        },

        releaseUnsafeLock : function(mesh, meshData) {
            args = [mesh, meshData];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/releaseunsafelock-mesh_meshunsafelock", args);
            return promise;
        },

        withShutLining : function(mesh, faceted, tolerance, curves) {
            args = [mesh, faceted, tolerance, curves];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/withshutlining-mesh_bool_double_ienumerable<shutliningcurveinfo>", args);
            return promise;
        },

        withDisplacement : function(mesh, displacement) {
            args = [mesh, displacement];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/withdisplacement-mesh_meshdisplacementinfo", args);
            return promise;
        },

        withEdgeSoftening : function(mesh, softeningRadius, chamfer, faceted, force, angleThreshold) {
            args = [mesh, softeningRadius, chamfer, faceted, force, angleThreshold];
            var promise = RhinoCompute.computeFetch("rhino/geometry/mesh/withedgesoftening-mesh_double_bool_bool_bool_double", args);
            return promise;
        },
    },
}
