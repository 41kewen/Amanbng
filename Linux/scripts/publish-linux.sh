#!/usr/bin/env bash
set -euo pipefail

repo_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)
runtime=${1:-linux-x64}
if [[ "$runtime" != linux-x64 && "$runtime" != linux-arm64 ]]; then
    printf '支持的目标：linux-x64、linux-arm64\n' >&2
    exit 1
fi
dotnet_cmd=${DOTNET:-dotnet}
output_dir="$repo_dir/artifacts"
mkdir -p -- "$output_dir"
stage=$(mktemp -d "$output_dir/.package.XXXXXX")
trap 'rm -rf -- "$stage"' EXIT
package="Amanbng-$runtime"
mkdir -p -- "$stage/$package"

"$dotnet_cmd" publish "$repo_dir/Linux/Amanbng.Linux/Amanbng.Linux.csproj" \
    -c Release -r "$runtime" --self-contained true \
    -p:RestoreLockedMode=true -p:DebugType=None -p:DebugSymbols=false \
    -o "$stage/$package"
cp -- "$repo_dir/LICENSE.txt" "$repo_dir/Linux/README.md" "$stage/$package/"
cp -- "$repo_dir/Linux/scripts/install.sh" "$repo_dir/Linux/scripts/uninstall.sh" "$stage/$package/"
cp -r -- "$repo_dir/Linux/licenses" "$stage/$package/"
python3 "$repo_dir/Linux/scripts/collect-licenses.py" \
    "$repo_dir/Linux/Amanbng.Linux/obj/project.assets.json" "$stage/$package/licenses" "$runtime"
chmod +x "$stage/$package/Amanbng.Linux" "$stage/$package/install.sh" "$stage/$package/uninstall.sh"
tar -czf "$output_dir/$package.tar.gz" -C "$stage" "$package"
(
    cd -- "$output_dir"
    sha256sum "$package.tar.gz" > "$package.tar.gz.sha256"
)
printf '已生成：%s/%s.tar.gz\n' "$output_dir" "$package"
