"""Copy the license notices embedded in the restored Linux runtime packages."""

import json
from pathlib import Path
import sys
import zipfile

assets_file, destination, runtime = sys.argv[1:]
assets = json.loads(Path(assets_file).read_text())
output = Path(destination)
output.mkdir(parents=True, exist_ok=True)
packages = {
    key: value["path"]
    for key, value in assets["libraries"].items()
    if key.startswith(("SkiaSharp/", "SkiaSharp.NativeAssets.Linux/",
                       "HarfBuzzSharp/", "HarfBuzzSharp.NativeAssets.Linux/"))
}
for dependency in assets["project"]["frameworks"]["net10.0"]["downloadDependencies"]:
    if dependency["name"].lower() == f"microsoft.netcore.app.runtime.{runtime}":
        version = dependency["version"].strip("[]").split(",")[0].strip()
        packages[f"DotNet-{version}"] = f"{dependency['name'].lower()}/{version}"
if not any(name.startswith("DotNet-") for name in packages):
    raise RuntimeError("The self-contained runtime license package was not found")

for name, package_path in packages.items():
    folder = next((Path(base) / package_path for base in assets["packageFolders"]
                   if (Path(base) / package_path).is_dir()), None)
    if folder is None:
        raise FileNotFoundError(package_path)
    archive = next(folder.glob("*.nupkg"))
    with zipfile.ZipFile(archive) as package:
        notices = [path for path in package.namelist()
                   if Path(path).name.lower() in ("license.txt", "third-party-notices.txt")]
        if not notices:
            raise RuntimeError(f"License missing in {name}")
        for path in notices:
            filename = name.replace("/", "-") + "-" + Path(path).name
            (output / filename).write_bytes(package.read(path))
