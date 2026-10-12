# 闹钟 · Alarm

支持 Windows 和 Android 的闹钟应用，包含定时提醒、间隔提醒、世界时钟、秒表、倒计时和闹钟使用记录。可独立运行，无需账号。

[下载版本](https://github.com/lmynb68/Alarm-app/releases) · [反馈问题](https://github.com/lmynb68/Alarm-app/issues)

## 功能

- 闹钟：单次或按星期重复，设置精确到秒，可直接填入当前时间。
- 间隔提醒：自定义间隔与总次数，例如每5分钟提醒一次，共12次；第一次在一个间隔后响铃。
- 铃声：5种内置声音，支持 WAV、MP3 自定义铃声、试听和应用内音量调节。
- 世界时钟：添加、移除时区，查看当地日期与时间。安卓支持按城市/时区英文名称搜索，Windows 使用系统时区列表。
- 秒表：显示累计分钟、秒和毫秒，支持开始、暂停、继续和重置。
- 倒计时：按分钟设置时长，支持暂停、继续、重置，到时响铃。
- 使用记录：简要页按月统计，详细页按日查询闹钟名称、类型、使用时段和响铃时长。
- 语言：简体中文和 English，可在应用内切换。

## 下载与运行

仓库保存源码。可下载文件以 [Releases 页面的附件](https://github.com/lmynb68/Alarm-app/releases)为准；源码更新不代表安装包已同步更新。

| 平台 | 环境 | 使用方式 |
| --- | --- | --- |
| Windows | Windows 10/11，.NET Framework 4.8 | 下载便携 ZIP，完整解压后运行 AlarmApp.exe，保留铃声与图标资源。 |
| Android | Android 8.0 及以上 | 下载 APK 并安装，根据首页提示授予闹钟、通知等权限。 |

Windows 当前采用便携版，无需安装，不自动设置开机启动。关闭窗口后在托盘运行；退出程序、注销或关机后不会响铃。睡眠唤醒依赖设备与电源设置。更新前请从托盘退出旧程序。

安卓通过系统闹钟调度，关闭应用页面不会取消启用的提醒。关机不响铃；系统强行停止应用后需要重新打开。部分设备需要允许后台运行。安卓倒计时显示在闹钟列表中，使用已有响铃界面。当前构建的 APK 为调试签名，正式发布签名尚待配置。

音量设置范围为0–100%，实际声音同时受系统音量影响。Windows 秒表与倒计时状态仅保留在当前运行会话中，退出程序后不恢复。

## 数据和统计

- Windows：数据保存在 %LOCALAPPDATA%/IndependentAlarm。删除解压目录不会删除该处设置、铃声与历史。
- Android：数据保存在应用私有目录。卸载应用或清除数据会删除设置、导入铃声与历史。
- 两端数据独立保存，当前没有同步功能。

统计中的“使用时长”指闹钟启用会话，**不是秒表计时记录或应用屏幕使用时间**。Windows 统计启用且程序运行的时间；安卓统计保持启用的经过时间，关闭页面、关机或强行停止期间也可能纳入该会话。

多个闹钟分别累计，跨日、跨月按查询区间裁剪。会话数是该期间有启用时长的会话数，试听不计入记录，删除闹钟保留历史。Windows 倒计时不计入闹钟历史；安卓倒计时作为单次定时闹钟记录。历史从该功能启用后产生，不补录过去的数据。

## 从源码构建

### Windows

在 Windows 下双击仓库根目录的 Build.cmd。脚本编译程序、运行自检，成功后在 artifacts/ 生成便携 ZIP 和 SHA256SUMS.txt。需要 .NET Framework 4.8 环境。

项目包含 AlarmApp.sln，可使用 Microsoft Visual Studio 构建；VS Code 可用于编辑源码和运行构建命令。

### Android

用 Android Studio 打开 android/，配置 JDK 17、Android SDK Platform 35 和 Build Tools 35.0.0。在该目录执行：

```powershell
.\gradlew.bat testDebugUnitTest lintDebug assembleDebug
```

APK 位于 android/app/build/outputs/apk/debug/app-debug.apk。详情见 [安卓说明](android/README.md)。本机 SDK 路径、缓存、构建产物和签名密钥不应提交。

## 项目结构

| 路径 | 内容 |
| --- | --- |
| src/AlarmApp/ | Windows C# 项目 |
| android/ | Android 项目、Gradle Wrapper 和单元测试 |
| assets/ | Windows 图标和内置铃声 |
| tools/ | Windows 构建与发布脚本 |
| docs/ | Windows 使用与验收说明 |
| .github/ | 工作流和问题反馈模板 |

## 验证状态

2026-10-12 本机构建已通过：Windows 编译及程序自检成功；安卓10项时间规则单元测试通过，lint无错误，APK已生成。单元测试不等同于界面或实际响铃验收，音量调节、倒计时、后台与锁屏行为仍需按设备实测。

[Windows 验收清单](docs/TESTING.md) · [Android 验收清单](android/TESTING.md)

## 反馈与许可

反馈时请说明平台、系统版本、复现步骤和预期结果。日志中请移除个人信息，不上传自己的闹钟数据或签名密钥。仓库尚未指定源码许可证。

项目地址：[lmynb68/Alarm-app](https://github.com/lmynb68/Alarm-app)。感谢收藏！
