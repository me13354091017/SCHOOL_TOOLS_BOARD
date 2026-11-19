# SchoolToolsBoard (STB) v1.0.1.16

> 底座：Ink Canvas Plus（clover-tree/Ink-Canvas-Plus，GPL-3.0）｜发行者：周之尧

## 设置页开关卡片被拉成整页高
上一版把「橡皮用完自动回到书写」卡片插在了 `Panel0` **外面**：锚点用的是 `<!-- 1 外观主题 -->` 注释，而那段注释在 `</StackPanel>` 之后，于是卡片变成 `ScrollViewer` 的直接子元素，`VerticalAlignment` 按拉伸算，开关被推到页面底部。

本版做法：先按 `x:Name="SwAutoPenBack"` 定位已插入的卡片，往前取最近的 `<Border Style="StbCard">` 起点、往后取第一个 `</Border>` 终点，把整张卡摘下来；再以 `<!-- 1 外观主题 -->` 为锚点，回退找到最近的 `</StackPanel>`（就是 Panel0 的结尾），把卡片插回它前面。卡内没有嵌套 Border，摘取边界不会错位；脚本会断言“开关位置在 Panel0 之后、且在外观主题注之前”。

## 快捷设置面板下方一大块空白
浮动栏 `BorderTools` 在原布局里会随容器拉伸。加了 `VerticalAlignment="Top"`，让面板高度只跟内容走，背景不再向下拖出一段。

## 保留
- 浮动栏接入组件系统（编辑浮动栏）
- 橡皮抬笔后才回笔 + 设置页开关

## 开源
- 上游：https://github.com/clover-tree/Ink-Canvas-Plus （GPL-3.0）
- 本项目：https://github.com/me13354091017/SCHOOL_TOOLS_BOARD
