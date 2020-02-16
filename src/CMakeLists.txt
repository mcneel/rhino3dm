cmake_minimum_required(VERSION 3.10)
project(rhino3dm)

# 24 Jan 2020 S. Baer
# When I added draco to this project we ended up in a situation where both
# EMSCRIPTEN and PYTHON_EXECUTABLE were defined which is what we were keying
# off of to figure out what type of project we should be building. Adding
# some local cmake varaibles to work around this issue. It seems like we can
# figure out something cleaner to use in the future as a command line arg
set(RHINO3DM_JS "NO")
set(RHINO3DM_PY "NO")
if (EMSCRIPTEN)
  set(RHINO3DM_JS "YES")
else()
  set(RHINO3DM_PY "YES")
endif()

# 24 Jan 2020 S. Baer
# I am having problems with compiling draco support into Python2 on Linux.
# Removing support for draco on that build until we figure out what we are
# doing wrong
set(INCLUDE_DRACO "YES")
if (${RHINO3DM_PY} AND (CMAKE_SYSTEM_NAME STREQUAL "Linux"))
  set(INCLUDE_DRACO "NO")
endif()


if (${RHINO3DM_JS})
  message("Web Assembly Compile")
endif()

if (${RHINO3DM_PY})
  message("Python Compile")
 add_subdirectory(lib/pybind11)
endif()

# Add draco add_library
if (${INCLUDE_DRACO})
  add_subdirectory(lib/draco)
  include_directories(${PROJECT_NAME} PUBLIC lib/draco/src ${CMAKE_BINARY_DIR})
  set(draco_src_root "lib/draco/src/draco")
  set(draco_build_dir "${CMAKE_BINARY_DIR}")

  set(draco_attributes_sources
    "${draco_src_root}/attributes/attribute_octahedron_transform.cc"
    "${draco_src_root}/attributes/attribute_octahedron_transform.h"
    "${draco_src_root}/attributes/attribute_quantization_transform.cc"
    "${draco_src_root}/attributes/attribute_quantization_transform.h"
    "${draco_src_root}/attributes/attribute_transform.cc"
    "${draco_src_root}/attributes/attribute_transform.h"
    "${draco_src_root}/attributes/attribute_transform_data.h"
    "${draco_src_root}/attributes/attribute_transform_type.h"
    "${draco_src_root}/attributes/geometry_attribute.cc"
    "${draco_src_root}/attributes/geometry_attribute.h"
    "${draco_src_root}/attributes/geometry_indices.h"
    "${draco_src_root}/attributes/point_attribute.cc"
    "${draco_src_root}/attributes/point_attribute.h")
set(
  draco_compression_attributes_dec_sources
  "${draco_src_root}/compression/attributes/attributes_decoder.cc"
  "${draco_src_root}/compression/attributes/attributes_decoder.h"
  "${draco_src_root}/compression/attributes/kd_tree_attributes_decoder.cc"
  "${draco_src_root}/compression/attributes/kd_tree_attributes_decoder.h"
  "${draco_src_root}/compression/attributes/kd_tree_attributes_shared.h"
  "${draco_src_root}/compression/attributes/mesh_attribute_indices_encoding_data.h"
  "${draco_src_root}/compression/attributes/normal_compression_utils.h"
  "${draco_src_root}/compression/attributes/point_d_vector.h"
  "${draco_src_root}/compression/attributes/sequential_attribute_decoder.cc"
  "${draco_src_root}/compression/attributes/sequential_attribute_decoder.h"
  "${draco_src_root}/compression/attributes/sequential_attribute_decoders_controller.cc"
  "${draco_src_root}/compression/attributes/sequential_attribute_decoders_controller.h"
  "${draco_src_root}/compression/attributes/sequential_integer_attribute_decoder.cc"
  "${draco_src_root}/compression/attributes/sequential_integer_attribute_decoder.h"
  "${draco_src_root}/compression/attributes/sequential_normal_attribute_decoder.cc"
  "${draco_src_root}/compression/attributes/sequential_normal_attribute_decoder.h"
  "${draco_src_root}/compression/attributes/sequential_quantization_attribute_decoder.cc"
  "${draco_src_root}/compression/attributes/sequential_quantization_attribute_decoder.h"
  )

