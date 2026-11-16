# SchoolToolsBoard（STB）课堂白板 v1.0.0.2

桌面主应用底座为 **Ink Canvas Plus（IC+）**；1.0.0.2 在 1.0.0.1 基础上给板中板加上自由拖拽缩放。

## 本版变更

- **版本号**：1.0.0.1 → 1.0.0.2（已装 1.0.0.0 / 1.0.0.1 的机器可通过程序内自动更新收到本版）
- **板中板可拖拽缩放**：窗口四边、四角八个方向都可拉住缩放（拖动边、光标自动变 SizeWE/SizeNS/SizeNWSE/SizeNESW），最小 280×220；标题栏仍可拖动整体移动，小/中/大三档快捷尺寸保留
- **板中板（1.0.0.1 新增）**：`MiniBoardWindow` 浮层白板，笔色黑/红/蓝/白、细中粗三档、橡皮、清空、白板↔黑板底色切换；入口在主界面「工具」组和左侧主工具栏
- **自动更新**：`CheckForUpdate()` 已指向本仓库 Release 的 `version.xml`（`releases/latest/download/version.xml`）
- **品牌**：程序名 `SchoolToolsBoard`、窗口标题「School Tools Board 课堂白板」、公司 `SCHOOL_TOOLS`、图标为母站 STB 图标
- **STB 主题**：`STBLight.xaml` / `STBDark.xaml`（象牙白 / 石墨），启动默认 STB 浅色，设置页「颜色主题」新增 STB 三项
- **黑板 / 白板底色**：`#1F1F1F → #101A16`、`#FFF2F2F2 → #FFF7F8F6`
- **欢迎界面**：整页重写为 STB 口径，头像换 STB 图标
- **设置界面**：面板底色 `#FBFAF7` / 描边 `#B9B09D`
- **安装包**：AppId `{{7E4A1C3B-...}}`，装到 `SCHOOL_TOOLS\SchoolToolsBoard`，产物 `SchoolToolsBoard-Setup-1.0.0.2.exe`

## 已知差异

- 板中板是**独立浮层**实现，**未与主画板共用笔迹/时光机/笔色状态**，也不能把板中板内容一键投回主板；与希沃完全一致的联动版尚未做
- PPT 批注走 IC+ 自带 Office Interop，不再需要独立加载项；时间机器为 IC+ 内建
- 旧 ICC-CE 版设置（`%APPDATA%\Ink Canvas`）不会自动迁移；需要 ICC-CE 完整板中板请用 [v1.1.5 归档版](https://github.com/me13354091017/SCHOOL_TOOLS_BOARD/releases/tag/v1.1.5)

## 运行环境

- Windows 10 1903 及以上；Windows 7 需自备 .NET Framework 4.7.2
