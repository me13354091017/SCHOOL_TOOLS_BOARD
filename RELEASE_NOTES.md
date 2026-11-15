# SchoolToolsBoard（STB）课堂白板 v1.1.3

面向课堂触控一体机的开源白板，**主应用程序 = 桌面版**（以 [ICC-CE](https://github.com/InkCanvasForClass/community) 源码为底座 fork 改造），另有配套 Web 版。GPL-3.0。本应用是 [SCHOOL_TOOLS](https://me13354091017.github.io/school_tools/) 旗下应用。

## 本版新增：EXE 安装包

不再只能“解压 zip”，现在提供 **Windows 安装向导**。

| 文件 | 说明 |
|---|---|
| `SchoolToolsBoard-Setup-1.1.3.exe` | **推荐**。双击即入安装向导：可选安装目录、建开始菜单与桌面快捷方式、带卸载项；内置全部主程序 + `ppt-agent/` PPT 批注插件 |
| `SchoolToolsBoard-STB-Desktop-1.1.3.zip` | 免安装版（绿色包），适合机房批量拷、不想写注册表的场景 |
| `SchoolToolsBoard-Web-1.1.3.html` | 配套 Web 离线单文件版，约 176 KB |
| `LICENSE.txt` | GPL-3.0 完整原文 |

在线上机版：<https://me13354091017.github.io/SCHOOL_TOOLS_BOARD/>

## 安装包是怎么打出来的

安装包与构建解耦，专门放在 fork 仓库的 `stb-installer.yml` 里：

1. 从 `stb-latest` 拉最新 `SchoolToolsBoard-STB-win-x64.zip` 并解包；
2. 在 windows-latest 上装 Inno Setup 6，用 `stb/setup.iss` 编译：LZMA2 最高压缩、`x64compatible`、`PrivilegesRequired=lowest`（不强制提权，装到当前用户）；
3. 产物上传回 `stb-latest`，主仓库发布时通过 GitHub API 自动发现 `*Setup*.exe` 并挂到 Release。

好处：以后主程序重新构建时，只要重跑安装包工作流，就能拿到对应新版本安装包，不必改 iss。

## 主应用能力

- **PPT 放映批注**：PowerPoint 放映中用笔书写，按幻灯片逐页墨迹保存，翻页墨迹跟着走（VSTO 加载项 `ppt-agent` + 管道通信）
- **多页白板 + 时间机器**：上百页滑页、无限步撤销/重做、页缩略图
- **手势与笔压**：双指缩放 / 旋转 / 平移，笔压参与笔宽，手掌防误触
- **形状与学科工具**：直线 / 矩形 / 椭圆 / 箭头 / 抛物线、坐标系与函数图像、计时器、聚光灯

### 品牌化方式（可长期跟上游升级）

源码基座在 fork 仓库 <https://github.com/me13354091017/community>（默认分支 `net10`），**不改上游任何一行源码**，全部品牌改动在 CI 构建前打补丁：程序名 → `SchoolToolsBoard`、窗口标题 → 「School Tools Board · 课堂白板」、图标由 `stb/icon.svg` 现场生成。

## 安装与升级

- 首次安装：运行 `SchoolToolsBoard-Setup-1.1.3.exe`，一路下一步即可。
- 从 v1.1.2 或更早升级：直接跑新安装包覆盖安装，或解压 zip 覆盖旧目录，配置与课件不受影响。
- 前置：Windows 10 1809+ / 11 x64；PPT 批注需本机安装 PowerPoint（安装包不捆绑 .NET 与 Office）。

## 已知限制

- 安装向导为英文界面（Inno Setup 官方未带简体中文语言包）；安装后的软件本身为中文界面。
- 安装包未做数字签名（CI 自签证书仅用于 PPT 加载项清单）；Windows 可能提示“未知发布者”，点“仍要运行”即可。
- 仍未覆盖：云账号与课件库、协同备课、微课录制、课堂活动（分组竞赛/抢答器）、截图提问、教材资源打通。

## 许可

GPL-3.0-or-later。上游 ICC-CE 作者 CJKmkp 及更早血统（WXRIW/Ink-Canvas → ChangSakura/Ink-Canvas → InkCanvasForClass → ICC-CE）在此一并致谢。
