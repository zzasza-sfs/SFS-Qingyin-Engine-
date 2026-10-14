# 源码说明（从源码构建 / 二次开发）

本目录是青鹰SE系列两款发动机在 Unity 工程中的**源资产与构建脚本**。
它们依附于 SFS 官方 *Modding Toolkit* 工程（Unity 6000.1.17f1），单独打开无法编译运行。

## 文件对照（放置回工程的位置）

| 本目录文件 | 工程内位置（`Assets/` 相对路径） |
|---|---|
| `Editor/QingYinProBuilder.cs` | `Editor/QingYinProBuilder.cs`（青鹰SE Pro 构建/导出工具） |
| `Editor/QingYinBatchTools.cs` | `Editor/QingYinBatchTools.cs`（青鹰SE 原有工具） |
| `Editor/Builder.cs` | `Editor/Builder.cs`（工具包自带打包窗口，随工程已有） |
| `Prefabs/QingYin SE Engine.prefab` | `Resources/Parts/Engines/QingYin SE Engine.prefab` |
| `Prefabs/QingYin Pro Engine.prefab` | `Resources/Parts/Engines/QingYin Pro Engine.prefab` |
| `PackData/SDSA.asset` | `Resources/Parts/Engines/SDSA.asset`（青鹰SE 包数据） |
| `PackData/QingYinSEPro.asset` | `Resources/Parts/Engines/QingYinSEPro.asset`（Pro 包数据） |
| `Textures/Pro_Graphite.png` 等 | `Resources/Part Textures/Pattern Textures/` 与 `Textures/Icons/` |
| `Audio/Engine_Pro.asset` | `Resources/Audio.Sound/Engine_Pro.asset` |

> `.meta` 文件必须与资产一同放置，其中的 GUID 决定引用关系；丢失 meta 会导致引用断裂。

## 构建与导出（Unity 编辑器菜单）

- **青鹰SE Pro**：`SFS ▶ QingYin Pro ▶ Export Current Prefab as Pack`
  （原样打包当前 prefab 并自动部署到游戏 Mods 目录；另有 “Rebuild + Build + Export Pack” 会从 SE 基准重新生成 Pro，慎用——会覆盖手动改动）
- **青鹰SE**：`SFS ▶ QingYin ▶ Validate Prefab` 校验；批处理入口 `QingYinBatchTools.ValidateAndBuild`
- 命令行（批处理）示例：
  ```
  Unity.exe -batchmode -nographics -projectPath "<工程路径>" ^
            -executeMethod QingYinProBuilder.ExportCurrentAsPack -quit -logFile build.log
  ```

## 结构守则（修改前必读）

1. **不可修改** prefab 根对象名（`QingYin SE Engine` / `QingYin Pro Engine`）——存档按名字引用；
2. **必须保留** 的功能件：
   - 根对象模块：Part / OrientationModule / VariablesModule / MagnetModule / CustomSurfaces / FlowModule / EngineModule / TorqueModule / AudioModule / MoveModule；
   - `Base` 的 PipeSurface（油箱对接线，仅 `top` 边开启）；
   - `Nozzle Extension` 的 PipeSurface（喷口对接线，仅 `bottom` 边开启）与 PolygonCollider 的 `ownEngineNozzle` 链接（防止点火自热）；
3. 管线（CustomPipe）数据约定：`width` 字段为**全宽**（视觉半径 = 宽度 ÷ 2）；纹理/颜色层 `height` 为沿管道局部累计长度；坐标须扣除父级位移（如 `Man` 的 y = -0.21）；
4. 构建脚本内置校验（必需模块、对接线、自热豁免、脚本完整性），不通过会中止导出。

## 依赖的外部资产

两款发动机复用 Modding Toolkit 自带的标准材质/贴图资源（Metal、Metal_4、Color_White、Edges Faces、Flat Smooth 等）
与游戏内置 `Liquid_Fuel` 燃料、`Engine_2` 音源素材——这些不在本仓库内，请在原工具包工程中使用。
