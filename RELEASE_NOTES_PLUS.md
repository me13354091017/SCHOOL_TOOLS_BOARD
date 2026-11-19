# SchoolToolsBoard (STB) v1.0.1.18

> 底座：Ink Canvas Plus（clover-tree/Ink-Canvas-Plus，GPL-3.0）｜发行者：周之尧

## 快捷设置面板版式
- 面板宽度 250 → 292，负边距同步从 -190 → -232，避免右侧被压；
- 画笔粗细滑杆 168 → 186，跟右侧数值之间留出间距；
- 「墨迹」这一行原来的三个裸图标（软盘、双文档、带刷新圈的笔）改成「图标 + 文字」三段：保存 / 保存到其它 / 回放，图标改成各自独立的竖向块，中间的分隔线去掉。

## 「保存到其它」的位置
1. 快捷设置 → 墨迹行中间的「保存到其它」（双文档 + 向右箭头），现在下面直接写着字；
2. 组件 · 浮动栏工具库里的「保存到其它」，加到自定义浮动栏后点一下直接弹另存对话框。

两者最后都走 `StbSaveAsNow()`：标题「保存墨迹到其它位置」，默认文件名 `SchoolToolsBoard-<时间>.icstk`，保存成功会弹通知。

## 开源
- 上游：https://github.com/clover-tree/Ink-Canvas-Plus （GPL-3.0）
- 本项目：https://github.com/me13354091017/SCHOOL_TOOLS_BOARD
