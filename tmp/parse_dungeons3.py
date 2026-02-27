import struct, os

tbl_dir = r"C:\Games\Dev\WIldstar\NexusForever\tmp\tables\tbl"

HEADER_SIZE = 96  # sizeof(GameTableHeader) with Pack=1

def read_header(data):
    sig, ver = struct.unpack_from("<II", data, 0)
    name_len, unk1, rec_size, field_count, field_offset, rec_count, \
        total_rec_size, rec_offset, max_id, lookup_offset, unk2 = \
        struct.unpack_from("<11Q", data, 8)
    return {
        'rec_size': int(rec_size),
        'field_count': int(field_count),
        'field_offset': int(field_offset),
        'rec_count': int(rec_count),
        'total_rec_size': int(total_rec_size),
        'rec_offset': int(rec_offset),
        'max_id': int(max_id),
    }

# ─── WorldLocation2.tbl ───
# Fields: Id(U), Radius(F), MaxVertDist(F), Pos0(F), Pos1(F), Pos2(F),
#         Facing0-3(F×4), WorldId(U), WorldZoneId(U), Phases(U)  [13 fields × 4 = 52 bytes/rec]
wl2_path = os.path.join(tbl_dir, "WorldLocation2.tbl")
with open(wl2_path, "rb") as f:
    wl2_data = f.read()
h = read_header(wl2_data)
print(f"WorldLocation2.tbl: {h['rec_count']} records, recSize={h['rec_size']}")

rec_base = HEADER_SIZE + h['rec_offset']

def read_wl2(idx):
    off = rec_base + idx * h['rec_size']
    wl2_id, radius, max_vert = struct.unpack_from("<Iff", wl2_data, off)
    pos0, pos1, pos2 = struct.unpack_from("<fff", wl2_data, off + 12)
    f0, f1, f2, f3 = struct.unpack_from("<ffff", wl2_data, off + 24)
    world_id, zone_id, phases = struct.unpack_from("<III", wl2_data, off + 40)
    return wl2_id, pos0, pos1, pos2, world_id, zone_id

wl2_by_id = {}
for i in range(h['rec_count']):
    wl2_id, pos0, pos1, pos2, world_id, zone_id = read_wl2(i)
    if wl2_id > 0:
        wl2_by_id[wl2_id] = (pos0, pos1, pos2, world_id)

# Verify known IDs from our migrations
verify = {15568, 16970, 24958, 37466, 38838, 38906, 48725, 50210,
          23498, 23499, 45345, 45346, 7188, 7189}
print("\nVerifying known WorldLocation2 IDs:")
for wl2_id in sorted(verify):
    if wl2_id in wl2_by_id:
        pos0, pos1, pos2, world_id = wl2_by_id[wl2_id]
        print(f"  WL2 {wl2_id:6d}: worldId={world_id:5d}  pos=({pos0:10.2f}, {pos1:10.2f}, {pos2:10.2f})")
    else:
        print(f"  WL2 {wl2_id:6d}: NOT FOUND")

# ─── World.tbl ───
# RecordSize=96, 19 table fields, 16 model fields (ChunkBounds is array of 4)
#
# Record layout (96 bytes total):
#   +00  Id             (uint, 4)
#   +04  AssetPath off1 (uint, 4)  \  String field: 8 bytes total
#   +08  AssetPath off2 (uint, 4)  /
#   +12  [padding]      (4 bytes)  — skip: offset1==0 & next field != String
#   +16  Flags          (uint, 4)
#   +20  MapType        (uint, 4)  ← WAS WRONG at +16 in old parser
#   +24  ScreenPath off1 (uint, 4) \  String field: 8 bytes, NO skip (next=String)
#   +28  ScreenPath off2 (uint, 4) /
#   +32  ScreenModelPath off1 (uint,4) \  String field: 8 bytes
#   +36  ScreenModelPath off2 (uint,4) /
#   +40  [padding]      (4 bytes)  — skip: offset1==0 & next field != String
#   +44  ChunkBounds[0] (uint, 4)
#   +48  ChunkBounds[1] (uint, 4)
#   +52  ChunkBounds[2] (uint, 4)
#   +56  ChunkBounds[3] (uint, 4)
#   +60  PlugAverageHeight      (uint, 4)
#   +64  LocalizedTextIdName    (uint, 4)
#   +68  MinItemLevel           (uint, 4)
#   +72  MaxItemLevel           (uint, 4)
#   +76  PrimeLevelOffset       (uint, 4)
#   +80  PrimeLevelMax          (uint, 4)
#   +84  VeteranTierScalingType (uint, 4)
#   +88  HeroismMenaceLevel     (uint, 4)
#   +92  RewardRotationContentId(uint, 4)
#  Total = 96 bytes ✓

