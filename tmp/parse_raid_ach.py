import struct
import os

tbl_dir = r"C:\Games\Dev\WIldstar\NexusForever\tmp\tables\tbl"

def parse_tbl_header(data):
    rec_size    = struct.unpack_from("<Q", data, 24)[0]
    field_count = struct.unpack_from("<Q", data, 32)[0]
    field_offset= struct.unpack_from("<Q", data, 40)[0]
    rec_count   = struct.unpack_from("<Q", data, 48)[0]
    rec_offset  = struct.unpack_from("<Q", data, 64)[0]
    return int(rec_size), int(field_count), int(field_offset), int(rec_count), int(rec_offset)

def parse_fields(data, field_count, field_offset):
    fields = []
    for i in range(field_count):
        off = field_offset + i * 4
        field_off  = struct.unpack_from("<H", data, off)[0]
        field_type = struct.unpack_from("<H", data, off + 2)[0]
        fields.append((field_off, field_type))
    return fields

def get_val(data, rec_start, field_off, field_type):
    abs_off = rec_start + field_off
    if field_type == 3:    return struct.unpack_from("<I", data, abs_off)[0]
    elif field_type == 4:  return struct.unpack_from("<f", data, abs_off)[0]
    elif field_type == 11:
        v = struct.unpack_from("<I", data, abs_off)[0]
        return bool(v)
    elif field_type == 20: return struct.unpack_from("<Q", data, abs_off)[0]
    elif field_type == 130:return struct.unpack_from("<I", data, abs_off)[0]
    return None

# 1. Parse Achievement.tbl
ach_path = os.path.join(tbl_dir, "Achievement.tbl")
with open(ach_path, "rb") as f:
    data = f.read()
rec_size, field_count, field_offset, rec_count, rec_offset = parse_tbl_header(data)
fields = parse_fields(data, field_count, field_offset)
print(f"Achievement.tbl: recSize={rec_size}, fieldCount={field_count}, recCount={rec_count}")
print(f"Fields (offset, type): {fields}")
print("First 5 records:")
for i in range(min(5, rec_count)):
    rec_start = rec_offset + i * rec_size
    vals = [get_val(data, rec_start, fo, ft) for fo, ft in fields]
    print(f"  rec[{i}]: {vals}")

# 2. Achievement-related tbl files
print("\nAchievement-related tbl files:")
for f in sorted(os.listdir(tbl_dir)):
    if "chiev" in f.lower():
        print(f"  {f}")

# 3. Prerequisite.tbl
prereq_path = os.path.join(tbl_dir, "Prerequisite.tbl")
if os.path.exists(prereq_path):
    with open(prereq_path, "rb") as f:
        data2 = f.read()
    rec_size2, field_count2, field_offset2, rec_count2, rec_offset2 = parse_tbl_header(data2)
    fields2 = parse_fields(data2, field_count2, field_offset2)
    print(f"\nPrerequisite.tbl: recSize={rec_size2}, fieldCount={field_count2}, recCount={rec_count2}")
    print(f"Fields: {fields2}")
    print("First 3 records:")
    for i in range(min(3, rec_count2)):
        rec_start = rec_offset2 + i * rec_size2
        vals = [get_val(data2, rec_start, fo, ft) for fo, ft in fields2]
        print(f"  rec[{i}]: {vals}")
