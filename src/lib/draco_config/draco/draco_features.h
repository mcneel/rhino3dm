// rhino3dm: Draco's feature header, which Draco's own CMake generates into its build tree. rhino3dm
// compiles only the encode/decode subset (src/draco_sources.cmake), so it supplies the header itself:
// mesh and point cloud compression, both edgebreakers, backwards compatibility, deduplication. The
// glTF transcoder (DRACO_TRANSCODER_SUPPORTED) is off.

#ifndef DRACO_FEATURES_H_
#define DRACO_FEATURES_H_

#define DRACO_POINT_CLOUD_COMPRESSION_SUPPORTED
#define DRACO_MESH_COMPRESSION_SUPPORTED
#define DRACO_NORMAL_ENCODING_SUPPORTED
#define DRACO_STANDARD_EDGEBREAKER_SUPPORTED
#define DRACO_PREDICTIVE_EDGEBREAKER_SUPPORTED
#define DRACO_BACKWARDS_COMPATIBILITY_SUPPORTED
#define DRACO_ATTRIBUTE_INDICES_DEDUPLICATION_SUPPORTED
#define DRACO_ATTRIBUTE_VALUES_DEDUPLICATION_SUPPORTED

#endif  // DRACO_FEATURES_H_