world_path = os.path.join(tbl_dir, "World.tbl")
with open(world_path, "rb") as f:
    world_data = f.read()
wh = read_header(world_data)
print(f"\nWorld.tbl: {wh['rec_count']} records, recSize={wh['rec_size']}, maxId={wh['max_id']}")

rec_total_bytes = wh['rec_size'] * wh['rec_count']
str_table_start = HEADER_SIZE + wh['rec_offset'] + rec_total_bytes
str_table_size = wh['total_rec_size'] - rec_total_bytes
str_table = world_data[str_table_start:str_table_start + str_table_size]

def read_world_string(off1, off2):
    use_off = max(off1, off2)
    if use_off == 0:
        return ""
    rel = use_off - rec_total_bytes
    if rel < 0 or rel >= len(str_table):
        return f"<off={use_off},rel={rel}>"
    nul = str_table.find(b'\x00', rel)
    end = nul if nul >= 0 else rel + 128
    return str_table[rel:end].decode('utf-8', errors='replace')

world_rec_base = HEADER_SIZE + wh['rec_offset']

MAP_TYPES = {
    0:  'World',
    1:  'Expedition',
    3:  'PvP',
    6:  'Adventure',
    11: 'Dungeon',
    12: 'Raid',
    16: 'Battleground',
    21: 'Tutorial',
}

def read_world(idx):
    off = world_rec_base + idx * wh['rec_size']
    world_id = struct.unpack_from("<I", world_data, off)[0]
    ap_off1, ap_off2 = struct.unpack_from("<II", world_data, off + 4)
    # +12 is 4-byte padding (always present: offset1==0, next=Flags=UInt)
    flags    = struct.unpack_from("<I", world_data, off + 16)[0]
    map_type = struct.unpack_from("<I", world_data, off + 20)[0]
    # ScreenPath at +24 (no skip after: next=ScreenModelPath=String)
    sp_off1, sp_off2 = struct.unpack_from("<II", world_data, off + 24)
    # ScreenModelPath at +32
    smp_off1, smp_off2 = struct.unpack_from("<II", world_data, off + 32)
    # +40 is 4-byte padding (always present: offset1==0, next=ChunkBounds=UInt)
    name = read_world_string(ap_off1, ap_off2)
    return world_id, flags, map_type, name

# Debug: show first 5 records with raw data
print("\n--- First 5 World records (debug) ---")
for idx in range(min(5, wh['rec_count'])):
    off = world_rec_base + idx * wh['rec_size']
    raw = world_data[off:off+24]
    world_id, flags, map_type, name = read_world(idx)
    print(f"  idx={idx} raw={raw.hex()} -> id={world_id} flags={flags} type={map_type} name={name!r}")

# Show all non-open-world maps
print("\nAll non-open-world maps (type != 0):")
for i in range(wh['rec_count']):
    world_id, flags, map_type, name = read_world(i)
    if map_type != 0:
        type_label = MAP_TYPES.get(map_type, f'type{map_type}')
        print(f"  World {world_id:5d}  [{type_label:12s}]  {name[:70]}")

# ─── WorldLocation2 entries per dungeon/adventure/raid world ───
interesting_types = {1, 3, 6, 11, 12, 16}
worlds_of_interest = {}
for i in range(wh['rec_count']):
    world_id, flags, map_type, name = read_world(i)
    if map_type in interesting_types:
        worlds_of_interest[world_id] = (map_type, name)

wl2_by_world = {}
for wl2_id, (pos0, pos1, pos2, world_id) in wl2_by_id.items():
    if world_id in worlds_of_interest:
        wl2_by_world.setdefault(world_id, []).append((wl2_id, pos0, pos1, pos2))

print("\n\nWorldLocation2 spawn points per dungeon/raid/adventure world:")
for world_id in sorted(worlds_of_interest):
    map_type, name = worlds_of_interest[world_id]
    type_label = MAP_TYPES.get(map_type, f'type{map_type}')
    entries = wl2_by_world.get(world_id, [])
    print(f"\n  World {world_id} [{type_label}] {name[:60]}")
    for wl2_id, pos0, pos1, pos2 in sorted(entries):
        print(f"    WL2 {wl2_id:6d}: ({pos0:10.2f}, {pos1:10.2f}, {pos2:10.2f})")
    if not entries:
        print("    (no WorldLocation2 entries)")
