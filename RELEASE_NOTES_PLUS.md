# SchoolToolsBoard（STB）课堂白板 v1.0.0.1

桌面主应用底座为 **Ink Canvas Plus（IC+）**；1.0.0.1 在 1.0.0.0 基础上修好自动更新链路并新增板中板。

## 本版变更

- **版本号**：1.0.0.0 → 1.0.0.1（旧版 1.0.0.0 已装机器可通过程序内自动更新收到本版）
- **板中板（新增）**：`MiniBoardWindow`——叠在主画面上方的无边框小白板，可拖动、笔色黑/红/蓝/白、细中粗三档、橡皮、清空、白板↔黑板底色切换、小/中/大三档尺寸；入口在主界面「工具」组和左侧主工具栏
- **自动更新（修正）**：`CheckForUpdate()` 原指向 `dl.inkcanvasplus.khyan.top/autoupdate.xml`（插值字符串，旧补丁正则没命中），现改为本仓库 Release 的 `version.xml`（`releases/latest/download/version.xml`）；代码与文档里残留的上游更新域名一并清理
- **品牌**：程序名 `SchoolToolsBoard`、窗口标题「School Tools Board 课堂白板」、公司 `SCHOOL_TOOLS`、图标换为母站 STB 图标
- **STB 主题**：`Resources/Styles/STBLight.xaml` / `STBDark.xaml`（象牙白 / 石墨），启动默认合并 STB 浅色，`Settings.Appearance.Theme` 默认 18，设置页「颜色主题」新增 STB 浅色 / 深色 / 跟随系统三项
- **黑板界面**：黑板底 `#1F1F1F → #101A16`、白板底 `#FFF2F2F2 → #FFF7F8F6`，全量替换取色常量与覆盖层
- **欢迎界面**：整页重写——标题、版本文案、更新内容全部换成 STB 口径，头像换 STB 图标（隐藏原作者 logo）
- **设置界面**：面板底色换 `#FBFAF7` / 描边 `#B9B09D`，主题下拉新增 STB 三项
- **安装包**：新 AppId，装到 `SCHOOL_TOOLS\SchoolToolsBoard`，产物 `SchoolToolsBoard-Setup-1.0.0.1.exe`

## 已知差异

- 本版板中板是**独立浮层**实现（自由书写 / 清屏 / 换底色 / 三档尺寸），**未与主画板的笔迹、时光机、笔色状态互通**，也不能把板中板内容一键投回主板；与希沃完全一致的联动版尚未做
- PPT 批注走 IC+ 自带的 Office Interop 实现，不再需要独立加载项（ppt-agent）
- 时间机器（TimeMachine）为 IC+ 内建
- 旧 ICC-CE 版设置（`%APPDATA%\Ink Canvas`）不会自动迁移；需要 ICC-CE 那套完整板中板请用 [v1.1.5 归档版](https://github.com/me13354091017/SCHOOL_TOOLS_BOARD/releases/tag/v1.1.5)

## 运行环境

- Windows 10 1903 及以上；Windows 7 需自备 .NET Framework 4.7.2
