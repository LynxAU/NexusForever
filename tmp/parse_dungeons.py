import struct, os, re

tbl_dir = r"C:\Games\Dev\WIldstar\NexusForever\tmp\tables\tbl"

def parse_tbl(path):
    with open(path, "rb") as f:
        data = f.read()
    rec_size    = int(struct.unpack_from("<Q", data, 24)[0])
    field_count = int(struct.unpack_from("<Q", data, 32)[0])
    field_offset= int(struct.unpack_from("<Q", data, 40)[0])
    rec_count   = int(struct.unpack_from("<Q", data, 48)[0])
    rec_offset  = int(struct.unpack_from("<Q", data, 64)[0])
    str_offset  = int(struct.unpack_from("<Q", data, 56)[0])

    fields = []
    for i in range(field_count):
        off = field_offset + i * 4
        fields.append((struct.unpack_from("<H", data, off)[0],
                       struct.unpack_from("<H", data, off+2)[0]))

    records = []
    for i in range(rec_count):
        rs = rec_offset + i * rec_size
        row = []
        for foff, ftype in fields:
            aoff = rs + foff
            if ftype == 3:   row.append(struct.unpack_from("<I", data, aoff)[0])
            elif ftype == 4: row.append(struct.unpack_from("<f", data, aoff)[0])
            elif ftype == 11:row.append(bool(struct.unpack_from("<I", data, aoff)[0]))
            elif ftype == 20:row.append(struct.unpack_from("<Q", data, aoff)[0])
            elif ftype == 130:
                soff = struct.unpack_from("<I", data, aoff)[0]
                end = data.index(b'\x00', str_offset + soff)
                row.append(data[str_offset + soff:end].decode('utf-8', errors='replace'))
            else:            row.append(None)
        records.append(row)
    return fields, records

# ─── World.tbl ───
# MapType: 0=World, 1=MiniDungeon/Expedition, 3=PvP, 6=Adventure, 11=Dungeon, 12=Raid ?
print("=== World.tbl — dungeons, raids, adventures ===")
fields, recs = parse_tbl(os.path.join(tbl_dir, "World.tbl"))
print(f"Field count: {len(fields)}, sample field types: {fields[:8]}")
if recs:
    print(f"First record: {recs[0]}")

# Try to identify MapType field by looking for known expedition IDs
# Known: Infestation=1232(type1), Slaughterdome=1535(type3)
# Filter by MapType: 6=Adventure, 11=Dungeon  (verify by cross-referencing known IDs)
known = {1232:'Infestation(Exp)', 1319:'OutpostM13(Exp)', 1535:'Slaughterdome(PvP)',
         797:'Walatiki(PvP)', 3022:'CryoPlex(PvP)', 1627:'RageLogic(Exp)'}

# Print all non-open-world map types (non-type-0) with IDs
type_groups = {}
for row in recs:
    wid = row[0]
    mtype = row[1] if len(row) > 1 else None
    name_field = next((v for v in row if isinstance(v, str) and v), '')
    if mtype not in (0, None):
        type_groups.setdefault(mtype, []).append((wid, name_field[:60]))

for mtype in sorted(type_groups.keys()):
    entries = type_groups[mtype]
    label = known.get(entries[0][0], '')
    print(f"\nMapType {mtype} ({len(entries)} maps):")
    for wid, nm in sorted(entries):
        k = known.get(wid, '')
        print(f"  {wid:6d}  {k:30s}  {nm}")

# ─── WorldLocation2.tbl ───
# Contains spawn positions indexed by ID; field layout: id, worldId, X, Y, Z, ...
print("\n\n=== WorldLocation2.tbl — field layout ===")
wl_fields, wl_recs = parse_tbl(os.path.join(tbl_dir, "WorldLocation2.tbl"))
print(f"Field count: {len(wl_fields)}, types: {wl_fields}")
if wl_recs:
    print(f"First record: {wl_recs[0]}")
    print(f"Second record: {wl_recs[1]}")

# Verify known IDs from WorldDatabase SQL files
verify_ids = {15568, 16970, 24958, 37466, 38838, 38906, 48725, 50210,
              23498, 23499, 45345, 45346, 7188, 7189}
print(f"\nVerifying known WorldLocation2 IDs:")
for row in wl_recs:
    if row[0] in verify_ids:
        print(f"  WL2 {row[0]:6d}: worldId={row[1] if len(row)>1 else '?'}  coords={row[2:6] if len(row)>=6 else row[1:]}")
