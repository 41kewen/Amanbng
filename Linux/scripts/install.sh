#!/usr/bin/env bash
set -euo pipefail

source_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
data_dir=${XDG_DATA_HOME:-"$HOME/.local/share"}
if [[ "$data_dir" != /* || "$data_dir" == *$'\n'* || "$data_dir" == *$'\r'* ]]; then
    printf 'XDG_DATA_HOME 必须是没有换行的绝对路径。\n' >&2
    exit 1
fi
install_dir="$data_dir/amanbng-linux"
desktop_file="$data_dir/applications/amanbng-linux.desktop"
marker=amanbng-linux-user-install-v1

if [[ "$source_dir" == "$install_dir" ]]; then
    printf '已在安装目录中，无需再次安装。\n'
    exit 0
fi
if [[ ! -x "$source_dir/Amanbng.Linux" || ! -f "$source_dir/Assets/icon.png" ]]; then
    printf '请从完整解压后的 Linux 安装包运行 install.sh。\n' >&2
    exit 1
fi
if [[ -L "$install_dir" ]] || { [[ -e "$install_dir" ]] && [[ ! -f "$install_dir/.amanbng-install" || "$(cat -- "$install_dir/.amanbng-install")" != "$marker" ]]; }; then
    printf '目标目录不是本脚本管理的安装，拒绝覆盖：%s\n' "$install_dir" >&2
    exit 1
fi
if [[ -L "$desktop_file" ]] || { [[ -e "$desktop_file" ]] && ! grep -qx 'X-Amanbng-Linux=true' "$desktop_file"; }; then
    printf '已有同名桌面入口，拒绝覆盖：%s\n' "$desktop_file" >&2
    exit 1
fi

mkdir -p -- "$data_dir/applications"
stage=$(mktemp -d "$data_dir/.amanbng-install.XXXXXX")
trap 'rm -rf -- "$stage"' EXIT
cp -a -- "$source_dir/." "$stage/"
printf '%s\n' "$marker" > "$stage/.amanbng-install"

# Desktop Entry strings and Exec command lines use different escaping rules.
# The env launcher also avoids GLib treating literal percent signs as field codes in the executable name.
escape_string() {
    local value=$1
    value=${value//\\/\\\\}
    value=${value//$'\t'/\\t}
    printf '%s' "$value"
}
exec_path="$install_dir/Amanbng.Linux"
exec_path=${exec_path//\\/\\\\}
exec_path=${exec_path//\"/\\\"}
exec_path=${exec_path//\$/\\\$}
exec_path=${exec_path//\`/\\\`}
exec_path=${exec_path//%/%%}
exec_path=$(escape_string "$exec_path")
icon_path=$(escape_string "$install_dir/Assets/icon.png")
cat > "$stage/amanbng-linux.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=牛逼工具箱
Name[en]=Amanbng
Comment=Amanbng Linux 娱乐工具箱
Exec=/usr/bin/env "$exec_path"
Icon=$icon_path
Terminal=false
Categories=Utility;Amusement;
StartupWMClass=Amanbng.Linux
X-Amanbng-Linux=true
EOF

if [[ -d "$install_dir" ]]; then
    rm -rf -- "$install_dir"
fi
mv -- "$stage" "$install_dir"
cp -- "$install_dir/amanbng-linux.desktop" "$desktop_file"
if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database "$data_dir/applications"
fi
printf '已安装：%s\n可从应用菜单打开“牛逼工具箱”。\n' "$install_dir"
