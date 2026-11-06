const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')
const fs = require('fs');

let rhino
beforeEach(async () => {
  rhino = await rhino3dm()
})

//objective: test the shape of the group table get group members method
test('createExtrusion', async () => {

  // read model 
  const model = '../models/groups.3dm'

  const buffer = fs.readFileSync(model)
  const arr = new Uint8Array(buffer)
  const doc = rhino.File3dm.fromByteArray(arr)

  expect(doc !== null).toBe(true)

  // groups -> allGroups
  const members = doc.allGroups.groupMembers(0)

  expect(Array.isArray(members)).toBe(true)
  expect(members[0].constructor.name).toBe('File3dmObject')

})

// a File3dm with groups g0, g1 and points in no group, in g0 and in both: [file, g0, g1, ids]
function groupTableFile(rhino) {
  const f = new rhino.File3dm()
  const g0 = f.allGroups.addGroup()
  const g1 = f.allGroups.addGroup()
  const add = groups => {
    const a = new rhino.ObjectAttributes()
    for (const g of groups) a.addToGroup(g)
    return f.objects.addPoint(new rhino.Point3d(groups.length, 0, 0), a)
  }
  const ids = { none: add([]), g0: add([g0]), both: add([g0, g1]) }
  return [f, g0, g1, ids]
}

test('groupTableGroupMembersListsTheObjectsInAGroup', () => {
  // groupMembers(index) lists the objects in that group, skipping objects in no group, and is empty for an unknown group (D35)
  const [f, g0, g1, ids] = groupTableFile(rhino)
  const m0 = f.allGroups.groupMembers(g0)
  expect(Array.isArray(m0)).toBe(true)
  expect(m0.map(o => String(o.id)).sort()).toEqual([ids.g0, ids.both].map(String).sort())
  expect(f.allGroups.groupMembers(g1).map(o => String(o.id))).toEqual([String(ids.both)])
  expect(f.allGroups.groupMembers(99)).toEqual([])
})

test('groupTableGroupMembersKeepTheFileAlive', async () => {
  // objects returned by groupMembers stay usable after the file is released
  const m0 = (() => {
    const [f, g0] = groupTableFile(rhino)
    return f.allGroups.groupMembers(g0)
  })()
  await helpers.gcSettle()
  expect(m0[0].geometry == null).toBe(false)
})

test('groupTableDeleteByIndexRemovesTheGroup', () => {
  // File3dmGroupTable.delete(index) removes that group and returns nothing, as RhinoCommon's Delete(int) (D14)
  const f = new rhino.File3dm()
  for (let i = 0; i < 3; i++) f.allGroups.addGroup()
  expect(f.allGroups.delete(0)).toBeUndefined()
  expect(f.allGroups.count).toBe(2)
})

test('groupTableDeleteIdReportsWhetherItDeleted', () => {
  // File3dmGroupTable.deleteId(id) returns true when it deleted the group, false for an unknown id (D14)
  const f = new rhino.File3dm()
  for (let i = 0; i < 3; i++) f.allGroups.addGroup()
  expect(f.allGroups.deleteId(f.allGroups.findIndex(1).id)).toBe(true)
  expect(f.allGroups.deleteId(require('crypto').randomUUID())).toBe(false)
  expect(f.allGroups.count).toBe(2)
})