set(
  draco_compression_attributes_enc_sources
  "${draco_src_root}/compression/attributes/attributes_encoder.cc"
  "${draco_src_root}/compression/attributes/attributes_encoder.h"
  "${draco_src_root}/compression/attributes/kd_tree_attributes_encoder.cc"
  "${draco_src_root}/compression/attributes/kd_tree_attributes_encoder.h"
  "${draco_src_root}/compression/attributes/linear_sequencer.h"
  "${draco_src_root}/compression/attributes/points_sequencer.h"
  "${draco_src_root}/compression/attributes/sequential_attribute_encoder.cc"
  "${draco_src_root}/compression/attributes/sequential_attribute_encoder.h"
  "${draco_src_root}/compression/attributes/sequential_attribute_encoders_controller.cc"
  "${draco_src_root}/compression/attributes/sequential_attribute_encoders_controller.h"
  "${draco_src_root}/compression/attributes/sequential_integer_attribute_encoder.cc"
  "${draco_src_root}/compression/attributes/sequential_integer_attribute_encoder.h"
  "${draco_src_root}/compression/attributes/sequential_normal_attribute_encoder.cc"
  "${draco_src_root}/compression/attributes/sequential_normal_attribute_encoder.h"
  "${draco_src_root}/compression/attributes/sequential_quantization_attribute_encoder.cc"
  "${draco_src_root}/compression/attributes/sequential_quantization_attribute_encoder.h"
  )

set(
  draco_compression_attributes_pred_schemes_dec_sources
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_constrained_multi_parallelogram_decoder.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_constrained_multi_parallelogram_shared.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_data.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_decoder.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_geometric_normal_decoder.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_geometric_normal_predictor_area.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_geometric_normal_predictor_base.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_multi_parallelogram_decoder.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_parallelogram_encoder.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_parallelogram_shared.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_tex_coords_decoder.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_tex_coords_portable_decoder.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_tex_coords_portable_predictor.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_decoder.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_decoder_factory.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_decoder_interface.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_decoding_transform.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_delta_decoder.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_factory.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_interface.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_normal_octahedron_canonicalized_decoding_transform.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_normal_octahedron_canonicalized_transform_base.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_normal_octahedron_decoding_transform.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_normal_octahedron_transform_base.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_wrap_decoding_transform.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_wrap_transform_base.h"
  )

set(
  draco_compression_attributes_pred_schemes_enc_sources
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_constrained_multi_parallelogram_encoder.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_constrained_multi_parallelogram_shared.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_data.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_encoder.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_geometric_normal_encoder.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_geometric_normal_predictor_area.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_geometric_normal_predictor_base.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_multi_parallelogram_encoder.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_parallelogram_encoder.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_parallelogram_shared.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_tex_coords_encoder.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_tex_coords_portable_encoder.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/mesh_prediction_scheme_tex_coords_portable_predictor.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_delta_encoder.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_encoder.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_encoder_factory.cc"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_encoder_factory.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_encoder_interface.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_encoding_transform.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_factory.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_interface.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_normal_octahedron_canonicalized_encoding_transform.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_normal_octahedron_canonicalized_transform_base.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_normal_octahedron_encoding_transform.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_normal_octahedron_transform_base.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_wrap_encoding_transform.h"
  "${draco_src_root}/compression/attributes/prediction_schemes/prediction_scheme_wrap_transform_base.h"
  )

set(draco_compression_bit_coders_sources
    "${draco_src_root}/compression/bit_coders/adaptive_rans_bit_coding_shared.h"
    "${draco_src_root}/compression/bit_coders/adaptive_rans_bit_decoder.cc"
    "${draco_src_root}/compression/bit_coders/adaptive_rans_bit_decoder.h"
    "${draco_src_root}/compression/bit_coders/adaptive_rans_bit_encoder.cc"
    "${draco_src_root}/compression/bit_coders/adaptive_rans_bit_encoder.h"
    "${draco_src_root}/compression/bit_coders/direct_bit_decoder.cc"
    "${draco_src_root}/compression/bit_coders/direct_bit_decoder.h"
    "${draco_src_root}/compression/bit_coders/direct_bit_encoder.cc"
    "${draco_src_root}/compression/bit_coders/direct_bit_encoder.h"
    "${draco_src_root}/compression/bit_coders/folded_integer_bit_decoder.h"
    "${draco_src_root}/compression/bit_coders/folded_integer_bit_encoder.h"
    "${draco_src_root}/compression/bit_coders/rans_bit_decoder.cc"
    "${draco_src_root}/compression/bit_coders/rans_bit_decoder.h"
    "${draco_src_root}/compression/bit_coders/rans_bit_encoder.cc"
    "${draco_src_root}/compression/bit_coders/rans_bit_encoder.h"
    "${draco_src_root}/compression/bit_coders/symbol_bit_decoder.cc"
    "${draco_src_root}/compression/bit_coders/symbol_bit_decoder.h"
    "${draco_src_root}/compression/bit_coders/symbol_bit_encoder.cc"
    "${draco_src_root}/compression/bit_coders/symbol_bit_encoder.h")

