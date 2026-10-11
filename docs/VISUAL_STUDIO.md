# 通过 Visual Studio Code 上传

本文中的 VS 指 Visual Studio Code。

在 VS Code 中打开仓库文件夹，使用源代码管理查看差异、提交并推送 main 分支。目标 origin 为 https://github.com/lmynb68/Alarm-app.git。不要选择上级 Codex 文件夹作为仓库，也不要强制推送覆盖远端。

Windows 构建在终端运行 Build.cmd；AlarmApp.sln 同时保留供 Microsoft Visual Studio 使用。

## 发布便携下载

源码推送不会自动发布下载包。完成 TESTING.md 验收后，在 GitHub Releases 创建 v1.0.0，上传 AlarmApp-1.0.0-Portable.zip 和 SHA256SUMS.txt。说明使用 GITHUB_RELEASE.md。目前仅提供便携版，无需安装。
