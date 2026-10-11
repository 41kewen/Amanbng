"""Exercise desktop entry escaping, upgrades, uninstall, and overwrite protection."""

import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import time


scripts = Path(__file__).resolve().parent
with tempfile.TemporaryDirectory(prefix="amanbng-install-test-") as temporary:
    root = Path(temporary)
    package = root / "package with space"
    package.mkdir()
    (package / "Assets").mkdir()
    for name in ("install.sh", "uninstall.sh"):
        shutil.copy2(scripts / name, package / name)
    (package / "Assets/icon.png").write_bytes(b"fixture")
    executable = package / "Amanbng.Linux"
    executable.write_text('#!/bin/sh\nprintf ok > "$AMANBNG_TEST_RESULT"\n')
    executable.chmod(0o755)
    data = root / 'data space "quote" $cash `tick` %field \\slash'
    result = root / "launched"
    environment = {
        **os.environ,
        "XDG_DATA_HOME": str(data),
        "AMANBNG_TEST_RESULT": str(result),
    }

    def run(*arguments):
        return subprocess.run(arguments, env=environment, check=True, timeout=20)

    for _ in range(2):
        run("bash", str(package / "install.sh"))
    desktop = data / "applications/amanbng-linux.desktop"
    run("desktop-file-validate", str(desktop))
    run("gio", "launch", str(desktop))
    for _ in range(40):
        if result.exists():
            break
        time.sleep(0.05)
    assert result.read_text() == "ok", "Desktop entry must execute the exact installed path"
    run("bash", str(data / "amanbng-linux/uninstall.sh"))
    assert not desktop.exists() and not (data / "amanbng-linux").exists()

    (data / "amanbng-linux").mkdir()
    retained = data / "amanbng-linux/keep-me"
    retained.write_text("preserve")
    rejected = subprocess.run(
        ["bash", str(package / "install.sh")],
        env=environment, capture_output=True, timeout=20,
    )
    assert rejected.returncode != 0 and retained.read_text() == "preserve"
    print("Installation, upgrade, desktop launch, uninstall and overwrite protection passed.")
