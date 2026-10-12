<!-- alarmapp-download-repository -->
<p align="center"><img src="assets/AppIcon.png" width="88" alt="AlarmApp icon"></p>

# Alarm-app · 闹钟

一个独立运行的 Windows / Android 闹钟应用，支持定时与间隔提醒、世界时钟、秒表、倒计时、应用内音量调节和使用记录查询。使用者不需要 Codex 或账号。

[下载与版本发布](https://github.com/lmynb68/Alarm-app/releases) · [问题反馈](https://github.com/lmynb68/Alarm-app/issues) · [VS Code 上传说明](docs/VISUAL_STUDIO.md)

> 普通用户从 Releases 下载 Windows 便携 ZIP 或 Android APK；开发者从仓库获取源码。是否有可下载版本，以 Releases 页面为准。

## 功能

- 定时闹钟：单次或按星期重复，支持读取当前时间，精确到秒。
- 间隔提醒：自定义间隔和次数，例如每 5 分钟响一次，共 12 次。
- 响铃操作：停止、5 分钟后提醒；关闭主窗口后留在托盘运行。
- 铃声：5 种内置声音，每个闹钟独立选择，支持导入 WAV / MP3 和试听。
- 世界时钟：添加、移除城市或时区，显示当地日期和时间。
- 计时：秒表显示分、秒和毫秒；倒计时支持暂停继续，到时响铃。
- 响铃音量：应用内调节并保存，适用于铃声和试听。
- 界面语言：简体中文 / English，切换后记住选择。
- 简要统计：按月、按类型查看计时使用时长、使用次数和响铃时长。
- 详细记录：按天查看名称、闹钟类型、开始结束时间和持续时长。

## 下载安装

安卓需要 Android 8.0 或以上。下载 APK 后安装，在首页允许闹钟与提醒、通知等权限；具体见 [安卓使用与构建说明](android/README.md)。当前 APK 为调试签名测试包。


需要 Windows 10 / 11 桌面环境及 .NET Framework 4.8 或更高版本。

| 下载文件 | 使用方式 |
| --- | --- |
| AlarmApp-1.0.0-Portable.zip | 完整解压，然后双击 AlarmApp.exe；保留声音和图标资源。 |
| SHA256SUMS.txt | 用于检查便携包的 SHA-256。 |

只提供便携版：解压即可运行，无需安装。程序不会自动设置开机启动。

更新或删除程序文件前，从托盘退出已运行的闹钟。关机、注销和退出程序后不会响铃；睡眠唤醒取决于硬件与 Windows 电源设置。当前发行包未进行代码签名。

## 数据与统计口径

设置、导入的铃声和历史记录保存在 %LOCALAPPDATA%/IndependentAlarm，发行包不包含维护者的个人记录，删除解压目录不会删除该处用户数据。

计时使用时长是闹钟启用且程序运行的时间；多个闹钟分别累计。使用次数是查询期间有计时时长的启用会话数。响铃时长是响铃窗口打开至停止或稍后提醒的时间。跨日、跨月按日期分摊；测试响铃不计入历史。历史从启用记录功能开始积累，不补造旧记录。

## 用 Visual Studio Code 开发

1. 克隆 https://github.com/lmynb68/Alarm-app.git，在 VS Code 中打开仓库文件夹。
2. 在 Windows 终端运行 Build.cmd，生成便携包；需要 .NET Framework 4.8 环境。
3. AlarmApp.sln 也可供 Microsoft Visual Studio 使用；这里用户所说的 VS 指 Visual Studio Code。

也可在 Windows 双击 Build.cmd：编译、运行自检并生成 artifacts/ 下的便携包和校验文件。自检失败时不会生成新的发行压缩包。实际响铃与托盘行为仍需要按 [验收清单](docs/TESTING.md) 手动验证。

## 项目结构

| 路径 | 内容 |
| --- | --- |
| android/ | 原生 Android 项目、Gradle Wrapper、测试与构建说明 |
| src/AlarmApp/ | C# 源码、项目文件和版本信息 |
| assets/ | 多尺寸图标与内置铃声 |
| tools/ | 构建和 GitHub Release 发布脚本 |
| docs/ | 使用、发布、测试与 VS 上传说明 |
| .github/ | Windows 构建流程和问题反馈模板 |

编译产物、VS 缓存和个人数据由 .gitignore 排除。ZIP 下载包应上传到 Releases，不提交到源码历史。

## 界面设计参考

以下是 A 方案的概念图，用于说明布局方向；其中数字均为示例，不代表真实使用记录或当前程序的实际截图。

![A 风格界面概念](docs/images/ui-concept-A.png)

## 验证状态

现有 EXE 的本机构建日志记录了成功编译。发行包已进行 ZIP 回读、CRC 与 SHA-256 检查。GitHub Windows 编译已通过；响铃、托盘和休眠唤醒仍需实际使用验证。Windows CI 仅执行解决方案编译，不能替代实际响铃、睡眠唤醒和桌面交互测试。

## 反馈与许可

通过 Issues 描述复现步骤、操作系统版本和预期行为。分享日志前移除个人信息，不提交自己的闹钟或历史 XML。

源码许可证尚未由维护者选择；当前不标注为 MIT 等已授权开源项目。

## English quick start

Download a ZIP from Releases when available. Extract the portable ZIP and run AlarmApp.exe, Requires Windows desktop and .NET Framework 4.8+. The app works independently of Codex and supports English/Simplified Chinese, scheduled/interval reminders, WAV/MP3 sounds and separate monthly/daily history views.

## 世界时钟与计时

世界时钟可添加、移除系统支持的时区，自动按该时区显示日期、时间和夏令时。安卓以城市/时区 ID 搜索，Windows 以系统时区名称选择；选择列表保存在本地。

计时页包含秒表与倒计时，均支持开始、暂停、继续和重置。安卓倒计时复用精确闹钟服务，退到桌面后仍可提醒，并在闹钟列表中显示；暂停后调整分钟数会重新设定时长。Windows 倒计时可在托盘中继续，完全退出应用后不会响铃；Windows 秒表和倒计时状态仅保留在当前应用会话中。安卓秒表记录经过时间，手动修改系统时间会影响计数。

2026-10-12 新增功能已通过完整构建：Windows 自检通过，安卓10项测试通过、lint无错误。界面与新增计时功能仍需实机验收。

## Android 开发

用 Android Studio 打开 android/，配置 JDK 17、SDK 35、Build Tools 35.0.0。在该目录运行 `gradlew.bat testDebugUnitTest lintDebug assembleDebug`，或执行 BuildAndroid.cmd。本机工具链、缓存、local.properties、签名密钥和 APK 均不提交。

项目地址：https://github.com/lmynb68/Alarm-app 。感谢收藏！