set(draco_enc_config_sources
    "${draco_src_root}/compression/config/compression_shared.h"
    "${draco_src_root}/compression/config/draco_options.h"
    "${draco_src_root}/compression/config/encoder_options.h"
    "${draco_src_root}/compression/config/encoding_features.h")

set(draco_dec_config_sources
    "${draco_src_root}/compression/config/compression_shared.h"
    "${draco_src_root}/compression/config/decoder_options.h"
    "${draco_src_root}/compression/config/draco_options.h")

set(draco_compression_decode_sources "${draco_src_root}/compression/decode.cc"
                                     "${draco_src_root}/compression/decode.h")

set(draco_compression_encode_sources
    "${draco_src_root}/compression/encode.cc"
    "${draco_src_root}/compression/encode.h"
    "${draco_src_root}/compression/encode_base.h"
    "${draco_src_root}/compression/expert_encode.cc"
    "${draco_src_root}/compression/expert_encode.h")

set(
  draco_compression_mesh_traverser_sources
  "${draco_src_root}/compression/mesh/traverser/depth_first_traverser.h"
  "${draco_src_root}/compression/mesh/traverser/max_prediction_degree_traverser.h"
  "${draco_src_root}/compression/mesh/traverser/mesh_attribute_indices_encoding_observer.h"
  "${draco_src_root}/compression/mesh/traverser/mesh_traversal_sequencer.h"
  "${draco_src_root}/compression/mesh/traverser/traverser_base.h")

set(
  draco_compression_mesh_dec_sources
  "${draco_src_root}/compression/mesh/mesh_decoder.cc"
  "${draco_src_root}/compression/mesh/mesh_decoder.h"
  "${draco_src_root}/compression/mesh/mesh_decoder_helpers.h"
  "${draco_src_root}/compression/mesh/mesh_edgebreaker_decoder.cc"
  "${draco_src_root}/compression/mesh/mesh_edgebreaker_decoder.h"
  "${draco_src_root}/compression/mesh/mesh_edgebreaker_decoder_impl.cc"
  "${draco_src_root}/compression/mesh/mesh_edgebreaker_decoder_impl.h"
  "${draco_src_root}/compression/mesh/mesh_edgebreaker_decoder_impl_interface.h"
  "${draco_src_root}/compression/mesh/mesh_edgebreaker_shared.h"
  "${draco_src_root}/compression/mesh/mesh_edgebreaker_traversal_decoder.h"
  "${draco_src_root}/compression/mesh/mesh_edgebreaker_traversal_predictive_decoder.h"
  "${draco_src_root}/compression/mesh/mesh_edgebreaker_traversal_valence_decoder.h"
  "${draco_src_root}/compression/mesh/mesh_sequential_decoder.cc"
  "${draco_src_root}/compression/mesh/mesh_sequential_decoder.h")

set(
  draco_compression_mesh_enc_sources
  "${draco_src_root}/compression/mesh/mesh_edgebreaker_encoder.cc"
  "${draco_src_root}/compression/mesh/mesh_edgebreaker_encoder.h"
  "${draco_src_root}/compression/mesh/mesh_edgebreaker_encoder_impl.cc"
  "${draco_src_root}/compression/mesh/mesh_edgebreaker_encoder_impl.h"
  "${draco_src_root}/compression/mesh/mesh_edgebreaker_encoder_impl_interface.h"
  "${draco_src_root}/compression/mesh/mesh_edgebreaker_shared.h"
  "${draco_src_root}/compression/mesh/mesh_edgebreaker_traversal_encoder.h"
  "${draco_src_root}/compression/mesh/mesh_edgebreaker_traversal_predictive_encoder.h"
  "${draco_src_root}/compression/mesh/mesh_edgebreaker_traversal_valence_encoder.h"
  "${draco_src_root}/compression/mesh/mesh_encoder.cc"
  "${draco_src_root}/compression/mesh/mesh_encoder.h"
  "${draco_src_root}/compression/mesh/mesh_encoder_helpers.h"
  "${draco_src_root}/compression/mesh/mesh_sequential_encoder.cc"
  "${draco_src_root}/compression/mesh/mesh_sequential_encoder.h")

