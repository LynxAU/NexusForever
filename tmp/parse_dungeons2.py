import struct, os

tbl_dir = r"C:\Games\Dev\WIldstar\NexusForever\tmp\tables\tbl"

HEADER_SIZE = 96  # sizeof(GameTableHeader) with Pack=1

def read_header(data):
    # struct fields (all ulong=8, except first two uint=4):
    # uint Signature, uint Version, ulong NameLength, ulong Unknown1,
    # ulong RecordSize, ulong FieldCount, ulong FieldOffset,
    # ulong RecordCount, ulong TotalRecordSize, ulong RecordOffset,
    # ulong MaxId, ulong LookupOffset, ulong Unknown2
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
# Fields (all 4-byte): Id(U), Radius(F), MaxVertDist(F), Pos0(F), Pos1(F), Pos2(F),
#                      Facing0(F), Facing1(F), Facing2(F), Facing3(F),
#                      WorldId(U), WorldZoneId(U), Phases(U)
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

# Build lookup: WorldLocation2Id -> (pos0, pos1, pos2, worldId)
wl2_by_id = {}
for i in range(h['rec_count']):
    wl2_id, pos0, pos1, pos2, world_id, zone_id = read_wl2(i)
    if wl2_id > 0:
        wl2_by_id[wl2_id] = (pos0, pos1, pos2, world_id)

# Verify known IDs
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
# Fields: Id(U), AssetPath(S→8bytes), Flags(U), Type(U), ScreenPath(S→8), ScreenModelPath(S→8),
#         ChunkBounds00..03(U×4), PlugAverageHeight(U), LocalizedTextIdName(U),
#         MinItemLevel(U), MaxItemLevel(U), PrimeLevelOffset(U), PrimeLevelMax(U),
#         VeteranTierScalingType(U), HeroismMenaceLevel(U), RewardRotationContentId(U)
# String = 2 x uint32 (8 bytes total)
# RecordSize=96, FieldCount=19
world_path = os.path.join(tbl_dir, "World.tbl")
with open(world_path, "rb") as f:
    world_data = f.read()
wh = read_header(world_data)
print(f"\nWorld.tbl: {wh['rec_count']} records, recSize={wh['rec_size']}, maxId={wh['max_id']}")

# String table
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
        return f"<off={use_off}>"
    end = str_table.index(b'\x00', rel) if b'\x00' in str_table[rel:] else rel + 64
    return str_table[rel:end].decode('utf-8', errors='replace')

world_rec_base = HEADER_SIZE + wh['rec_offset']

# MapType enum (from WildStar/NexusForever):
MAP_TYPES = {0:'World', 1:'MiniDungeon/Expedition', 3:'PvP', 6:'Adventure', 11:'Dungeon', 12:'Raid', 16:'Battleground', 21:'Tutorial'}

def read_world(idx):
    off = world_rec_base + idx * wh['rec_size']
    world_id = struct.unpack_from("<I", world_data, off)[0]
    # AssetPath string: 2 x uint32
    ap_off1, ap_off2 = struct.unpack_from("<II", world_data, off + 4)
    flags = struct.unpack_from("<I", world_data, off + 12)[0]
    map_type = struct.unpack_from("<I", world_data, off + 16)[0]
    # ScreenPath string
    sp_off1, sp_off2 = struct.unpack_from("<II", world_data, off + 20)
    # ScreenModelPath string
    smp_off1, smp_off2 = struct.unpack_from("<II", world_data, off + 28)
    name = read_world_string(ap_off1, ap_off2)
    return world_id, map_type, name

print("\nNon-open-world maps (type != 0):")
for i in range(wh['rec_count']):
    world_id, map_type, name = read_world(i)
    if map_type != 0:
        type_label = MAP_TYPES.get(map_type, f'type{map_type}')
        print(f"  World {world_id:5d}  [{type_label:22s}]  {name[:60]}")

# ─── Find WorldLocation2 entries matching each dungeon/adventure/raid world ───
print("\n\nWorldLocation2 spawn points per dungeon/raid/adventure world:")
interesting_types = {1, 3, 6, 11, 12, 16}
worlds_of_interest = {}
for i in range(wh['rec_count']):
    world_id, map_type, name = read_world(i)
    if map_type in interesting_types:
        worlds_of_interest[world_id] = (map_type, name)

wl2_by_world = {}
for wl2_id, (pos0, pos1, pos2, world_id) in wl2_by_id.items():
    if world_id in worlds_of_interest:
        wl2_by_world.setdefault(world_id, []).append((wl2_id, pos0, pos1, pos2))

for world_id in sorted(worlds_of_interest):
    map_type, name = worlds_of_interest[world_id]
    type_label = MAP_TYPES.get(map_type, f'type{map_type}')
    entries = wl2_by_world.get(world_id, [])
    print(f"\n  World {world_id} [{type_label}] {name[:50]}")
    for wl2_id, pos0, pos1, pos2 in sorted(entries):
        print(f"    WL2 {wl2_id:6d}: ({pos0:10.2f}, {pos1:10.2f}, {pos2:10.2f})")
    if not entries:
        print("    (no WorldLocation2 entries)")
