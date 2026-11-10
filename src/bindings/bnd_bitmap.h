// rhino3dm's bitmap API, kept hand-written (Luis, 2026-10-15; api/exceptions.toml
// [[hand_kept]]): RhinoCommon's BitmapEntry has no working member outside Rhino
// (pilot #37), so nothing can be generated for images. This is the vNext (X)
// counterpart of bnd_bitmap.* with the review fixes:
//   - FindId looks the id up as an IMAGE (the old one asked for a material: #35)
//   - Add reports what it did (the new index, or -1) and FileFullPath can be set,
//     so a bitmap can actually be added (the file keys images by path: #38)
//   - iteration walks the file's live images, so it survives a Delete (#24)
//   - FindIndex returns null for a missing index; only `[i]` raises
//   - Bitmap is a ModelComponent: Id, Name, Index, user strings, ...
// The surrounding generated classes are wired in by the generator
// (gen_registry.HAND_BINDINGS / HAND_MEMBERS).
#pragma once

#include "bnd_common_gen.h"
#include "bnd_modelcomponent_gen.h"

class BND_File3dm;

class BND_Bitmap : public BND_ModelComponent
{
public:
  mutable ON_Bitmap* m_ptr = nullptr;  // owned
  ONX_Model* m_model = nullptr;        // or a view: the file ...
  ON_UUID m_id = ON_nil_uuid;          // ... and the image's id
  // ON_Bitmap itself cannot be copied (ON_VIRTUAL_OBJECT_IMPLEMENT: Duplicate is null), so
  // ONX_Model rejects it -- the old Bitmap() could never be added (#38). ON_WindowsBitmapEx
  // is the file-reference bitmap: it writes its path (plain ON_WindowsBitmap writes only
  // pixels, and a pathless image is dropped on read: images are keyed by path)
  BND_Bitmap() : m_ptr(new ON_WindowsBitmapEx()) {}
  explicit BND_Bitmap(ON_Bitmap* owned) : m_ptr(owned) {}
  BND_Bitmap(ONX_Model* model, ON_UUID id) : m_model(model), m_id(id) {}
  ~BND_Bitmap() { delete m_ptr; }
  BND_Bitmap(const BND_Bitmap&) = delete;

  // the file's image (a view whose image left the file throws), or the owned one
  ON_Bitmap* RH3DM_Const() const;
  // a view edits the file's image in place, as the old binding did
  ON_Bitmap* RH3DM_NonConst() const { return RH3DM_Const(); }
  ON_ModelComponent* RH3DM_McPtr() const override { return RH3DM_Const(); }
  ON_ModelComponent* RH3DM_McPtrNC() const override { return RH3DM_NonConst(); }
  bool RH3DM_NoOwnedPtr() const override { return m_ptr == nullptr; }

  // 0 unless the image carries pixels (ON_WindowsBitmap, ON_EmbeddedBitmap); a
  // bitmap that only references a file answers 0 -- its path is FileFullPath
  int Width() const { return RH3DM_Const()->Width(); }
  int Height() const { return RH3DM_Const()->Height(); }
  int BitsPerPixel() const { return RH3DM_Const()->BitsPerPixel(); }
  size_t SizeOfScan() const { return RH3DM_Const()->SizeofScan(); }
  size_t SizeOfImage() const { return RH3DM_Const()->SizeofImage(); }
  std::wstring FileFullPath() const;
  void SetFileFullPath(std::wstring path);
};

class BND_File3dmBitmapTableIter;

// File3dm.Bitmaps: the file's images
class BND_File3dmBitmapTable
{
public:
  ONX_Model* m_model = nullptr;
  explicit BND_File3dmBitmapTable(ONX_Model* model) : m_model(model) {}

  int Count() const;
  BND_Bitmap* Add(const BND_Bitmap& bitmap);     // the file's copy (a view), or null when rejected
  bool Delete(BND_UUID id);
  BND_Bitmap* FindIndex(int index) const;         // the index-th live image; nullptr when there is none
  BND_Bitmap* FindId(BND_UUID id) const;          // nullptr when there is none
  BND_Bitmap* GetItem(int index) const;           // `[index]`: throws when there is none
  BND_File3dmBitmapTableIter* Iter() const;
};

// the live images, in manifest order (ONX_ModelComponentIterator)
class BND_File3dmBitmapTableIter
{
public:
  ONX_Model* m_model = nullptr;
  ONX_ModelComponentIterator m_it;
  bool m_started = false;
  explicit BND_File3dmBitmapTableIter(ONX_Model* model)
    : m_model(model), m_it(*model, ON_ModelComponent::Type::Image) {}
  BND_Bitmap* Next();                             // nullptr at the end
};

// File3dm.Bitmaps (registered with the generated File3dm: HAND_MEMBERS)
BND_File3dmBitmapTable BND_File3dm_Bitmaps(const BND_File3dm& file);

#if defined(ON_PYTHON_COMPILE)
void initBitmapBindings(rh3dmpymodule& m);
#else
void initBitmapBindings(void);
#endif
