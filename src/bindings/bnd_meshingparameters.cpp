#include "bnd_meshingparameters.h"

BND_DICT BND_MeshingParameters_Encode(const BND_MeshingParameters& mp)
{
  BND_DICT d = RH3DM_DictNew();
  RH3DM_DictSet(d, "TextureRange", (int)mp.Gen_TextureRange());
  RH3DM_DictSet(d, "JaggedSeams", mp.Gen_JaggedSeams());
  RH3DM_DictSet(d, "RefineGrid", mp.Gen_RefineGrid());
  RH3DM_DictSet(d, "SimplePlanes", mp.Gen_SimplePlanes());
  RH3DM_DictSet(d, "ComputeCurvature", mp.Gen_ComputeCurvature());
  RH3DM_DictSet(d, "ClosedObjectPostProcess", mp.Gen_ClosedObjectPostProcess());
  RH3DM_DictSet(d, "GridMinCount", mp.Gen_GridMinCount());
  RH3DM_DictSet(d, "GridMaxCount", mp.Gen_GridMaxCount());
  RH3DM_DictSet(d, "GridAngle", mp.Gen_GridAngle());
  RH3DM_DictSet(d, "GridAspectRatio", mp.Gen_GridAspectRatio());
  RH3DM_DictSet(d, "GridAmplification", mp.Gen_GridAmplification());
  RH3DM_DictSet(d, "Tolerance", mp.Gen_Tolerance());
  RH3DM_DictSet(d, "MinimumTolerance", mp.Gen_MinimumTolerance());
  RH3DM_DictSet(d, "RelativeTolerance", mp.Gen_RelativeTolerance());
  RH3DM_DictSet(d, "MinimumEdgeLength", mp.Gen_MinimumEdgeLength());
  RH3DM_DictSet(d, "MaximumEdgeLength", mp.Gen_MaximumEdgeLength());
  RH3DM_DictSet(d, "RefineAngle", mp.Gen_RefineAngle());
  return d;
}

// the old Python Decode read GridMinCount as a bool (17 -> 1: pilot #57); every key is read as its type
BND_MeshingParameters BND_MeshingParameters_Decode(BND_DICT jsonObject)
{
  BND_MeshingParameters mp;
  mp.Gen_set_TextureRange((RH3DM_MeshingParameterTextureRange)RH3DM_DictGet<int>(jsonObject, "TextureRange"));
  mp.Gen_set_JaggedSeams(RH3DM_DictGet<bool>(jsonObject, "JaggedSeams"));
  mp.Gen_set_RefineGrid(RH3DM_DictGet<bool>(jsonObject, "RefineGrid"));
  mp.Gen_set_SimplePlanes(RH3DM_DictGet<bool>(jsonObject, "SimplePlanes"));
  mp.Gen_set_ComputeCurvature(RH3DM_DictGet<bool>(jsonObject, "ComputeCurvature"));
  mp.Gen_set_ClosedObjectPostProcess(RH3DM_DictGet<bool>(jsonObject, "ClosedObjectPostProcess"));
  mp.Gen_set_GridMinCount(RH3DM_DictGet<int>(jsonObject, "GridMinCount"));
  mp.Gen_set_GridMaxCount(RH3DM_DictGet<int>(jsonObject, "GridMaxCount"));
  mp.Gen_set_GridAngle(RH3DM_DictGet<double>(jsonObject, "GridAngle"));
  mp.Gen_set_GridAspectRatio(RH3DM_DictGet<double>(jsonObject, "GridAspectRatio"));
  mp.Gen_set_GridAmplification(RH3DM_DictGet<double>(jsonObject, "GridAmplification"));
  mp.Gen_set_Tolerance(RH3DM_DictGet<double>(jsonObject, "Tolerance"));
  mp.Gen_set_MinimumTolerance(RH3DM_DictGet<double>(jsonObject, "MinimumTolerance"));
  mp.Gen_set_RelativeTolerance(RH3DM_DictGet<double>(jsonObject, "RelativeTolerance"));
  mp.Gen_set_MinimumEdgeLength(RH3DM_DictGet<double>(jsonObject, "MinimumEdgeLength"));
  mp.Gen_set_MaximumEdgeLength(RH3DM_DictGet<double>(jsonObject, "MaximumEdgeLength"));
  mp.Gen_set_RefineAngle(RH3DM_DictGet<double>(jsonObject, "RefineAngle"));
  return mp;
}

BND_DICT BND_MeshingParameters_ToJSON(const BND_MeshingParameters& mp, BND_DICT)
{
  return BND_MeshingParameters_Encode(mp);
}
