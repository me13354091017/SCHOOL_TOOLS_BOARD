# SchoolToolsBoard（STB）课堂白板 v1.0.0.0

本次把桌面主应用底座从 **ICC-CE（Ink Canvas for Class）换成 Ink Canvas Plus（IC+）**，版本号全面重置为 1.0.0.0，取代 ICC-CE 版成为正式主应用。

## 本版变更

- **底座**：`clover-tree/Ink-Canvas-Plus`（GPL-3.0，WPF / .NET Framework 4.7.2），上游源码不改，构建前打补丁
- **品牌**：程序名 `SchoolToolsBoard`、窗口标题「School Tools Board 课堂白板」、公司 `SCHOOL_TOOLS`、图标换为母站 STB 图标
- **STB 主题**：新增 `Resources/Styles/STBLight.xaml` / `STBDark.xaml`（象牙白 / 石墨），启动默认合并 STB 浅色，`Settings.Appearance.Theme` 默认改为 18（STB + 浅色），主题切换表新增第 7 项
- **黑板界面**：黑板底 `#1F1F1F → #101A16`、白板底 `#FFF2F2F2 → #FFF7F8F6`，全量替换取色常量与覆盖层
- **安装包**：换用新 AppId，安装目录 `SCHOOL_TOOLS\SchoolToolsBoard`，产物 `SchoolToolsBoard-Setup-1.0.0.0.exe`

## 与 ICC-CE 版的差异（务必知悉）

- IC+ **没有板中板**（MiniWhiteboard），本版未提供该能力
- PPT 批注走 IC+ 自带的 Office Interop 实现，不再需要独立加载项（ppt-agent）
- 时间机器（TimeMachine）为 IC+ 内建，行为与 ICC-CE 版一致
- 旧 ICC-CE 版设置（`%APPDATA%\Ink Canvas`）不会自动迁移

## 运行环境

- Windows 10 1903 及以上；Windows 7 需自备 .NET Framework 4.7.2
- 触控一体机建议以 x64 运行