set(
  draco_compression_point_cloud_dec_sources
  "${draco_src_root}/compression/point_cloud/point_cloud_decoder.cc"
  "${draco_src_root}/compression/point_cloud/point_cloud_decoder.h"
  "${draco_src_root}/compression/point_cloud/point_cloud_kd_tree_decoder.cc"
  "${draco_src_root}/compression/point_cloud/point_cloud_kd_tree_decoder.h"
  "${draco_src_root}/compression/point_cloud/point_cloud_sequential_decoder.cc"
  "${draco_src_root}/compression/point_cloud/point_cloud_sequential_decoder.h")

set(
  draco_compression_point_cloud_enc_sources
  "${draco_src_root}/compression/point_cloud/point_cloud_encoder.cc"
  "${draco_src_root}/compression/point_cloud/point_cloud_encoder.h"
  "${draco_src_root}/compression/point_cloud/point_cloud_kd_tree_encoder.cc"
  "${draco_src_root}/compression/point_cloud/point_cloud_kd_tree_encoder.h"
  "${draco_src_root}/compression/point_cloud/point_cloud_sequential_encoder.cc"
  "${draco_src_root}/compression/point_cloud/point_cloud_sequential_encoder.h")

set(draco_compression_entropy_sources
    "${draco_src_root}/compression/entropy/ans.h"
    "${draco_src_root}/compression/entropy/rans_symbol_coding.h"
    "${draco_src_root}/compression/entropy/rans_symbol_decoder.h"
    "${draco_src_root}/compression/entropy/rans_symbol_encoder.h"
    "${draco_src_root}/compression/entropy/shannon_entropy.cc"
    "${draco_src_root}/compression/entropy/shannon_entropy.h"
    "${draco_src_root}/compression/entropy/symbol_decoding.cc"
    "${draco_src_root}/compression/entropy/symbol_decoding.h"
    "${draco_src_root}/compression/entropy/symbol_encoding.cc"
    "${draco_src_root}/compression/entropy/symbol_encoding.h")

set(draco_core_sources
    "${draco_src_root}/core/bit_utils.cc"
    "${draco_src_root}/core/bit_utils.h"
    "${draco_src_root}/core/bounding_box.cc"
    "${draco_src_root}/core/bounding_box.h"
    "${draco_src_root}/core/cycle_timer.cc"
    "${draco_src_root}/core/cycle_timer.h"
    "${draco_src_root}/core/data_buffer.cc"
    "${draco_src_root}/core/data_buffer.h"
    "${draco_src_root}/core/decoder_buffer.cc"
    "${draco_src_root}/core/decoder_buffer.h"
    "${draco_src_root}/core/divide.cc"
    "${draco_src_root}/core/divide.h"
    "${draco_src_root}/core/draco_index_type.h"
    "${draco_src_root}/core/draco_index_type_vector.h"
    "${draco_src_root}/core/draco_types.cc"
    "${draco_src_root}/core/draco_types.h"
    "${draco_src_root}/core/encoder_buffer.cc"
    "${draco_src_root}/core/encoder_buffer.h"
    "${draco_src_root}/core/hash_utils.cc"
    "${draco_src_root}/core/hash_utils.h"
    "${draco_src_root}/core/macros.h"
    "${draco_src_root}/core/math_utils.h"
    "${draco_src_root}/core/options.cc"
    "${draco_src_root}/core/options.h"
    "${draco_src_root}/core/quantization_utils.cc"
    "${draco_src_root}/core/quantization_utils.h"
    "${draco_src_root}/core/status.h"
    "${draco_src_root}/core/status_or.h"
    "${draco_src_root}/core/varint_decoding.h"
    "${draco_src_root}/core/varint_encoding.h"
    "${draco_src_root}/core/vector_d.h")

