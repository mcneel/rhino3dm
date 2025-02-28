#include "stdafx.h"

RH_C_FUNCTION void ON_Xform_Scale( ON_Xform* xf, const ON_PLANE_STRUCT* plane, double xFactor, double yFactor, double zFactor )
{
  if( xf && plane )
  {
    ON_Plane temp = FromPlaneStruct(*plane);
    *xf = ON_Xform::ScaleTransformation(temp, xFactor, yFactor, zFactor);
  }
}

RH_C_FUNCTION void ON_Xform_Rotation( ON_Xform* xf, double sinAngle, double cosAngle, ON_3DVECTOR_STRUCT rotationAxis, ON_3DPOINT_STRUCT rotationCenter)
{
  if( xf )
  {
    const ON_3dVector* axis = (const ON_3dVector*)&rotationAxis;
    const ON_3dPoint* center = (const ON_3dPoint*)&rotationCenter;
    xf->Rotation(sinAngle, cosAngle, *axis, *center);
  }
}

RH_C_FUNCTION bool ON_Xform_PlaneToPlane( ON_Xform* xf, const ON_PLANE_STRUCT* plane0, const ON_PLANE_STRUCT* plane1, bool rotation)
{
  bool rc = false;
  if( xf && plane0 && plane1 )
  {
    ON_Plane temp0 = FromPlaneStruct(*plane0);
    ON_Plane temp1 = FromPlaneStruct(*plane1);
    if( rotation )
    {
      xf->Rotation(temp0, temp1);
      rc = true;
    }
    else
    {
      rc = xf->ChangeBasis(temp0, temp1);
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_Xform_Mirror( ON_Xform* xf, ON_3DPOINT_STRUCT pointOnMirrorPlane, ON_3DVECTOR_STRUCT normalToMirrorPlane)
{
  if( xf )
  {
    const ON_3dPoint* _pt = (const ON_3dPoint*)&pointOnMirrorPlane;
    const ON_3dVector* _n = (const ON_3dVector*)&normalToMirrorPlane;
    xf->Mirror(*_pt, *_n);
  }
}

RH_C_FUNCTION bool ON_Xform_ChangeBasis2(
  ON_Xform* xf,
  ON_3DVECTOR_STRUCT x0, 
  ON_3DVECTOR_STRUCT y0, 
  ON_3DVECTOR_STRUCT z0,
  ON_3DVECTOR_STRUCT x1, 
  ON_3DVECTOR_STRUCT y1, 
  ON_3DVECTOR_STRUCT z1
)
{
  bool rc = false;
  if( xf )
  {
    const ON_3dVector* _x0 = (const ON_3dVector*)&x0;
    const ON_3dVector* _y0 = (const ON_3dVector*)&y0;
    const ON_3dVector* _z0 = (const ON_3dVector*)&z0;
    const ON_3dVector* _x1 = (const ON_3dVector*)&x1;
    const ON_3dVector* _y1 = (const ON_3dVector*)&y1;
    const ON_3dVector* _z1 = (const ON_3dVector*)&z1;
    rc = xf->ChangeBasis(*_x0, *_y0, *_z0, *_x1, *_y1, *_z1);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Xform_ChangeBasis3(
  ON_Xform* xf,
  ON_3DPOINT_STRUCT p0,
  ON_3DVECTOR_STRUCT x0,
  ON_3DVECTOR_STRUCT y0,
  ON_3DVECTOR_STRUCT z0,
  ON_3DPOINT_STRUCT p1,
  ON_3DVECTOR_STRUCT x1,
  ON_3DVECTOR_STRUCT y1,
  ON_3DVECTOR_STRUCT z1
)
{
  // 30-Jan-2025 Dale Fugier
  bool rc = false;
  if (xf)
  {
    const ON_3dPoint* _p0 = (const ON_3dPoint*)&p0;
    const ON_3dVector* _x0 = (const ON_3dVector*)&x0;
    const ON_3dVector* _y0 = (const ON_3dVector*)&y0;
    const ON_3dVector* _z0 = (const ON_3dVector*)&z0;
    const ON_3dPoint* _p1 = (const ON_3dPoint*)&p1;
    const ON_3dVector* _x1 = (const ON_3dVector*)&x1;
    const ON_3dVector* _y1 = (const ON_3dVector*)&y1;
    const ON_3dVector* _z1 = (const ON_3dVector*)&z1;
    rc = xf->ChangeBasis(*_p0, *_x0, *_y0, *_z0, *_p1, *_x1, *_y1, *_z1);
  }
  return rc;
}

RH_C_FUNCTION void ON_Xform_PlanarProjection(ON_Xform* xf, const ON_PLANE_STRUCT* plane)
{
  if( xf && plane )
  {
    ON_Plane temp = FromPlaneStruct(*plane);
    xf->PlanarProjection(temp);
  }
}

RH_C_FUNCTION void ON_Xform_Shear(ON_Xform* xf, 
                                  const ON_PLANE_STRUCT* plane, 
                                  ON_3DVECTOR_STRUCT x, 
                                  ON_3DVECTOR_STRUCT y, 
                                  ON_3DVECTOR_STRUCT z)
{
  if( xf && plane )
  {
    ON_Plane t_plane = FromPlaneStruct(*plane);
    const ON_3dVector* _x = (const ON_3dVector*)&x;
    const ON_3dVector* _y = (const ON_3dVector*)&y;
    const ON_3dVector* _z = (const ON_3dVector*)&z;

    *xf = ON_Xform::ShearTransformation(t_plane, *_x, *_y, *_z);
  }
}

RH_C_FUNCTION bool ON_Xform_IsZeroTransformation(const ON_Xform* xf, double tolerance)
{
  bool rc = false;
  if (xf)
  {
    rc = xf->IsZeroTransformation(tolerance);
  }
  return rc;
}


RH_C_FUNCTION int ON_Xform_IsSimilarity(const ON_Xform* xf)
{
  int rc = 0;
  if( xf )
  {
    rc = xf->IsSimilarity();
  }
  return rc;
}

RH_C_FUNCTION int ON_Xform_IsSimilarity2(const ON_Xform* xf, double tolerance)
{
  int rc = 0;
  if (xf)
  {
    rc = xf->IsSimilarity(tolerance);
  }
  return rc;
}

RH_C_FUNCTION int ON_Xform_DecomposeSimilarity(const ON_Xform* xf, ON_3dVector* translation, double* dilation, ON_Xform* rotation, double tolerance)
{
  int rc = 0;
  if (xf && translation && dilation && rotation)
    rc = xf->DecomposeSimilarity(*translation, *dilation, *rotation, tolerance);
  return rc;
}

RH_C_FUNCTION void ON_Xform_DecomposeTextureMapping(const ON_Xform* xf, ON_3dVector* offset, ON_3dVector* repeat, ON_3dVector* rotation)
{
  if (xf && offset && repeat && rotation)
  {
    xf->DecomposeTextureMapping(*offset, *repeat, *rotation);
  }
}

RH_C_FUNCTION void ON_Xform_ComposeTextureMapping(ON_Xform* xf, const ON_3dVector* offset, const ON_3dVector* repeat, const ON_3dVector* rotation)
{
  if (xf && offset && repeat && rotation)
  {
    *xf = ON_Xform::TextureMapping(*offset, *repeat, *rotation);
  }
}

RH_C_FUNCTION int ON_Xform_IsRigid(const ON_Xform* xf, double tolerance)
{
  int rc = 0;
  if (xf)
  {
    rc = xf->IsRigid(tolerance);
  }
  return rc;
}

RH_C_FUNCTION int ON_Xform_DecomposeRigid(const ON_Xform* xf, ON_3dVector* translation, ON_Xform* rotation, double tolerance)
{
  int rc = 0;
  if (xf && translation && rotation)
    rc = xf->DecomposeRigid(*translation, *rotation, tolerance);
  return rc;
}

RH_C_FUNCTION bool ON_Xform_IsAffine(const ON_Xform* xf)
{
  bool rc = false;
  if (xf)
  {
    rc = xf->IsAffine();
  }
  return rc;
}

RH_C_FUNCTION bool ON_Xform_IsLinear(const ON_Xform* xf)
{
  bool rc = false;
  if (xf)
  {
    rc = xf->IsLinear();
  }
  return rc;
}

RH_C_FUNCTION bool ON_Xform_DecomposeAffine(const ON_Xform* xf, ON_3dVector* translation, ON_Xform* linear)
{
  bool rc = false;
  if (xf && translation && linear)
    rc = xf->DecomposeAffine(*translation, *linear);
  return rc;
}

RH_C_FUNCTION bool ON_Xform_DecomposeAffine2(const ON_Xform* xf, ON_Xform* linear, ON_3dVector* translation)
{
  bool rc = false;
  if (xf && translation && linear)
    rc = xf->DecomposeAffine(*linear, *translation);
  return rc;
}

RH_C_FUNCTION bool ON_Xform_DecomposeAffine3(const ON_Xform* xf, ON_3dVector* translation, ON_Xform* rotation, ON_Xform* orthogonal, ON_3dVector* diagonal)
{
  bool rc = false;
  if (xf && translation && rotation && orthogonal && diagonal)
    rc = xf->DecomposeAffine(*translation, *rotation, *orthogonal, *diagonal);
  return rc;
}

RH_C_FUNCTION bool ON_Xform_IsRotation(const ON_Xform* xf)
{
  bool rc = false;
  if (xf)
  {
    rc = xf->IsRotation();
  }
  return rc;
}

RH_C_FUNCTION int ON_Xform_Compare(const ON_Xform* pConstXform0, const ON_Xform* pConstXform1)
{
  int rc = 0;
  if (pConstXform0 && pConstXform1)
    rc = pConstXform0->Compare(*pConstXform1);
  return rc;
}

RH_C_FUNCTION bool ON_Xform_GetQuaternion(const ON_Xform* pConstXform, ON_Quaternion* pQuaternion)
{
  bool rc = false;
  if (pConstXform && pQuaternion)
    rc = pConstXform->GetQuaternion(*pQuaternion);
  return rc;
}

RH_C_FUNCTION void ON_Xform_Affineize(ON_Xform* xf)
{
  if (xf)
  {
    xf->Affineize();
  }
}

RH_C_FUNCTION void ON_Xform_Linearize(ON_Xform* xf)
{
  if (xf)
  {
    xf->Linearize();
  }
}

RH_C_FUNCTION bool ON_Xform_Orthogonalize(ON_Xform* xf, double tolerance)
{
  bool rc = false;
  if (xf)
  {
    rc = xf->Orthogonalize(tolerance);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Xform_DecomposeSymmetric(ON_Xform* xf, ON_Xform* matrix, ON_3dVector* diagonal)
{
  bool rc = false;
  if (xf && matrix && diagonal)
  {
    rc = xf->DecomposeSymmetric(*matrix, *diagonal);
  }
  return rc;
}

RH_C_FUNCTION double ON_Xform_Determinant(const ON_Xform* xf)
{
  double rc = 0.0;
  if( xf )
    rc = xf->Determinant();
  return rc;
}

RH_C_FUNCTION bool ON_Xform_Invert( ON_Xform* xf )
{
  bool rc = false;
  if( xf )
    rc = xf->Invert();
  return rc;
}

RH_C_FUNCTION void ON_Xform_RotationZYX(ON_Xform* xf, double yaw, double pitch, double roll)
{
  if (xf)
    xf->RotationZYX(yaw, pitch, roll);
}

RH_C_FUNCTION bool ON_Xform_GetYawPitchRoll(const ON_Xform* xf, double* yaw, double* pitch, double* roll)
{
  bool rc = false;
  if (xf && yaw && pitch && roll)
    rc = xf->GetYawPitchRoll(*yaw, *pitch, *roll);
  return rc;
}

RH_C_FUNCTION void ON_Xform_RotationZYZ(ON_Xform* xf, double alpha, double beta, double gamma)
{
  if (xf)
    xf->RotationZYZ(alpha, beta, gamma);
}

RH_C_FUNCTION bool ON_Xform_GetEulerZYZ(const ON_Xform* xf, double* alpha, double* beta, double* gamma)
{
  bool rc = false;
  if (xf && alpha && beta && gamma)
    rc = xf->GetEulerZYZ(*alpha, *beta, *gamma);
  return rc;
}

RH_C_FUNCTION unsigned int ON_Xform_GetHashCode(const ON_Xform* xf)
{
  unsigned int rc = 0;
  if (xf)
    rc = xf->CRC32(0);
  return rc;
}

///////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////
#if !defined(RHINO3DM_BUILD)
typedef void (CALLBACK* MORPHPOINTPROC)(ON_3DPOINT_STRUCT point, ON_3dPoint* out_point);

class CCustomSpaceMorph : public ON_SpaceMorph
{
public:
  CCustomSpaceMorph(double tolerance, bool quickpreview, bool preserveStructure, MORPHPOINTPROC callback)
  {
    SetTolerance(tolerance);
    SetQuickPreview(quickpreview);
    SetPreserveStructure(preserveStructure);
    m_callback = callback;
  }

  ON_3dPoint MorphPoint( ON_3dPoint point ) const override
  {
    ON_3dPoint rc = point;
    if( m_callback )
    {
      ON_3DPOINT_STRUCT* _pt = (ON_3DPOINT_STRUCT*)(&point);
      m_callback(*_pt, &rc);
    }
    return rc;
  }

private:
  MORPHPOINTPROC m_callback;
};

// not available in stand alone OpenNURBS build

RH_C_FUNCTION bool ON_SpaceMorph_MorphGeometry(ON_Geometry* pGeometry, double tolerance, bool quickpreview, bool preserveStructure, MORPHPOINTPROC callback)
{
  bool rc = false;
  if( pGeometry && callback )
  {
    CCustomSpaceMorph sm(tolerance, quickpreview, preserveStructure, callback);
    rc = pGeometry->Morph(sm);
  }
  return rc;
}

RH_C_FUNCTION bool ON_SpaceMorph_MorphGeometry2(ON_Geometry* pGeometry, const ON_SpaceMorph* pConstSpaceMorph)
{
  if( pGeometry && pConstSpaceMorph )
    return pGeometry->Morph(*pConstSpaceMorph);
  return false;
}

RH_C_FUNCTION bool ON_SpaceMorph_MorphPlane(ON_PLANE_STRUCT* pPlane, double tolerance, bool quickpreview, bool preserveStructure, MORPHPOINTPROC callback)
{
  bool rc = false;
  if (pPlane && callback)
  {
    CCustomSpaceMorph sm(tolerance, quickpreview, preserveStructure, callback);
    ON_Plane temp = FromPlaneStruct(*pPlane);
    rc = temp.Morph(sm);
    CopyToPlaneStruct(*pPlane, temp);
  }
  return rc;
}

RH_C_FUNCTION bool ON_SpaceMorph_MorphPlane2(ON_PLANE_STRUCT* pPlane, const ON_SpaceMorph* pConstSpaceMorph)
{
  bool rc = false;
  if (pPlane && pConstSpaceMorph)
  {
    ON_Plane temp = FromPlaneStruct(*pPlane);
    rc = temp.Morph(*pConstSpaceMorph);
    CopyToPlaneStruct(*pPlane, temp);
  }
  return rc;
}

RH_C_FUNCTION bool ON_SpaceMorph_GetValues(const ON_SpaceMorph* pConstSpaceMorph, double* tolerance, bool* quickpreview, bool* preserveStructure)
{
  bool rc = false;
  if( pConstSpaceMorph && tolerance && quickpreview && preserveStructure )
  {
    *tolerance = pConstSpaceMorph->Tolerance();
    *quickpreview = pConstSpaceMorph->QuickPreview();
    *preserveStructure = pConstSpaceMorph->PreserveStructure();
    rc = true;
  }
  return rc;
}

RH_C_FUNCTION void ON_SpaceMorph_MorphPoint(const ON_SpaceMorph* pConstSpaceMorph, ON_3dPoint* point)
{
  if( pConstSpaceMorph && point )
  {
    *point = pConstSpaceMorph->MorphPoint(*point);
  }
}
#endif

RH_C_FUNCTION void ON_SpaceMorph_Delete(ON_SpaceMorph* pSpaceMorph)
{
  if( pSpaceMorph )
    delete pSpaceMorph;
}

RH_C_FUNCTION ON_Matrix* ON_Matrix_New(int rows, int cols)
{
  ON_Matrix* rc = new ON_Matrix(rows, cols);
  rc->Zero();
  return rc;
}

RH_C_FUNCTION ON_Matrix* ON_Matrix_New2(const ON_Xform* pXform)
{
  if( pXform )
    return new ON_Matrix(*pXform);
  return nullptr;
}

RH_C_FUNCTION void ON_Matrix_Delete(ON_Matrix* pMatrix)
{
  if( pMatrix )
    delete pMatrix;
}

RH_C_FUNCTION double ON_Matrix_GetValue(const ON_Matrix* pConstMatrix, int row, int column)
{
  if( pConstMatrix )
    return (*pConstMatrix)[row][column];
  return 0;
}

RH_C_FUNCTION void ON_Matrix_SetValue(ON_Matrix* pMatrix, int row, int column, double val)
{
  if( pMatrix )
    (*pMatrix)[row][column] = val;
}

RH_C_FUNCTION void ON_Matrix_Zero(ON_Matrix* pMatrix)
{
  if( pMatrix )
    pMatrix->Zero();
}

RH_C_FUNCTION void ON_Matrix_SetDiagonal(ON_Matrix* pMatrix, double value)
{
  if( pMatrix )
    pMatrix->SetDiagonal(value);
}

RH_C_FUNCTION bool ON_Matrix_Transpose(ON_Matrix* pMatrix)
{
  if(pMatrix)
    return pMatrix->Transpose();
  return false;
}

RH_C_FUNCTION bool ON_Matrix_Swap(ON_Matrix* pMatrix, bool swaprows, int a, int b)
{
  bool rc = false;
  if( pMatrix )
  {
    if( swaprows )
      rc = pMatrix->SwapRows(a,b);
    else
      rc = pMatrix->SwapCols(a,b);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Matrix_Invert(ON_Matrix* pMatrix, double zeroTolerance)
{
  bool rc = false;
  if(pMatrix)
    rc = pMatrix->Invert(zeroTolerance);
  return rc;
}

RH_C_FUNCTION void ON_Matrix_Multiply(ON_Matrix* pMatrixRC, const ON_Matrix* pConstMatrixA, const ON_Matrix* pConstMatrixB)
{
  if( pMatrixRC && pConstMatrixA && pConstMatrixB )
  {
    pMatrixRC->Multiply(*pConstMatrixA, *pConstMatrixB);
  }
}

RH_C_FUNCTION void ON_Matrix_Add(ON_Matrix* pMatrixRC, const ON_Matrix* pConstMatrixA, const ON_Matrix* pConstMatrixB)
{
  if( pMatrixRC && pConstMatrixA && pConstMatrixB )
  {
    pMatrixRC->Add(*pConstMatrixA, *pConstMatrixB);
  }
}

RH_C_FUNCTION void ON_Matrix_Scale(ON_Matrix* pMatrix, double scale)
{
  if( pMatrix )
    pMatrix->Scale(scale);
}

RH_C_FUNCTION int ON_Matrix_RowReduce(ON_Matrix* pMatrix, double zero_tol, double* determinant, double* pivot)
{
  int rc = 0;
  if( pMatrix && determinant && pivot )
    rc = pMatrix->RowReduce(zero_tol, *determinant, *pivot);
  return rc;
}

RH_C_FUNCTION int ON_Matrix_RowReduce2(ON_Matrix* pMatrix, double zero_tol, /*ARRAY*/double* b, double* pivot)
{
  int rc = 0;
  if( pMatrix && b )
    rc = pMatrix->RowReduce(zero_tol, b, pivot);
  return rc;
}

RH_C_FUNCTION int ON_Matrix_RowReduce3(ON_Matrix* pMatrix, double zero_tol, /*ARRAY*/ON_3dPoint* b, double* pivot)
{
  int rc = 0;
  if( pMatrix && b )
    rc = pMatrix->RowReduce(zero_tol, b, pivot);
  return rc;
}

RH_C_FUNCTION bool ON_Matrix_BackSolve(ON_Matrix* pMatrix, double zero_tol, int bSize, /*ARRAY*/const double* b, /*ARRAY*/double* x)
{
  bool rc = false;
  if( pMatrix && b && x )
    rc = pMatrix->BackSolve(zero_tol, bSize, b, x);
  return rc;
}

RH_C_FUNCTION bool ON_Matrix_BackSolve2(ON_Matrix* pMatrix, double zero_tol, int bSize, /*ARRAY*/const ON_3dPoint* b, /*ARRAY*/ON_3dPoint* x)
{
  bool rc = false;
  if( pMatrix && b && x )
    rc = pMatrix->BackSolve(zero_tol, bSize, b, x);
  return rc;
}

RH_C_FUNCTION bool ON_Matrix_GetBool(const ON_Matrix* pConstMatrix, int which)
{
  const int idxIsRowOrthoganal = 0;
  const int idxIsRowOrthoNormal = 1;
  const int idxIsColumnOrthoganal = 2;
  const int idxIsColumnOrthoNormal = 3;
  bool rc = false;

  if( pConstMatrix )
  {
    switch (which)
    {
    case idxIsRowOrthoganal:
      rc = pConstMatrix->IsRowOrthoganal();
      break;
    case idxIsRowOrthoNormal:
      rc = pConstMatrix->IsRowOrthoNormal();
      break;
    case idxIsColumnOrthoganal:
      rc = pConstMatrix->IsColOrthoganal();
      break;
    case idxIsColumnOrthoNormal:
      rc = pConstMatrix->IsColOrthoNormal();
      break;
    default:
      break;
    }
  }
  return rc;
}
