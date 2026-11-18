# SchoolToolsBoard (STB) v1.0.1.6

> 底座：Ink Canvas Plus（clover-tree/Ink-Canvas-Plus，GPL-3.0）｜发行者：周之尧

## 修复与新功能

### 1. 调色盘选色后真的变颜色了（上版仍写黑笔）
根因不在点击事件，而是上游的取色逻辑：`GetBaseColor()` 用一个 0–5 的颜色索引，`default` 分支直接 `return Colors.Black`。调色盘为了不让六个内置色点“打勾”，把索引置为 -1，结果每次 `ColorSwitchCheck()` 末尾的 `ApplyMarkerMode()` 又把颜色重设回黑色。

本版把 `default` 分支改为优先返回调色盘选定的自定义色（没选过就还是黑），并且先 `Clone()` 再整体赋值，避开 `DrawingAttributes` 被冻结时报异常的情况。选完色写字就是你选的颜色，荧光笔也会用同色的半透明版本。

### 2. 笔尖上那个蓝框没了
上版只改了 `Extras.cs`。但板中板还有另一套 `MiniBoardWindow.xaml.cs` 里的 MouseMove 逻辑，那才是实际在跑的，它照样把蓝色圈点亮。本版把那处一并关掉；同时两处圆环元素的 `Stroke` 改成透明，就算哪天被误点亮也看不到蓝色。

### 3. 保存目录改名
- 墨迹：`文档\SchoolToolsBoard\Strokes\User Saved`、`...\Strokes\Auto Saved\Presentations`
- 截图：`图片\SchoolToolsBoard\Screenshots`（开“按日期建子目录”时再加 `yyyyMMdd`）

旧目录下的历史文件不会被迁移也不会被删，需要的话手动拷过去。

### 4. 浮动栏新增「保存到其它」
就在「保存」图标右边，点击弹另存对话框（默认文件名 `SchoolToolsBoard-年月日-时分秒.icstk`），可以把当前墨迹直接存到任意目录（U 盘、共享盘），不影响默认目录。

### 5. 主页与更新文案重写
- 仓库主页 README 全部重写：项目定位、功能清单、两个安装包的区别、系统要求、自动/手动更新方式、与上游的关系
- 设置 → 关于 → 「应用更新」说明改为：检查更新读 GitHub Release 的 `version.xml`，有新版本可直接下载安装包，也可以去发布页手动下载

## 安装说明
- 覆盖安装即可，安装目录沿用 `Program Files\SCHOOL_TOOLS\SchoolToolsBoard`。
- 便携版解压即用。

## 开源
- 上游：https://github.com/clover-tree/Ink-Canvas-Plus （GPL-3.0）
- 本项目：https://github.com/me13354091017/SCHOOL_TOOLS_BOARD