set(draco_io_sources
    "${draco_src_root}/io/file_utils.cc"
    "${draco_src_root}/io/file_utils.h"
    "${draco_src_root}/io/mesh_io.cc"
    "${draco_src_root}/io/mesh_io.h"
    "${draco_src_root}/io/obj_decoder.cc"
    "${draco_src_root}/io/obj_decoder.h"
    "${draco_src_root}/io/obj_encoder.cc"
    "${draco_src_root}/io/obj_encoder.h"
    "${draco_src_root}/io/parser_utils.cc"
    "${draco_src_root}/io/parser_utils.h"
    "${draco_src_root}/io/ply_decoder.cc"
    "${draco_src_root}/io/ply_decoder.h"
    "${draco_src_root}/io/ply_encoder.cc"
    "${draco_src_root}/io/ply_encoder.h"
    "${draco_src_root}/io/ply_property_reader.h"
    "${draco_src_root}/io/ply_property_writer.h"
    "${draco_src_root}/io/ply_reader.cc"
    "${draco_src_root}/io/ply_reader.h"
    "${draco_src_root}/io/point_cloud_io.cc"
    "${draco_src_root}/io/point_cloud_io.h")

set(draco_mesh_sources
    "${draco_src_root}/mesh/corner_table.cc"
    "${draco_src_root}/mesh/corner_table.h"
    "${draco_src_root}/mesh/corner_table_iterators.h"
    "${draco_src_root}/mesh/mesh.cc"
    "${draco_src_root}/mesh/mesh.h"
    "${draco_src_root}/mesh/mesh_are_equivalent.cc"
    "${draco_src_root}/mesh/mesh_are_equivalent.h"
    "${draco_src_root}/mesh/mesh_attribute_corner_table.cc"
    "${draco_src_root}/mesh/mesh_attribute_corner_table.h"
    "${draco_src_root}/mesh/mesh_cleanup.cc"
    "${draco_src_root}/mesh/mesh_cleanup.h"
    "${draco_src_root}/mesh/mesh_misc_functions.cc"
    "${draco_src_root}/mesh/mesh_misc_functions.h"
    "${draco_src_root}/mesh/mesh_stripifier.cc"
    "${draco_src_root}/mesh/mesh_stripifier.h"
    "${draco_src_root}/mesh/triangle_soup_mesh_builder.cc"
    "${draco_src_root}/mesh/triangle_soup_mesh_builder.h"
    "${draco_src_root}/mesh/valence_cache.h")

set(draco_point_cloud_sources
    "${draco_src_root}/point_cloud/point_cloud.cc"
    "${draco_src_root}/point_cloud/point_cloud.h"
    "${draco_src_root}/point_cloud/point_cloud_builder.cc"
    "${draco_src_root}/point_cloud/point_cloud_builder.h")

set(
  draco_points_common_sources
  "${draco_src_root}/compression/point_cloud/algorithms/point_cloud_compression_method.h"
  "${draco_src_root}/compression/point_cloud/algorithms/point_cloud_types.h"
  "${draco_src_root}/compression/point_cloud/algorithms/quantize_points_3.h"
  "${draco_src_root}/compression/point_cloud/algorithms/queuing_policy.h")

set(
  draco_points_dec_sources
  "${draco_src_root}/compression/point_cloud/algorithms/dynamic_integer_points_kd_tree_decoder.cc"
  "${draco_src_root}/compression/point_cloud/algorithms/dynamic_integer_points_kd_tree_decoder.h"
  "${draco_src_root}/compression/point_cloud/algorithms/float_points_tree_decoder.cc"
  "${draco_src_root}/compression/point_cloud/algorithms/float_points_tree_decoder.h"
  )

set(
  draco_points_enc_sources
  "${draco_src_root}/compression/point_cloud/algorithms/dynamic_integer_points_kd_tree_encoder.cc"
  "${draco_src_root}/compression/point_cloud/algorithms/dynamic_integer_points_kd_tree_encoder.h"
  "${draco_src_root}/compression/point_cloud/algorithms/float_points_tree_encoder.cc"
  "${draco_src_root}/compression/point_cloud/algorithms/float_points_tree_encoder.h"
  )

set(draco_metadata_sources "${draco_src_root}/metadata/geometry_metadata.cc"
                           "${draco_src_root}/metadata/geometry_metadata.h"
                           "${draco_src_root}/metadata/metadata.cc"
                           "${draco_src_root}/metadata/metadata.h")

