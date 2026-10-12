# 实时闹钟 Android

Windows 项目旁的原生 Android 子项目。Android 8.0（API 26）及以上，Java 17，无运行时网络权限、账号或 Codex 依赖。

## 功能

- 定时到秒；按星期重复，未选星期为单次；默认填当前时间，并可再次读取当前时间。
- 间隔 1–1440 分钟、1–10000 次，从保存/启用时重新计数；例如每 5 分钟，共 12 次。
- 名称、启用/停用、编辑、删除。五种内置铃声及 WAV/MP3 导入（每文件最多 20 MB）。
- 铃声复制至应用私有目录，原文件移动后仍可使用；试听 10 秒后停止，可手动停止。
- 精确 AlarmManager 调度、响铃前台服务、锁屏通知、全屏响铃页、停止和 5 分钟后提醒。
- 同时到点的提醒保留各自记录和停止操作；音频播放最近触发的铃声，避免多音频混响。
- 简体中文 / English，可在应用内切换并保存。
- 简要页按月/类型统计；详细页按日显示名称、类型、启用或响铃时段与时长。可选择年份和日期。
- 设备重启并首次解锁后、更新后恢复调度；调整系统时间/时区后重算定时闹钟。

## 构建与运行

用 Android Studio 打开本 android 目录（不是 Windows 的 .sln），同步 Gradle。使用 JDK 17，安装 Android SDK Platform 35 和 Build Tools 35.0.0。项目自带 Gradle 8.11.1 Wrapper，JAR 已与官方 SHA-256 对照，发行 ZIP 校验值也已固定。首次同步需要网络下载 Google/Gradle/Maven 官方依赖。

连接手机并开启开发者调试，选择 app → Run。也可在 Windows 双击 BuildAndroid.cmd，执行单元测试、lint 和 debug APK 构建。命令行：

```text
gradlew.bat testDebugUnitTest lintDebug assembleDebug
```

SDK 路径由 Android Studio 写入 local.properties，或自行设置 ANDROID_HOME；该文件不要提交。APK 原路径 app/build/outputs/apk/debug/app-debug.apk，脚本会复制到outputs/AlarmApp-Android-Release/AlarmApp-Android-1.0.0-Debug.apk，并生成 SHA256SUMS.txt（仅在测试与 lint 通过后）。

发布给普通用户请在 Android Studio 使用 Build → Generate Signed App Bundle or APK → APK，保存好自己的签名密钥，后续版本使用同一密钥。不要提交 .jks/.keystore、密钥密码或签名配置。2026-10-12 已生成与当前源码一致的调试 APK，可用于手机测试；正式签名发布另行进行。

## 手机首次设置

允许“闹钟和提醒”，Android 13+ 允许通知，Android 14+ 可允许全屏通知。首页会显示缺失权限和设置入口。全屏未获准时仍用通知打开响铃页；通知被禁用会影响锁屏操作。不要将应用设为系统“强行停止”；部分手机还需要在电池设置中允许后台运行。

关机不会响铃。重启恢复发生在首次解锁后；重启时错过的定时提醒不补响，间隔提醒从恢复后一个间隔开始，不补造响铃次数。手动强行停止后，必须再次打开应用恢复调度。

## 统计口径与数据

启用时长是保持启用的经过时间（含后台，不是看屏幕时长），各闹钟独立累计。它与 Windows 必须保持进程运行的口径不同；手机关闭或强行停止期间仍会纳入启用经过时间，因此不能拿它衡量实际后台运行时长。定时单次/间隔最后一次到点时结束启用会话；响铃时长记录前台响铃服务存活期间。

跨日、跨月按所选期间裁剪；使用会话数为该期间有启用时长的会话数，详细页“该会话总响铃次数”是完整会话累计，并非所选日的次数。历史使用当前设备时区显示；改时区或系统时间会改变归属与经过时间。

历史保存为应用私有目录 alarms.json，AtomicFile 写入；响铃每 30 秒保存检查点，进程被系统杀死后以最后检查点结束，最多可能少记 30 秒。启用会话继续保留。试听不计历史；删除闹钟保留其历史。系统卸载或清除数据会删除历史和导入铃声，当前未做跨设备同步或备份。

## 验证状态

2026-10-12 完整 Gradle 构建成功：10 项单元测试通过，0 失败；lint 无错误，5 项警告；APK 打包成功且与当前源码哈希一致。测试 APK 883834 字节，位于 outputs/AlarmApp-Android-Release。手机安装、锁屏及后台响铃尚未验证。详见 TESTING.md 和 VALIDATION.json。

## 实现依据

- [Android 精确闹钟](https://developer.android.com/develop/background-work/services/alarms)
- [Android 前台服务类型](https://developer.android.com/develop/background-work/services/fgs/service-types)
- [全屏通知权限](https://developer.android.com/about/versions/14/behavior-changes-14#secure-fsi)
- [AGP 8.9 的 JDK/Gradle 要求](https://developer.android.com/build/releases/agp-8-9-0-release-notes)
- [Gradle 官方校验](https://gradle.org/release-checksums/)

## 本机验证入口

双击 outputs/BuildAndroid.cmd 或本目录 BuildAndroid.cmd / VerifyAndroid.cmd。入口统一执行 Gradle 的 testDebugUnitTest、lintDebug、assembleDebug，读取真实 JUnit 报告；全部通过后复制 APK 到 outputs/AlarmApp-Android-Release 并生成 SHA256 校验文件。

日志位于 verification/verify.log 和 native-build.log，阶段结果为 result.json。保持窗口开启直到显示最终结果。脚本记录构建源码哈希，若构建过程中源码变化，会拒绝交付该 APK。

本机入口自动选择 .toolchain 内的 JDK 17 和 SDK 35。其他电脑需配置 JDK 17、SDK 35、Build Tools 35.0.0；可通过 VerifyAndroid.ps1 的 -SdkHome 指定 SDK。工具链、Gradle 缓存、本机路径和日志不提交 Git。构建仅改变当前进程环境变量。

## 世界时钟与计时

世界时钟可添加、移除系统支持的时区，自动按该时区显示日期、时间和夏令时。安卓以城市/时区 ID 搜索，Windows 以系统时区名称选择；选择列表保存在本地。

计时页包含秒表与倒计时，均支持开始、暂停、继续和重置。安卓倒计时复用精确闹钟服务，退到桌面后仍可提醒，并在闹钟列表中显示；暂停后调整分钟数会重新设定时长。Windows 倒计时可在托盘中继续，完全退出应用后不会响铃；Windows 秒表和倒计时状态仅保留在当前应用会话中。安卓秒表记录经过时间，手动修改系统时间会影响计数。

2026-10-12 新增功能已通过完整构建：Windows 自检通过，安卓10项测试通过、lint无错误。界面与新增计时功能仍需实机验收。
