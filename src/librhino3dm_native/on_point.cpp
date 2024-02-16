#include "stdafx.h"

unsigned int ARGB_to_ABGR( unsigned int argb )
{
  // ON_Color defines alpha=0 as opaque where System.Drawing.Color defines alpha=255 as opaque
  // This function is for converting from System.Drawing.Color to ON_Color
  unsigned int alpha = (argb & 0xff000000)>>24;
  alpha = (255-alpha);
  unsigned int abgr = (alpha)<<24 | (argb & 0x000000ff)<<16 | (argb & 0x0000ff00) | (argb & 0x00ff0000)>>16;
  return abgr;
}
unsigned int ABGR_to_ARGB( unsigned int abgr )
{
  // ON_Color defines alpha=0 as opaque where System.Drawing.Color defines alpha=255 as opaque
  // This function is for converting from ON_Color to System.Drawing.Color
  unsigned int alpha = (abgr & 0xff000000)>>24;
  alpha = (255-alpha);
  unsigned int argb = (alpha)<<24 | (abgr & 0x000000ff)<<16 | (abgr & 0x0000ff00) | (abgr & 0x00ff0000)>>16;
  return argb;
}

RH_C_FUNCTION void ON_Interval_Intersection( ON_Interval* ptr, ON_INTERVAL_STRUCT a, ON_INTERVAL_STRUCT b )
{
  if( ptr )
  {
    const ON_Interval* _a = (const ON_Interval*)&a;
    const ON_Interval* _b = (const ON_Interval*)&b;
    ptr->Intersection(*_a, *_b);
  }
}

RH_C_FUNCTION void ON_Interval_Union( ON_Interval* ptr, ON_INTERVAL_STRUCT a, ON_INTERVAL_STRUCT b )
{
  if( ptr )
  {
    const ON_Interval* _a = (const ON_Interval*)&a;
    const ON_Interval* _b = (const ON_Interval*)&b;
    ptr->Union(*_a, *_b);
  }
}

////////////////////////////////////////////////////////////////////////////////////

RH_C_FUNCTION double ON_2dVector_Length(ON_2DVECTOR_STRUCT v)
{
  return ((ON_2dVector*)&v)->Length();
}

RH_C_FUNCTION bool ON_2dVector_Unitize( ON_2dVector* v )
{
  bool rc = false;
  if( v )
    rc = v->Unitize();
  return rc;
}

RH_C_FUNCTION bool ON_2dVector_IsTiny(ON_2DVECTOR_STRUCT v, double tinyTolerance)
{
	const ON_2dVector* _v = (const ON_2dVector*)&v;
	return _v->IsTiny(tinyTolerance);
}

RH_C_FUNCTION void ON_2dVector_Rotate(ON_2dVector* v, double angle)
{
  if (v)
    v->Rotate(angle);
}

////////////////////////////////////////////////////////////////////////////////////

RH_C_FUNCTION double ON_2fVector_Length(ON_2FVECTOR_STRUCT v)
{
  return ((ON_2fVector*)&v)->Length();
}

RH_C_FUNCTION bool ON_2fVector_Unitize(ON_2fVector* v)
{
  bool rc = false;
  if (v)
    rc = v->Unitize();
  return rc;
}

RH_C_FUNCTION bool ON_2fVector_IsTiny(ON_2FVECTOR_STRUCT v, double tinyTolerance)
{
  const ON_2fVector* _v = (const ON_2fVector*)&v;
  return _v->IsTiny(tinyTolerance);
}

RH_C_FUNCTION bool ON_2fVector_PerpendicularTo(ON_2fVector* v, ON_2FVECTOR_STRUCT other)
{
  bool rc = false;
  if (v)
  {
    const ON_2fVector* _other = (const ON_2fVector*)&other;
    rc = v->PerpendicularTo(*_other);
  }
  return rc;
}

////////////////////////////////////////////////////////////////////////////////////

RH_C_FUNCTION bool ON_3fVector_Unitize( ON_3fVector* v )
{
  bool rc = false;
  if( v )
    rc = v->Unitize();
  return rc;
}

RH_C_FUNCTION bool ON_3fVector_PerpendicularTo( ON_3fVector* v, ON_3FVECTOR_STRUCT other )
{
  bool rc = false;
  if( v )
  {
    const ON_3fVector* _other = (const ON_3fVector*)&other;
    rc = v->PerpendicularTo(*_other);
  }
  return rc;
}

