# SchoolToolsBoard（STB）课堂白板 v1.1.2

面向课堂触控一体机的开源白板，**主应用程序已统一为桌面版**——以 [ICC-CE（InkCanvasForClass Community Edition）](https://github.com/InkCanvasForClass/community) 源码为底座 fork 改造；自研 WPF 版正式下线。GPL-3.0。本应用是 [SCHOOL_TOOLS](https://me13354091017.github.io/school_tools/) 旗下应用。

## 本版变更：扶正桌面版，移除自研 WPF 版

| 项 | 变化 |
|---|---|
| 版本体系 | 从「Web / 自研 WPF / ICC-CE 底座」三版本收敛为「**桌面版（主应用）** + Web 版（配套）」 |
| 自研 WPF 版 | **整体移除**：`wpf/` 源码、`.github/workflows/wpf.yml`、Release 中的 `SchoolToolsBoard-WPF-*.exe` 附件、Pages 的交叉编译与 `download/school-tools-board.exe` 全部删除 |
| 版本号 | `1.1.2`（破坏性变更：旧的 WPF 下载链接与附件不再存在） |
| 文档 | README / 本说明按「桌面版为主应用」重写，目录结构与流水线说明同步更新 |

## 下载

| 文件 | 说明 |
|---|---|
| `SchoolToolsBoard-STB-Desktop-1.1.2.zip` | **主应用**。解压运行 `SchoolToolsBoard.exe`（Windows 10 1809+ / 11 x64），内置 `ppt-agent/` PPT 批注加载项 |
| `SchoolToolsBoard-Web-1.1.2.html` | 配套 Web 离线单文件版，约 170 KB，双击即用、断网可用 |
| `LICENSE.txt` | GPL-3.0 完整原文 |

在线上机版：<https://me13354091017.github.io/SCHOOL_TOOLS_BOARD/>

## 主应用（桌面版）能力

- **PPT 放映批注**：PowerPoint 放映中用笔书写，按幻灯片逐页保存墨迹，翻页墨迹跟着走（VSTO 加载项 `ppt-agent` + 管道通信）
- **多页白板 + 时间机器**：上百页滑页、无限步撤销/重做、页缩略图
- **手势与笔压**：双指缩放 / 旋转 / 平移，笔压参与笔宽，手掌防误触
- **形状与学科工具**：直线 / 矩形 / 椭圆 / 箭头 / 抛物线、坐标系与函数图像、计时器、聚光灯

### 品牌化方式（可长期跟上游升级）

源码基座在 fork 仓库 <https://github.com/me13354091017/community>（默认分支 `net10`），**不改上游任何一行源码**，全部品牌改动在 CI 构建前打补丁：

1. `InkCanvasForClass.csproj` 的 `AssemblyName` → `SchoolToolsBoard`
2. `Ink Canvas/MainWindow.xaml` 的窗口标题 → 「SchoolToolsBoard · 课堂白板」
3. `Resources/icc.ico` 由 `stb/icon.svg` 现场生成（多尺寸 ICO）

`stb-build.yml` 在 windows-latest 上依次完成：还原 → 生成图标 → MSBuild 构建主工程 → 装 VSTO Runtime + 自签代码签名证书 → 构建 `InkCanvas.PowerPointAddIn` → 打包 → 发布 `stb-latest`。主仓库发布时直接从 `stb-latest` 取 zip，保证桌面版与上游同步升级不产生任何合并冲突。

## 升级建议

- 已经在用 v1.1.1 桌面版的老师：直接把新 zip 解压覆盖旧目录即可，配置与课件不受影响。
- 之前下过 WPF 版的：该版本已停止维护，请改用桌面版（功能更全，且支持 PPT 原位批注）。

## 已知限制

- 桌面版仅 Windows x64（Win10 1809 及以上 / Win11）。
- PPT 批注需本机安装 PowerPoint，并在首次使用时允许加载项。
- 仍未覆盖：云账号与课件库、协同备课、微课录制、课堂活动（分组竞赛/抢答器）、截图提问、教材资源打通——这些需要后端与内容生态。

## 许可

GPL-3.0-or-later。上游 ICC-CE 作者 CJKmkp 及更早的血统（WXRIW/Ink-Canvas → ChangSakura/Ink-Canvas → InkCanvasForClass → ICC-CE）在此一并致谢。