set(draco_metadata_enc_sources "${draco_src_root}/metadata/metadata_encoder.cc"
                               "${draco_src_root}/metadata/metadata_encoder.h")

set(draco_metadata_dec_sources "${draco_src_root}/metadata/metadata_decoder.cc"
                               "${draco_src_root}/metadata/metadata_decoder.h")

set(draco_animation_sources "${draco_src_root}/animation/keyframe_animation.cc"
                            "${draco_src_root}/animation/keyframe_animation.h")

set(draco_animation_enc_sources
    "${draco_src_root}/animation/keyframe_animation_encoder.cc"
    "${draco_src_root}/animation/keyframe_animation_encoder.h")

set(draco_animation_dec_sources
    "${draco_src_root}/animation/keyframe_animation_decoder.cc"
    "${draco_src_root}/animation/keyframe_animation_decoder.h")

set(draco_js_dec_sources
    "${draco_src_root}/javascript/emscripten/decoder_webidl_wrapper.cc"
    "${draco_src_root}/javascript/emscripten/draco_decoder_glue_wrapper.cc")

set(draco_js_enc_sources
    "${draco_src_root}/javascript/emscripten/draco_encoder_glue_wrapper.cc"
    "${draco_src_root}/javascript/emscripten/encoder_webidl_wrapper.cc")

set(
  draco_animation_js_dec_sources
  "${draco_src_root}/javascript/emscripten/animation_decoder_webidl_wrapper.cc"
  "${draco_src_root}/javascript/emscripten/draco_animation_decoder_glue_wrapper.cc"
  )

set(
  draco_animation_js_enc_sources
  "${draco_src_root}/javascript/emscripten/animation_encoder_webidl_wrapper.cc"
  "${draco_src_root}/javascript/emscripten/draco_animation_encoder_glue_wrapper.cc"
  )

set(draco_version_sources "${draco_build_dir}/draco_version.cc"
                          "${draco_build_dir}/draco_version.h")

  set(draco_decoder_src
      ${draco_attributes_sources}
      ${draco_compression_attributes_dec_sources}
      ${draco_compression_attributes_pred_schemes_dec_sources}
      ${draco_compression_bit_coders_sources}
      ${draco_compression_decode_sources}
      ${draco_compression_entropy_sources}
      ${draco_compression_mesh_traverser_sources}
      ${draco_compression_mesh_dec_sources}
      ${draco_compression_point_cloud_dec_sources}
      ${draco_core_sources}
      ${draco_dec_config_sources}
      ${draco_io_sources}
      ${draco_mesh_sources}
      ${draco_metadata_dec_sources}
      ${draco_metadata_sources}
      ${draco_point_cloud_sources}
      ${draco_points_dec_sources}
      ${draco_version_sources})

  set(draco_encoder_src
      ${draco_attributes_sources}
      ${draco_compression_attributes_enc_sources}
      ${draco_compression_attributes_pred_schemes_enc_sources}
      ${draco_compression_bit_coders_sources}
      ${draco_compression_encode_sources}
      ${draco_compression_entropy_sources}
      ${draco_compression_mesh_traverser_sources}
      ${draco_compression_mesh_enc_sources}
      ${draco_compression_point_cloud_enc_sources}
      ${draco_core_sources}
      ${draco_enc_config_sources}
      ${draco_io_sources}
      ${draco_mesh_sources}
      ${draco_metadata_enc_sources}
      ${draco_metadata_sources}
      ${draco_point_cloud_sources}
      ${draco_points_enc_sources}
      ${draco_version_sources})
endif()

file(GLOB bindings_SRC "bindings/*.h" "bindings/*.cpp")
file(GLOB zlib_SRC "lib/opennurbs/zlib/*.h" "lib/opennurbs/zlib/*.c")

