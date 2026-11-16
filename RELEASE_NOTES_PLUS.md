# SchoolToolsBoard（STB）课堂白板 v1.0.0.0

桌面主应用底座换为 **Ink Canvas Plus（IC+）**，版本全面重置为 1.0.0.0，取代 ICC-CE 版成为主应用。

## 本版变更

- **底座**：`clover-tree/Ink-Canvas-Plus`（GPL-3.0，WPF / .NET Framework 4.7.2），上游源码不改，构建前打补丁
- **板中板（新增）**：`MiniBoardWindow`——叠在主板面上的小白板窗口，可拖动、笔色黑/红/蓝/白、细中粗三档、橡皮、清空、白牌↔黑板底色切换、小/中/大三档尺寸；入口在主界面「工具」组和左侧主工具栏
- **自动更新（修正）**：`CheckForUpdate()` 原指向 `dl.inkcanvasplus.khyan.top/autoupdate.xml`，现改为本仓库 Release 的 `version.xml`（`releases/latest/download/version.xml`），并清理代码/文档里残留的该域名
- **品牌**：程序名 `SchoolToolsBoard`、窗口标题「School Tools Board 课堂白板」、公司 `SCHOOL_TOOLS`、图标换为母站 STB 图标
- **STB 主题**：新增 `Resources/Styles/STBLight.xaml` / `STBDark.xaml`（象牙白 / 石墨），启动默认合并 STB 浅色，`Settings.Appearance.Theme` 默认 18，设置页「颜色主题」新增 STB 浅色 / 深色 / 跟随系统三项
- **黑板界面**：黑板底 `#1F1F1F → #101A16`、白板底 `#FFF2F2F2 → #FFF7F8F6`，全量替换取色常量与覆盖层
- **欢迎界面**：整页重写——标题、版本文案、更新内容全部换成 STB 口径，头像换 STB 图标（隐藏原作者 logo），链接指向本仓库 Release 与母站
- **设置界面**：面板底色换 `#FBFAF7` / 描边 `#B9B09D`，主题下拉新增 STB 三项
- **链接清理**：程序内官网、更新日志、关于信息、README、Manual.md、privacy.txt 里的上游域名全部改指 SCHOOL_TOOLS 母站与本仓库
- **安装包**：新 AppId，装到 `SCHOOL_TOOLS\SchoolToolsBoard`，产物 `SchoolToolsBoard-Setup-1.0.0.0.exe`

## 与 ICC-CE 版的差异（务必知悉）

- 本版板中板为独立浮层窗口实现的轻量版（自由书写/清屏/换底色/三档尺寸），不是 ICC-CE 那套与主画布共用笔迹数据与时光机的完整板中板；需要原版请用 [v1.1.5 归档版](https://github.com/me13354091017/SCHOOL_TOOLS_BOARD/releases/tag/v1.1.5)
- PPT 批注走 IC+ 自带的 Office Interop 实现，不再需要独立加载项（ppt-agent）
- 时间机器（TimeMachine）为 IC+ 内建
- 旧 ICC-CE 版设置（`%APPDATA%\Ink Canvas`）不会自动迁移

## 运行环境

- Windows 10 1903 及以上；Windows 7 需自备 .NET Framework 4.7.2
