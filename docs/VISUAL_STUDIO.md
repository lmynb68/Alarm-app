# 通过 Visual Studio 上传

目标仓库：https://github.com/lmynb68/Alarm-app.git

1. 在 Visual Studio 中打开本项目的 AlarmApp.sln。
2. 选择 Git → 创建 Git 存储库。如果 VS 已创建了本地仓库，跳过这一步。选择“仅本地”，存储库路径必须是包含 README.md 和 AlarmApp.sln 的 AlarmApp-Windows 目录，避免选择上级 Codex 目录。
3. 在 Git 存储库设置中添加远程 origin，URL 填写 https://github.com/lmynb68/Alarm-app.git。已存在 origin 时先检查其地址。
4. 在 Git 更改窗口检查待提交文件。应包含源码、assets、installer、tools、docs 和 .github；artifacts、bin、obj、.vs、用户闹钟和历史数据不应提交。
5. 将本地分支命名为 main，填写提交说明，例如 Initial Windows alarm app，然后选择“全部提交”。
6. 选择 Git → 推送，按 VS 提示登录 GitHub。完成后打开仓库网页，确认 README 和源码已经显示。

当前环境无法控制 VS 界面；文档准备完成不代表已推送。仓库远端若出现了新的提交，先获取并检查，不要强制推送覆盖。

## 发布下载

源代码推送不会自动生成下载版本。先按 TESTING.md 验证便携版和安装版，再在 GitHub Releases 创建 v1.0.0，上传 artifacts 中的 Portable.zip、Setup.zip 和 SHA256SUMS.txt。Release 说明可使用 GITHUB_RELEASE.md。

安装包目前是含 Install.cmd 的 ZIP，不是 MSI 或单文件 Setup.exe。