# temporarily rename the 3 cpp files that we don't want to compile on OSX
file(RENAME "lib/opennurbs/opennurbs_gl.cpp" "lib/opennurbs/opennurbs_gl.skip")
file(RENAME "lib/opennurbs/opennurbs_unicode_cp932.cpp" "lib/opennurbs/opennurbs_unicode_cp932.skip")
file(RENAME "lib/opennurbs/opennurbs_unicode_cp949.cpp" "lib/opennurbs/opennurbs_unicode_cp949.skip")
file(GLOB opennurbs_SRC "lib/opennurbs/*.h" "lib/opennurbs/*.cpp")
file(RENAME "lib/opennurbs/opennurbs_gl.skip" "lib/opennurbs/opennurbs_gl.cpp")
file(RENAME "lib/opennurbs/opennurbs_unicode_cp932.skip" "lib/opennurbs/opennurbs_unicode_cp932.cpp")
file(RENAME "lib/opennurbs/opennurbs_unicode_cp949.skip" "lib/opennurbs/opennurbs_unicode_cp949.cpp")

file(RENAME "lib/opennurbs/android_uuid/gen_uuid_nt.c" "lib/opennurbs/android_uuid/gen_uuid_nt.skip")
file(GLOB uuid_SRC "lib/opennurbs/android_uuid/*.h" "lib/opennurbs/android_uuid/*.c")
file(RENAME "lib/opennurbs/android_uuid/gen_uuid_nt.skip" "lib/opennurbs/android_uuid/gen_uuid_nt.c")

set (CMAKE_CXX_STANDARD 14)

if(${RHINO3DM_JS})
  # we will eventually want the following warnging flags on both compiles
  # for now, just WASM
  set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wall -Wno-overloaded-virtual -Wno-switch -Wno-unknown-pragmas -Wno-unused-private-field")
  set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -s ALLOW_MEMORY_GROWTH=1 --bind")
endif()
#set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -O3")


add_definitions(-D_GNU_SOURCE)
add_definitions(-DON_COMPILING_OPENNURBS)
add_definitions(-DOPENNURBS_FREETYPE_INC_)
add_definitions(-DMY_ZCALLOC -DZ_PREFIX)
add_definitions(-DOPENNURBS_ZLIB_LIB_DIR)
add_definitions(-DUNICODE)
if(${RHINO3DM_JS})
  # these shouldn't be necessary in the future as we adjust opennurbs to be
  # emscripten aware
  add_definitions(-DON_32BIT_RUNTIME)
  add_definitions(-DON_LITTLE_ENDIAN)
  add_definitions(-DON_RUNTIME_ANDROID)
endif()
if (${INCLUDE_DRACO})
  add_definitions(-DON_INCLUDE_DRACO)
endif()

if(${RHINO3DM_JS})
  set(CMAKE_EXECUTABLE_SUFFIX ".html")
endif()

if(${RHINO3DM_JS})
  add_executable(rhino3dm ${bindings_SRC} ${zlib_SRC} ${opennurbs_SRC} ${draco_encoder_src} ${draco_decoder_src})
endif()

if (${RHINO3DM_PY})
  if(CMAKE_SYSTEM_NAME STREQUAL "Linux")
    set (CMAKE_CXX_STANDARD 17)
    add_definitions(-DON_RUNTIME_LINUX)
    add_definitions(-DON_CLANG_CONSTRUCTOR_BUG)
    pybind11_add_module(_rhino3dm ${bindings_SRC} ${opennurbs_SRC} ${zlib_SRC} ${uuid_SRC})
    #target_link_libraries(_rhino3dm -luuid)
  else()
    add_library(zlib_static STATIC ${zlib_SRC})
    add_library(opennurbs_static STATIC ${opennurbs_SRC})
    pybind11_add_module(_rhino3dm ${bindings_SRC})
    target_link_libraries(_rhino3dm PRIVATE zlib_static)
    target_link_libraries(_rhino3dm PRIVATE opennurbs_static)
  endif()
  if(${INCLUDE_DRACO})
    target_link_libraries(_rhino3dm PRIVATE draco dracoenc dracodec)
  endif()
endif()


# I plan to mode the following install phase out of this file and into the top
# level python script for generating wasm 
# dan@mcneel.com: this is already in the build.py -p js (this can be deleted as soon
# as the scripts are used in the build process)
if(${RHINO3DM_JS})
  install(DIRECTORY DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/build/artifacts_js")
  install(FILES "build/javascript/rhino3dm.js" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/build/artifacts_js")
  install(FILES "build/javascript/rhino3dm.wasm" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/build/artifacts_js")
  install(FILES "build/javascript/rhino3dm.js" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/../docs/javascript/samples/resources")
  install(FILES "build/javascript/rhino3dm.wasm" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/../docs/javascript/samples/resources")
endif()
