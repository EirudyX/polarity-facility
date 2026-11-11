# 极性失控 Polarity Facility

《极性失控》前期策划案的 Unity 3D 双球物理解谜切片。场景、美术与程序在工程内可编辑。切片使用 Unity 2022.3.58f1c1，无第三方运行时插件。

## 试玩

- Windows 包解压后运行 `PolarityFacility.exe`，Enter 开始。
- 初始 A 球用 WASD 移动、Q 切极。B 球用方向键移动、右 Shift 切极。Tab 交换两组键位的目标球，R 重开。
- 游戏内只提供操作键位与同极相斥、异极相吸。完整课堂演示路线见配套 Word。

## Unity 工程

1. 解压源码并在 Unity Hub 添加项目目录。
2. 打开 `Assets/Scenes/PolarityFacility.unity`，点击 Play 后选择 Game 视图。
3. 在 Tools → Polarity Facility → Physics Lab 运行 7 项检查。
4. `Assets/Scripts/MagnetRules.cs` 包含磁力与触发条件，`FacilityLevel.cs` 负责物理步进及视效。生成器位于 `Assets/Editor/FacilityBuilder.cs`。使用场景重建菜单前请另存自己的场景修改。
5. 可通过 Unity Build Settings 构建 Windows x64。程序会在纯本地运行。

## 实践范围

实现接触锁定、凹槽锚定、排斥弹射、距离牵制、固定磁极、双球压盘与出场。原策划案的动态质量、泵涌和在线联机属于后续功能。

采用 unity-dod-architecture、ui-imgui、unity-ui-ux-checklist 的工作流，借鉴 unity-development 的本地验证顺序。相关原始文档见配套 Word。