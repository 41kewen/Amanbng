# 牛逼工具箱 · Linux

这是 Amanbng 的 Linux 桌面移植版，使用 C# / .NET 10 和 Avalonia 11.3.18。原 Windows UWP 工程保持独立；Linux 不需要 Wine、Windows 证书或 Microsoft Store。

## 使用

支持带有 X11 或 XWayland 的 Linux 桌面。推荐 Ubuntu 24.04 / Linux Mint 22 或更新的兼容发行版。发布包包含 .NET 运行时，使用时不需要安装 SDK。`linux-x64` 用于 Intel/AMD 64 位电脑；ARM64 可自行交叉发布，但尚未进行真机验证。原生 Wayland、Alpine/musl 不在当前支持范围内。

从本次 Linux 移植的 GitHub Release（若提供）或 Actions 的 `amanbng-linux-x64` 构建产物下载并解压，然后运行：

```bash
sha256sum -c Amanbng-linux-x64.tar.gz.sha256
tar -xzf Amanbng-linux-x64.tar.gz
cd Amanbng-linux-x64
./Amanbng.Linux
```

Actions 下载的是外层 ZIP，先解压 ZIP 才能得到上述 tar.gz 和校验文件。原仓库的 `App_*.zip` 为 Windows 包，不能直接在 Linux 上运行。

可选安装到当前用户的应用菜单（不用 sudo）：

```bash
./install.sh
```

安装目录是 `${XDG_DATA_HOME:-$HOME/.local/share}/amanbng-linux`；桌面入口在同一数据目录的 `applications` 中。再次运行新版本的 `install.sh` 可更新该安装。卸载：

```bash
"${XDG_DATA_HOME:-$HOME/.local/share}/amanbng-linux/uninstall.sh"
```

## 系统依赖与音效

Ubuntu / Debian / Mint 可安装：

```bash
sudo apt install libx11-6 libice6 libsm6 libfontconfig1 libicu-dev xdg-utils fonts-noto-cjk \
  gstreamer1.0-tools gstreamer1.0-plugins-base-apps gstreamer1.0-plugins-good \
  gstreamer1.0-plugins-ugly gstreamer1.0-libav
```

Wayland 桌面还需要 XWayland。字体缺失时可能显示方框，安装 Noto CJK 字体后重新启动应用。

音效依次使用 `gst-play-1.0`、`ffplay` 或 `mpg123` 中已安装的第一个播放器；需要可用的音频服务及 MP3 解码器。播放失败时底部显示原因，其余功能仍可用，也可关闭右上角“音效”。快速点击会停止上一段音效再播放下一段；切换工具或关闭窗口会停止播放。

“时间查看器”和项目链接由系统默认浏览器打开；视频彩蛋由系统默认视频播放器打开。缺少关联应用时会提示错误。

## 保留的功能

| 功能 | Linux 版行为 |
| --- | --- |
| 超级读卡器 | 显示“dú kā qì”并播放原版 N1.mp3 |
| 身高计算器 | 输入身高，等待 3 秒后返回输入的身高 |
| 时间查看器 | 四种显示语言，打开百度搜索“现在几点了” |
| 手机定位器 | 等待 5 秒后返回输入的位置；高级模式显示原图 |
| 字符拼接器 | 拼接两段文字，支持换行和 Unicode，可复制结果 |
| 剪刀石头布 | 保留原版可见的 AI 出招，以及“普通”模式只能选输的一手；增加“再来一局” |
| 电子功德器 | 原版图片/音效，敲击加一，点击图片清零 |

保留原版简单/困难模式的相同行为；手机定位和读卡均为娱乐效果，不调用真实定位或读卡硬件。功德数和输入内容只保留在当前窗口中。

Linux 直接进入工具箱，省去原版无服务器的登录页、Windows Hello 和首次启动标记文件。首页用静态界面替代自动循环视频，原背景视频和“体验内测”视频仍可点击播放。素材直接从原仓库 `App/Assets` 引用，不重复存放。

## 开发与验证

安装 .NET 10 SDK，在仓库根目录执行（不要构建 Windows 的 OneApp.sln）：

```bash
dotnet restore Linux/Amanbng.Linux.slnx --locked-mode
dotnet build Linux/Amanbng.Linux.slnx -c Release --no-restore
dotnet format Linux/Amanbng.Linux.slnx --verify-no-changes --no-restore
dotnet test Linux/Amanbng.Linux.slnx -c Release --no-build
dotnet run --project Linux/Amanbng.Linux -c Release
```

测试使用 Avalonia Headless，不需要显示服务器；覆盖出拳组合、文字输入、页面交互、延时完成/取消、音效和打开链接的失败反馈。Headless 测试替换系统调用，真实桌面和音频仍需人工验证。

安装脚本的集成检查：`python3 Linux/scripts/test-install.py`（需要 Python 3、`desktop-file-validate` 和 `gio`）。检查特殊路径的安装、升级、菜单启动、卸载及已有目录保护，全部在临时目录执行。

发布可直接运行的自包含压缩包（打包需要 Python 3 来收集依赖许可证）：

```bash
bash Linux/scripts/publish-linux.sh
# 可选：DOTNET=/path/to/dotnet bash Linux/scripts/publish-linux.sh linux-arm64
```

输出为 `artifacts/Amanbng-linux-x64.tar.gz` 及 SHA-256 校验文件。包中包含原项目 Apache-2.0 许可证；第三方运行时、框架及原素材保留各自的适用许可。新增平台代码集中在 `Linux/`，NuGet 锁文件用于可重复还原依赖。
