# SchoolToolsBoard 1.0.1.34

修复函数画板缩放按钮图标显示不全，以及放映中翻页报 COMException 0x8000000B。

## 日志里发现的真实报错
用户提供的日志（1.0.1.33）里，2026-11-20 20:17 连续 20+ 次出现：

```
System.Runtime.InteropServices.COMException (0x8000000B): 该操作尝试访问的数据超出了有效范围
   在 Microsoft.Office.Interop.PowerPoint.SlideShowWindows.get_Item(Int32 Index)
   在 InkCanvasPlus.MainWindow.BtnPPTSlidesUp_Click / BtnPPTSlidesDown_Click
```

原因：WPS 放映、轻量批注模式或 COM 断开时，`SlideShowWindows.Count` 为 0，但两个翻页方法仍然直接取 `SlideShowWindows[1]`，在 SaveScreenShot 与 View.Previous()/Next() 处抛出未处理异常。

修正：两个方法先判断 `pptApplication == null || StbPptComShowCount() < 1`，直接返回；有放映窗时保持原逻辑不变。

## 缩放按钮图标
`BtnZoomIn/BtnZoomOut/BtnZoomReset` 原来用字体字符 `＋ / － / ⟲`，在 32×30 按钮里被字体回退裁成一条竖线或奇怪的三角。现在改为矢量 `Path`：

- 放大：`M8,0 V16 M0,8 H16` 真正的十字
- 缩小：`M0,8 H16` 横线
- 还原：`M10,4 A5,5 0 1 0 10,14 ...` 回环箭头

按钮 34×32、`Padding=0`、图标 14~15 居中，不再依赖字体。

## 延续
1.0.1.33 的放映误判修正、1.0.1.32 的点击定位与键盘浮层、1.0.1.31 的重新测量与补乘号。

底座 Ink Canvas Plus（GPL-3.0），改动经 stb-plus 补丁链（fix3→fix26）CI 注入，未改上游源码。
