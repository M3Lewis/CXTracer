import os
from PIL import Image

# Use relative path in workspace
ico_dir = os.path.join("src", "CXTracer", "Icons")

print("Loading icons...")
img16 = Image.open(os.path.join(ico_dir, "AppIcon16.ico")).copy()
img24 = Image.open(os.path.join(ico_dir, "AppIcon24.ico")).copy()
img48 = Image.open(os.path.join(ico_dir, "AppIcon48.ico")).copy()
img256 = Image.open(os.path.join(ico_dir, "AppIcon256.ico")).copy()

img16 = img16.convert("RGBA")
img24 = img24.convert("RGBA")
img48 = img48.convert("RGBA")
img256 = img256.convert("RGBA")

output_path = os.path.join(ico_dir, "AppIcon.ico")
print(f"Saving combined icon to {output_path}...")
img256.save(
    output_path,
    format="ICO",
    sizes=[(256, 256), (48, 48), (24, 24), (16, 16)],
    append_images=[img48, img24, img16]
)
print("Successfully combined icons into AppIcon.ico!")
