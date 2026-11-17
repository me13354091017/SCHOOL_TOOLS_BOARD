# stb-plus：SchoolToolsBoard 的 IC+ 补丁线

本目录不修改上游戏源码本体。CI 在构建前依次执行下面的脚本，把 STB 品牌与功能打到 Ink Canvas Plus 的克隆上；上游升版时只需核对锦点。

## 执行顺序

1. `apply-stb-plus-patches.ps1` — PHASE A~J：品牌名/版本号、主窗口标题与黑板底色、App.xaml 默认主题、全局取色与文案、STB 主题字典、Inno Setup 安装包、全量链接替换、AutoUpdater 域名修正、欢迎页重写、设置页主题下拉、面板底色。
2. `apply-stb-miniboard.ps1` — PHASE K：拷入板中板三个文件，修正 `CheckForUpdate()` 的更新地址，在主界面与左侧竖栏插入「板中板」入口。
3. `apply-stb-settings.ps1` — PHASE L：拷入 `StbSettingsWindow`，把 `SetTheme`/`CheckForUpdate` 放宽为 public，接管 `BtnSettings_Click`。
4. `apply-stb-floatbar.ps1` — PHASE M：浮动栏与左侧竖栏的「设置」「板中板」改为图标在上、文字在下。

链路靠脚本尾部相互调用串起：miniboard → settings → floatbar，workflow 只显式调用前两个。

## 新增文件

- `extra/MiniBoardWindow.xaml(.cs)` — 板中板浮层，外观与黑板一致（`#101A16` 底、白笔、深色浮动工具条），可拖动、四边四角缩放。
- `extra/MainWindow.MiniBoard.cs` — 板中板入口处理器。
- `extra/StbSettingsWindow.xaml(.cs)` — 整页设置窗口，7 项左导航 + 自绘卡片。
- `Styles/STBLight.xaml`、`STBDark.xaml` — STB 主题字典。

## 发版

1. 改根目录 `VERSION_PLUS`（仅版本号一行）与 `RELEASE_NOTES_PLUS.md`。
2. 推送后自动触发 `Release Plus` 流水线：构建 → 打包 zip → Inno Setup → 生成 `version.xml` → 发布/覆盖同名 tag。
3. 产物命名固定：`SchoolToolsBoard-Setup-<版本>.exe`、`SchoolToolsBoard-Plus-Desktop-<版本>.zip`。

## 约束

- 每份脚本都带 `Assert`，锦点失配会直接构建失败，不会静默产出一个破包。
- 上游 `InkCanvasPlus.csproj` 是 SDK 风格，新增 xaml/cs 自动入编，不用改工程文件。
- iNKORE 依赖保持上游的 0.9.25.1，不升级；新 UI 一律用自绘控件，不依赖 `SettingsCard`。
