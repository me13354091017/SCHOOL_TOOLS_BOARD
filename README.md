# SchoolToolsBoard（STB）课堂白板

> 面向教室大屏与触控一体机的免费白板软件，按「希沃白板」的使用习惯重做交互，底座为开源项目 Ink Canvas Plus。

![平台](https://img.shields.io/badge/Windows-10%2F11-2C6BED) ![底座](https://img.shields.io/badge/based%20on-Ink%20Canvas%20Plus-1F6FC4) ![许可](https://img.shields.io/badge/license-GPL--3.0-2FA84F)

## 这是什么

**SchoolToolsBoard（简称 STB）** 是 [SCHOOL_TOOLS](https://me13354091017.github.io/school_tools/) 系列里的课堂白板应用，专为「上课时随手就能写」而做：不用装驱动，不依赖 Office 插件，打开就能在 PPT、PDF、网页、桌面上直接批注、圈画、板书，并且能把整堂课的墨迹与截图留档。

它的底座是开源项目 **Ink Canvas Plus**（GPL-3.0）。STB 不改动上游源码本体，所有改动都在构建前由 `stb-plus/*.ps1` 打补丁完成，因此上游每次更新都能干净地合并进来。

- 仓库名：`SCHOOL_TOOLS_BOARD`
- 应用名：`SchoolToolsBoard`
- 图标标识：`STB`
- 发行者：周之尧

## 功能

**书写与批注**
- 笔 / 荧光笔 / 橡皮（点擦与整笔擦）/ 选择 / 手指拖动，一键切换
- 24 色调色板：黑、白、红、橙、黄、绿、青、蓝、紫、粉、棕、灰等基础色，加深蓝、天蓝、薄荷、草绿、橄榄、杏色、珊瑚、玫瑰、酒红、靛蓝、银灰、炭灰
- 12 套主题色：象牙白、石墨、海盐蓝、薄荷绿、暖阳黄、蜜桃橙、樱花粉、薰衣草紫、青碧、珊瑚红、抹茶、雾灰；主浮动栏、快捷设置、调色板、板中板工具条与函数窗统一换色
- 画笔粗细统一走主浮动栏「快捷设置」滑杆，调色板与板中板不再各带一条重复滑杆
- 橡皮大小既能按 7 档调，也能拖 4–60 像素滑杆；板中板调色板与主浮动栏颜色保持同步
- 直线、矩形、圆等图形绘制，以及撤销 / 重做

**课堂场景**
- 批注模式：浮在 PPT、WPS、PDF、浏览器之上直接写，翻页自动跟随
- 黑板模式：整屏纯色板书，深浅主题切换
- **板中板**：可拖动、可缩放的悬浮小画板，带标题拖拽条与右下拖拽角，支持最大化与折叠
- 组件系统：时钟、倒计时、随机点名等课堂小工具
- 截图、保存墨迹、保存到其它位置

**系统集成**
- 开机自启、单实例、托盘常驻
- 边缘手势屏蔽，避免从屏幕边缘起笔被系统手势打断
- 自动检查更新（读取本仓库 Release 的 `version.xml`）

## 下载与安装

到 **Releases** 页下载最新版本：<https://github.com/me13354091017/SCHOOL_TOOLS_BOARD/releases>

每个版本提供两个包：

| 文件 | 用途 |
| --- | --- |
| `SchoolToolsBoard-Setup-x.y.z.w.exe` | 安装包，安装到 `Program Files\SCHOOL_TOOLS\SchoolToolsBoard`，带开始菜单与桌面快捷方式，支持覆盖升级 |
| `SchoolToolsBoard-Plus-Desktop-x.y.z.w.zip` | 便携版，解压即用，适合拷到 U 盘带去教室 |

系统要求：Windows 10 / 11（x64），.NET Framework 4.7.2 及以上（Win10 自带），建议 4GB 内存以上，触控一体机体验最佳。

> 首次启动若被 SmartScreen 拦下，点「更多信息 → 仍要运行」；这是未购买代码签名证书的常见提示，不是文件损坏。

## 更新说明

- **自动检查更新**：设置 → 关于 → 应用更新 → 「检查更新」，读取 `https://github.com/me13354091017/SCHOOL_TOOLS_BOARD/releases/latest/download/version.xml`，有新版本会提示并给出安装包下载地址。
- **欢迎 / 更新弹窗**：每次发版内容由 `stb-plus/extra/welcome.txt` 生成，与版本号同步。
- **手动更新**：直接下载新版本安装包覆盖安装即可，设置与墨迹文件都会保留。
- **版本号规则**：`主版本.次版本.修订.构建`，课堂可用版为 `1.0.x.x`。

### 当前版本 1.0.2.7「浮窗拖动不消失」

- 修复“点开笑脸菜单不关，拖动主界面浮动栏后浮动栏消失、进程还在”：拖动改用 Win32 屏幕坐标，避免 Popup 抢走鼠标后把浮动栏算到屏幕外
- 拖动位移改为“当前鼠标位置 - 按下时位置”，每次移动和松手都把浮动栏夹回屏幕可见区域
- 保留 1.0.2.6 的 40ms 左键状态看门狗，Popup 吃掉 MouseUp 时也会强制收起透明遮罩
- 补全异常日志：`LogHelper.NewLog(Exception)` 会写入 Logs 日志

### 1.0.2.0「多彩的颜色」

- 调色板从 12 色扩到 24 色，新增深蓝、天蓝、薄荷、草绿、橄榄、杏色、珊瑚、玫瑰、酒红、靛蓝、银灰、炭灰
- 设置页新增 12 套主题色；主浮动栏、快捷设置、调色板、板中板浮动工具条、函数窗统一换色，函数坐标图背景保持白色
- 调色板与板中板不再各带一条画笔粗细滑杆，统一使用主浮动栏「快捷设置」的滑杆
- 快捷设置面板靠屏幕右缘时自动向左内收，修复右侧被切掉、圆角变直角的问题
- 应用内更新弹窗的说明文案随每次发版同步更新

历史版本日志见 [Releases](https://github.com/me13354091017/SCHOOL_TOOLS_BOARD/releases)。

## 与上游的关系

- 上游：<https://github.com/clover-tree/Ink-Canvas-Plus>（GPL-3.0，作者 Clover Yan / XY Wang）
- 本项目遵循 GPL-3.0，衍生分发需同样开源
- 所有 STB 改动集中在 `stb-plus/`（补丁脚本与 `extra/` 覆盖文件），CI 顺序：上游源码 → 品牌补丁 → 板中板 → 外观 → 修正 → 欢迎页 → 编译 → 打包 → 发布

## 反馈

- 提 Issue：<https://github.com/me13354091017/SCHOOL_TOOLS_BOARD/issues>
- 母站（更多教学工具）：<https://me13354091017.github.io/school_tools/>