RH_C_FUNCTION void ON_3fVector_Rotate(ON_3fVector* v, double angle, ON_3FVECTOR_STRUCT axis)
{
  if (v)
  {
    const ON_3fVector* _axis = (const ON_3fVector*)&axis;
    v->Rotate(angle, *_axis);
  }
}

////////////////////////////////////////////////////////////////////////////////////

RH_C_FUNCTION bool ON_3dVector_Unitize( ON_3dVector* v )
{
  bool rc = false;
  if( v )
    rc = v->Unitize();
  return rc;
}

RH_C_FUNCTION int ON_3dVector_IsParallelTo( ON_3DVECTOR_STRUCT v0, ON_3DVECTOR_STRUCT v1, double angleTol)
{
  const ON_3dVector* _v0 = (const ON_3dVector*)&v0;
  const ON_3dVector* _v1 = (const ON_3dVector*)&v1;
  int rc = _v0->IsParallelTo(*_v1, angleTol);
  return rc;
}

RH_C_FUNCTION bool ON_3dVector_IsTiny( ON_3DVECTOR_STRUCT v, double tinyTolerance)
{
  const ON_3dVector* _v = (const ON_3dVector*)&v;
  return _v->IsTiny(tinyTolerance);
}

RH_C_FUNCTION void ON_3dVector_Rotate( ON_3dVector* v, double angle, ON_3DVECTOR_STRUCT axis )
{
  if( v )
  {
    const ON_3dVector* _axis = (const ON_3dVector*)&axis;
    v->Rotate(angle, *_axis);
  }
}

RH_C_FUNCTION bool ON_3dVector_PerpendicularTo( ON_3dVector* v, ON_3DVECTOR_STRUCT other )
{
  bool rc = false;
  if( v )
  {
    const ON_3dVector* _other = (const ON_3dVector*)&other;
    rc = v->PerpendicularTo(*_other);
  }
  return rc;
}

RH_C_FUNCTION bool ON_3dVector_PerpendicularTo2(ON_3dVector* v, ON_3DPOINT_STRUCT p0, ON_3DPOINT_STRUCT p1, ON_3DPOINT_STRUCT p2)
{
  bool rc = false;
  if (v)
  {
    ON_3dPoint P0(p0.val);
    ON_3dPoint P1(p1.val);
    ON_3dPoint P2(p2.val);
    rc = v->PerpendicularTo(P0, P1, P2);
  }
  return rc;
}

RH_C_FUNCTION int ONC_ComparePoint(int dim, bool is_rat, ON_3DPOINT_STRUCT a, ON_3DPOINT_STRUCT b)
{
  const ON_3dPoint* _a = (const ON_3dPoint*)&a;
  const ON_3dPoint* _b = (const ON_3dPoint*)&b;
  int rat = is_rat?1:0;
  return ON_ComparePoint(dim, rat, &(_a->x), &(_b->x));
}

// needs to get moved to on_line.cpp
RH_C_FUNCTION bool ON_Line_ClosestPointTo( ON_3DPOINT_STRUCT testPoint, ON_3DPOINT_STRUCT from, ON_3DPOINT_STRUCT to, double* t)
{
  const ON_3dPoint* _from = (const ON_3dPoint*)&from;
  const ON_3dPoint* _to = (const ON_3dPoint*)&to;
  ON_Line line(*_from, *_to);
  const ON_3dPoint* _testPoint = (const ON_3dPoint*)&testPoint;
  bool rc = line.ClosestPointTo(*_testPoint, t);
  return rc;
}

RH_C_FUNCTION bool ON_4dPoint_Equality( ON_4DPOINT_STRUCT a, ON_4DPOINT_STRUCT b )
{
  // True if all 4 coordinates are identical ( 0 == ON_4dPoint::CompareDictionary() ).
  // See difference between ON_4dPoint::CompareProjective() and ON_4dPoint::CompareDictionary()
  return ( ON_4dPoint(a.val) == ON_4dPoint(b.val) );
}

RH_C_FUNCTION bool ON_4dPoint_Normalize( ON_4dPoint* a )
{
  bool rc = false;
  if( a )
    rc = a->Normalize();
  return rc;
}
