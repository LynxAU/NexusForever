import struct, os

tbl_dir = r'C:\Games\Dev\WIldstar\NexusForever\tmp\tables\tbl'
HEADER_SIZE = 96

def read_header(data):
    fields = struct.unpack_from('<11Q', data, 8)
    names = ['name_len','unk1','rec_size','field_count','field_offset','rec_count',
             'total_rec_size','rec_offset','max_id','lookup_offset','unk2']
    return dict(zip(names, [int(v) for v in fields]))

with open(os.path.join(tbl_dir, 'WorldLocation2.tbl'), 'rb') as f:
    wl2_data = f.read()
h2 = read_header(wl2_data)
rb2 = HEADER_SIZE + h2['rec_offset']
wl2_by_world = {}
for i in range(h2['rec_count']):
    off = rb2 + i * h2['rec_size']
    wl2_id, = struct.unpack_from('<I', wl2_data, off)
    pos = struct.unpack_from('<fff', wl2_data, off + 12)
    world_id, = struct.unpack_from('<I', wl2_data, off + 40)
    if wl2_id > 0:
        wl2_by_world.setdefault(world_id, []).append((wl2_id,) + pos)

with open(os.path.join(tbl_dir, 'World.tbl'), 'rb') as f:
    wd = f.read()
wh = read_header(wd)
rec_total = wh['rec_size'] * wh['rec_count']
str_start = HEADER_SIZE + wh['rec_offset'] + rec_total
stbl = wd[str_start:str_start + wh['total_rec_size'] - rec_total]
wrb = HEADER_SIZE + wh['rec_offset']


def getstr(o1, o2):
    u = max(o1, o2)
    if u == 0:
        return ''
    r = u - rec_total
    if r < 0:
        return ''
    i = r
    while i + 1 < len(stbl):
        if stbl[i] == 0 and stbl[i + 1] == 0:
            break
        i += 2
    s = stbl[r:i].decode('utf-16-le', 'replace')
    sep = '\\'
    parts = s.split(sep)
    return parts[-1] if len(parts) > 1 else s


def dist(x, y, z):
    return (x ** 2 + y ** 2 + z ** 2) ** 0.5


# Worlds to skip (test/no content/duplicates)
skip_worlds = {
    1343,  # DungeonSettingsTest - test world
    1265,  # Eastern - unclear/test
    1496,  # Western - unclear/test
    3042,  # Datascape duplicate - 0 WL2
    3524,  # RedMoonTerror duplicate - 0 WL2
    3131,  # WarplotSkyMapBackup - no WL2
}

print("-- Entrance WL2 per instance world (min distance to origin) --")
print()

for target_type, label in [(11, 'DUNGEON'), (6, 'ADVENTURE'), (9, 'RAID'), (3, 'PVP_ARENA')]:
    print(f"=== {label} (mapType={target_type}) ===")
    for i in range(wh['rec_count']):
        off = wrb + i * wh['rec_size']
        wid, = struct.unpack_from('<I', wd, off)
        mt, = struct.unpack_from('<I', wd, off + 20)
        if mt != target_type:
            continue
        if wid in skip_worlds:
            continue
        o1, o2 = struct.unpack_from('<II', wd, off + 4)
        name = getstr(o1, o2)
        entries = wl2_by_world.get(wid, [])
        if not entries:
            print(f"  World {wid:5d} [{name}]  -- NO WL2 ENTRIES, skip")
            continue

        # Find min-distance WL2
        best = min(entries, key=lambda e: dist(e[1], e[2], e[3]))
        wl2id, x, y, z = best
        d = dist(x, y, z)

        # Also show WL2 with lowest ID
        first_by_id = sorted(entries)[0]
        fid, fx, fy, fz = first_by_id
        fd = dist(fx, fy, fz)

        chosen = wl2id if d < fd else fid
        reason = "min_dist" if d < fd else "first_id"

        print(f"  World {wid:5d} [{name:35s}]  chosen_WL2={chosen:6d} ({reason})  dist={min(d,fd):.1f}")
        if wl2id != fid:
            print(f"    min-dist: WL2 {wl2id:6d} dist={d:.1f}  vs  first-id: WL2 {fid:6d} dist={fd:.1f}")

    print()
