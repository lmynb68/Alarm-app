# 发布说明

公开仓库：https://github.com/lmynb68/Alarm-app

先按 VISUAL_STUDIO.md 在 VS 中提交并推送源码，再完成 TESTING.md 的本机验证。

推荐在 GitHub 网页的 Releases → Draft a new release 创建 v1.0.0，选择 main 分支，上传 artifacts 中的 AlarmApp-1.0.0-Portable.zip、AlarmApp-1.0.0-Setup.zip 和 SHA256SUMS.txt；说明使用 GITHUB_RELEASE.md。发布后实际下载一次并检查文件。

也可使用 PublishGitHub.cmd，需要已安装 GitHub CLI。默认账户 lmynb68、仓库 Alarm-app；脚本检查 README 标记，保留已有项目 README，并拒绝覆盖已有 v1.0.0。首次发布前必须先推送 README。

Build.cmd 在 Windows 上重新编译、执行自检后生成 artifacts。现有包来自先前成功编译的程序；自动构建、本机自检、安装与卸载的验证范围详见 TESTING.md 和 VALIDATION.json。
