"""Package the generated transparent logo into Windows icon sizes, preserving alpha."""
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
source = root / "assets" / "aion-2-helper.png"
image = Image.open(source)
if image.mode != "RGBA" or image.getchannel("A").getextrema() != (0, 255):
    raise SystemExit("The logo must retain genuine transparent alpha.")
image.save(root / "assets" / "aion-2-helper.ico", format="ICO",
           sizes=[(16, 16), (20, 20), (24, 24), (32, 32), (40, 40),
                  (48, 48), (64, 64), (128, 128), (256, 256)])
print("Aion 2 Helper icon packaged with alpha at 9 Windows sizes.")
