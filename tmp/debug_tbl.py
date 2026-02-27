import struct, os

tbl_dir = r"C:\Games\Dev\WIldstar\NexusForever\tmp\tables\tbl"

def dump_header(name):
    path = os.path.join(tbl_dir, name)
    with open(path, "rb") as f:
        data = f.read()
    print(f"\n=== {name} ({len(data)} bytes) ===")
    # Signature
    sig = data[:4]
    print(f"Sig: {sig}")
    # Dump first 128 bytes as hex
    for i in range(0, min(128, len(data)), 16):
        hex_part = ' '.join(f'{b:02x}' for b in data[i:i+16])
        print(f"  {i:4d}: {hex_part}")
    # Parse known offsets (from parse_raid_ach.py)
    print(f"  off24 (rec_size?):     {struct.unpack_from('<Q',data,24)[0]}")
    print(f"  off32 (field_count?):  {struct.unpack_from('<Q',data,32)[0]}")
    print(f"  off40 (field_offset?): {struct.unpack_from('<Q',data,40)[0]}")
    print(f"  off48 (rec_count?):    {struct.unpack_from('<Q',data,48)[0]}")
    print(f"  off56 (str_offset?):   {struct.unpack_from('<Q',data,56)[0]}")
    print(f"  off64 (rec_offset?):   {struct.unpack_from('<Q',data,64)[0]}")

    field_count = int(struct.unpack_from("<Q", data, 32)[0])
    field_offset = int(struct.unpack_from("<Q", data, 40)[0])
    rec_size = int(struct.unpack_from("<Q", data, 24)[0])
    rec_count = int(struct.unpack_from("<Q", data, 48)[0])
    rec_offset = int(struct.unpack_from("<Q", data, 64)[0])

    print(f"\nField descriptors (raw) at offset {field_offset}:")
    for i in range(min(field_count, 20)):
        off = field_offset + i * 4
        b0,b1,b2,b3 = data[off],data[off+1],data[off+2],data[off+3]
        foff = struct.unpack_from("<H", data, off)[0]
        ftype = struct.unpack_from("<H", data, off+2)[0]
        print(f"  field[{i:2d}]: bytes={b0:02x} {b1:02x} {b2:02x} {b3:02x}  off={foff}  type={ftype}")

    print(f"\nFirst 2 records raw ({rec_size} bytes each) at offset {rec_offset}:")
    for ri in range(min(2, rec_count)):
        rs = rec_offset + ri * rec_size
        row_bytes = data[rs:rs+rec_size]
        hex_row = ' '.join(f'{b:02x}' for b in row_bytes[:64])
        print(f"  rec[{ri}]: {hex_row}")
        # Try reading as uint32 at each 4-byte boundary
        vals = [struct.unpack_from("<I", row_bytes, j*4)[0] for j in range(min(rec_size//4, 16))]
        print(f"    uint32s: {vals}")
        # Try reading as float
        fvals = [struct.unpack_from("<f", row_bytes, j*4)[0] for j in range(min(rec_size//4, 16))]
        print(f"    floats:  {[f'{v:.3f}' for v in fvals]}")

dump_header("WorldLocation2.tbl")
dump_header("World.tbl")
