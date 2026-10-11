#!/usr/bin/env bash
set -euo pipefail

data_dir=${XDG_DATA_HOME:-"$HOME/.local/share"}
if [[ "$data_dir" != /* ]]; then
    printf 'XDG_DATA_HOME 必须是绝对路径。\n' >&2
    exit 1
fi
install_dir="$data_dir/amanbng-linux"
desktop_file="$data_dir/applications/amanbng-linux.desktop"
if [[ -L "$install_dir" || ! -f "$install_dir/.amanbng-install" || "$(cat -- "$install_dir/.amanbng-install")" != amanbng-linux-user-install-v1 ]]; then
    printf '没有找到本脚本管理的安装：%s\n' "$install_dir" >&2
    exit 1
fi
rm -rf -- "$install_dir"
if [[ ! -L "$desktop_file" && -f "$desktop_file" ]] && grep -qx 'X-Amanbng-Linux=true' "$desktop_file"; then
    rm -- "$desktop_file"
fi
if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database "$data_dir/applications"
fi
printf '牛逼工具箱已卸载。\n'
