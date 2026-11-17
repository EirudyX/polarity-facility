# 极性失控 Polarity Facility

《极性失控》前期策划案的 Unity 3D 双球物理解谜切片。场景、美术与程序在工程内可编辑。原切片使用 Unity 2022.3.58f1c1，无第三方运行时插件。

包含配重和磁轨演示的分支使用 Unity 6000.3.25f1。

## 选关与功能演示

- 运行 `Build/Windows-DemoMenu/PolarityFacility.exe`，启动页预留「第 1 关」「第 2 关」。两个位置均显示「待制作」。
- 点击右下角「功能演示」或按 `D` 打开演示列表。点击项目或按 `1`、`2`、`3`，分别进入双球磁力、配重转移、可翻转磁轨；进入后按 Enter 开始。
- 演示内按 Esc 返回演示列表，开始后可按 `R` 重开。完成后按 Enter 或点击 `Demo List` 返回列表。在列表按 Esc 或点击「返回选关」回到启动页。
- 在 Unity 打开 `Assets/Scenes/LevelMenu.unity` 并点击 Play 可验证入口。通过 **Tools → Polarity Facility → Build Demo Menu for Windows** 构建完整的 Windows 试玩包。
- 以下命令会对启动页和演示列表截图，分别从列表打开三个场景，自动跑通每个演示并验证返回列表。`-facilityCampaign` 沿用旧版的自动验证参数名。

```powershell
$prefix = Join-Path $env:TEMP 'polarity-demo-menu'
& .\Build\Windows-DemoMenu\PolarityFacility.exe -facilityCampaign -facilityProof $prefix -logFile "$prefix.log"
```

## 配重转移与跨隙弹射

- 从 `Build/Windows-MassTransfer/PolarityFacility.exe` 开始独立试玩。Unity 中打开 `Assets/Scenes/MassTransfer.unity` 并点击 Play。
- 两球开场都带配重。分别在配重台的两个圆盘上停留片刻，可把 B 球的金色配重转移给 A 球。A 球固定在凹槽后，切极弹射减轻的 B 球，它能飞到对岸触发升桥。最后两球一起抵达出口。
- 用 **Tools → Polarity Facility → Build Mass Transfer for Windows** 构建 Windows 试玩包。**Rebuild Mass Transfer Scene** 会重新生成演示场景，手动编辑前可先另存场景。
- 构建后可以用 PowerShell 运行自动试玩。它会验证重球落下、双盘转移、轻球落到感应盘、双球抵达出口及重开还原配重。

```powershell
$prefix = Join-Path $env:TEMP 'polarity-mass'
& .\Build\Windows-MassTransfer\PolarityFacility.exe -facilityProof $prefix -logFile "$prefix.log"
```

## 可翻转磁轨

- 从 `Build/Windows-RailRelay/PolarityFacility.exe` 试玩。Unity 中打开 `Assets/Scenes/RailRelay.unity` 并点击 Play。
- B 球保持蓝色，沿上方平台往右走到转角。A 球停在下方平台的圆盘上，短磁轨会从红变蓝，并把 B 球推向上方窄道。B 球继续向右触发感应盘，A 球便可以离开原地，沿升起的桥前往出口。最后两球一起进入大蓝圈。
- 用 **Tools → Polarity Facility → Build Rail Relay for Windows** 构建试玩包。**Rebuild Rail Relay Scene** 可重新生成场景。
- Windows 自动试玩会验证转角受阻、磁轨反向、B 球不切极通过、A 球过桥、双球出场和重开复位。

```powershell
$prefix = Join-Path $env:TEMP 'polarity-rail'
& .\Build\Windows-RailRelay\PolarityFacility.exe -facilityProof $prefix -logFile "$prefix.log"
```

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

双球磁力演示实现接触锁定、凹槽锚定、排斥弹射、距离牵制、固定磁极、双球压盘与出场。配重转移演示增加动态质量，磁轨演示增加可翻转的环境磁极。原策划案的泵涌和在线联机属于后续功能。

采用 unity-dod-architecture、ui-imgui、unity-ui-ux-checklist 的工作流，借鉴 unity-development 的本地验证顺序。相关原始文档见配套 Word。
