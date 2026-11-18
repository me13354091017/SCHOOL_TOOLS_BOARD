# SchoolToolsBoard (STB) v1.0.1.8

> 底座：Ink Canvas Plus（clover-tree/Ink-Canvas-Plus，GPL-3.0）｜发行者：周之尧

## 本版重点

### 1. 板中板调橡皮大小不再卡顿
拖动橡皮大小滑杆时只更新数值与文案，停手 160ms 后再重建一次 `EraserShape`；同时只在整数档位真正变化时才处理。以前每拖一格就重建一次形状，拖到 60 像素时会明显顿。

### 2. 欢迎 / 更新弹窗以后自动跟版本
应用内的更新弹窗（欢迎页）文案以前是写死的，会一直显示旧版本说明。现在它的唯一来源是 `stb-plus/extra/welcome.txt`：构建时生成 `StbWelcomeContent.cs` 并注入 `WelcomeWindow`，每次发版只要改 `welcome.txt` 就能同步，再也不会版本号对不上。

本轮已把弹窗内容换成 1.0.1.8 的实际更新（板中板橡皮、PPT 模式能力、调色盘修复等）。

## 保留能力
- 板中板「橡皮」单击即弹滑杆（4–60）；PPT / 批注模式也有调色盘粗细与橡皮大小
- 调色盘选色为自定义色，不再回写成黑笔
- 墨迹目录 `文档\SchoolToolsBoard\Strokes`，截图目录 `图片\SchoolToolsBoard\Screenshots`
- 浮动栏「保存」旁有「保存到其它」

## 安装说明
- 覆盖安装即可，安装目录沿用 `Program Files\SCHOOL_TOOLS\SchoolToolsBoard`。
- 便携版解压即用。

## 开源
- 上游：https://github.com/clover-tree/Ink-Canvas-Plus （GPL-3.0）
- 本项目：https://github.com/me13354091017/SCHOOL_TOOLS_BOARD
