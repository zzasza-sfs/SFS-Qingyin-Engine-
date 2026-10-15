# 青鹰SE系列 ｜ Spaceflight Simulator 自定义发动机模组

青鹰SE（QingYing SE）与青鹰SE Pro 是一套面向 **Spaceflight Simulator（PC）** 的自定义部件模组，
包含基准型与增压改进型两款 1.0 米级液体发动机，采用统一的设计语言与配色体系。

| 指标 | 青鹰SE（基准型） | 青鹰SE Pro（增压改进型） | 变化 |
|---|---|---|---|
| 推力 | 100 kN | **112 kN** | +12.0% |
| 真空比冲 | 290 s | **311 s** | +7.2% |
| 干重 | 3.5 t | 3.8 t | +8.6%（权衡） |
| 推重比 | 2.91 | 3.00 | +3.1% |
| 万向摆角 | ±5° | **±7°** | +40%（升级点） |
| 生涯造价 | 45 | 70 | +55.6%（权衡） |
| 总长（安装面→喷口） | 2.06 | 2.48 | +20.4%（权衡） |
| 构造 | 一体式喷管 / 等径涡轮泵管 | 两件式喷管（联轴环套接）/ 两段式涡轮泵 | — |

> 详细的产品定位、核心功能、适用场景、规格与部署指南见 **[docs/青鹰SE系列_产品白皮书.html](docs/青鹰SE系列_产品白皮书.html)**（内含性能对比图）。

## 仓库结构

```
├─ packs/    模组本体（直接放入游戏即可使用）
│   ├─ 青鹰SE.pack
│   └─ 青鹰SEPro.pack
├─ source/   源码（Unity Modding Toolkit 工程资产与构建脚本）
│   ├─ Editor/      构建/导出脚本（C#）
│   ├─ Prefabs/     两款发动机的源 prefab
│   ├─ PackData/    模组元数据（名称/版本/图标）
│   ├─ Textures/    Pro 专属贴图与图标
│   └─ Audio/       Pro 音效资产
├─ docs/     产品白皮书 + 性能对比图
└─ images/   实机截图与设计预览
```

## 安装（玩家）

1. 将 `packs/青鹰SE.pack` 与 `packs/青鹰SEPro.pack` 复制到：
   `…\Spaceflight Simulator\Spaceflight Simulator Game\Mods\Custom_Assets\Parts\`
2. 启动游戏，模组列表会显示“青鹰SE / 青鹰SE Pro”；建造模式中在 **Engines / Basic** 分类取用。
3. 两个模组互相独立、可共存，也可混装于同一枚火箭（安装口径一致）。

## 从源码构建（开发者）

见 **[source/README.md](source/README.md)**。简而言之：将 `source/` 内的资产按目录放回
Unity *Modding Toolkit* 工程对应位置，使用编辑器菜单一键打包导出。

## 展示

| | |
|---|---|
| ![青鹰SE](images/01_青鹰SE_实机截图.png) | ![青鹰SEPro 设计](images/02_青鹰SEPro_设计预览.png) |
| 青鹰SE 实机 | 青鹰SE Pro 设计预览（联轴环对接细节见 images/03） |

## 致谢与说明

- 作者：**zzasza**
- 开发环境：Unity 6.1（6000.1.17f1）+ SFS Modding Toolkit
- 许可：转载请保留作者署名。如需商用请先联系作者。
