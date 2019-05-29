//RHINO_SDK_FUNCTION
//ON_Surface* RhinoExtrudeCurveStraight( const ON_Curve* pCurve, ON_3dVector direction, double scale = 1.0);
//
// IS NOW
// public static Surface Surface::CreateExtrusion(Curve profile, Vector3d direction)


//RHINO_SDK_FUNCTION
//ON_Surface* RhinoExtrudeCurveToPoint( const ON_Curve* pCurve, const ON_3dPoint& apex);
//
// IS NOW
// public static Surface Surface::CreateExtrusionToPoint(Curve profile, Point3d apexPoint)



//RHINO_SDK_FUNCTION
//bool RhinoCreateTaperedExtrude( const ON_Curve* input_curve, double distance, ON_3dVector direction,
//                                ON_3dPoint base, double draft_angle, int cornertype,
//                                ON_SimpleArray<ON_Brep*>& output_breps );
// NOT WRAPPED YET
