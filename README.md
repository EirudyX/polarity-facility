# 极性失控 Polarity Facility

《极性失控》前期策划案的 Unity 3D 双球物理解谜切片。场景、美术与程序在工程内可编辑。原切片使用 Unity 2022.3.58f1c1，无第三方运行时插件。

当前五项机制演示使用 Unity 6000.3.25f1。项目进度、验证记录和接续位置见 [HANDOFF.md](HANDOFF.md)。

## 选关与功能演示

- 本机已验证的最新试玩为 `Build/Windows-ClimbDemo/PolarityFacility.exe`。启动页预留「第 1 关」「第 2 关」，均显示「待制作」。通过菜单重建后的包位于 `Build/Windows-DemoMenu`。
- 点击右下角「功能演示」或按 `D` 打开演示列表。点击项目或按 `1` 到 `5`，分别进入双球磁力、配重转移、可翻转磁轨、泵涌、磁吸攀壁。进入后按 Enter 开始。
- 演示内按 Esc 返回演示列表，开始后可按 `R` 重开。完成后按 Enter 返回列表，前三项另有 `Demo List` 按钮。在列表按 Esc 或点击「返回选关」回到启动页。
- 在 Unity 打开 `Assets/Scenes/LevelMenu.unity` 并点击 Play 可验证入口。通过 **Tools → Polarity Facility → Build Demo Menu for Windows** 构建完整的 Windows 试玩包。
- 以下命令会对启动页和演示列表截图，分别打开五个场景，自动跑通每个演示并验证返回列表。`-facilityCampaign` 沿用旧版的自动验证参数名。

```powershell
$prefix = Join-Path $env:TEMP 'polarity-demo-menu'
$player = '.\Build\Windows-ClimbDemo\PolarityFacility.exe'
# 若刚通过 Unity 菜单重建，将 player 改为 .\Build\Windows-DemoMenu\PolarityFacility.exe
& $player -facilityCampaign -facilityProof $prefix -logFile "$prefix.log"
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

## 泵涌

- 在功能演示列表按 `4`，再按 Enter。A 球固定在凹槽，按空格或右 Shift 切换 B 球磁极。
- 让 B 球越过上坡后的断口，落到接收台。顶部显示首次和最快过线速度，以及撞墙前后的带符号速度。按 `R` 立即重试。
- 试验道沿用现有磁力公式、理想弹性反弹和较低的滚动阻力。自动试玩比较单次弹射在坡中滚回与泵涌一轮后越过断口的结果。
- 在 Unity 可直接打开 `Assets/Scenes/PumpSurge.unity`。**Tools → Polarity Facility → Rebuild Pump Surge Scene** 可重新生成场景。

## 磁吸攀壁

- 本轮修正版试玩为 `Build/Windows-ClimbDemo/PolarityFacility.exe`，包含全部五个演示。常规菜单构建命令仍会输出到 `Build/Windows-DemoMenu`。
- 在功能演示列表按 `5`，再按 Enter。A / D 移动 A 球，方向键移动 B 球，右 Shift 或空格切换 B 球极性。R 重试，Esc 返回列表。
- 墙与顶棚为 N 极。蓝色 B 球向左靠近墙后会吸附，按上或右沿磁面前进，下或左后退。在顶棚上切成红色会脱离并受到排斥，保持向右移动落到对岸接收台，升起下方桥梁。两球沿右侧路线抵达出口后完成演示。
- 本原型使用磁面吸附约束、沿面驱动与真实重力下落。自动试玩验证同极排斥、异极吸附、墙角过渡、顶棚停留、切极落台、双球出场与重试复位。
- Unity 场景为 `Assets/Scenes/MagneticClimb.unity`，**Tools → Polarity Facility → Rebuild Magnetic Climb Scene** 可重新生成。

## 试玩

- Windows 包解压后运行 `PolarityFacility.exe`，Enter 开始。
- 前三项演示中，A 球用 WASD 移动、Q 切极，B 球用方向键移动、右 Shift 切极。Tab 交换两组键位的目标球，R 重开。泵涌与磁吸攀壁的操作见各自章节。
- 功能演示保留操作、目标与状态反馈。完整课堂演示路线见配套 Word。

## Unity 工程

1. 解压源码并在 Unity Hub 添加项目目录。
2. 打开 `Assets/Scenes/PolarityFacility.unity`，点击 Play 后选择 Game 视图。
3. 在 Tools → Polarity Facility → Physics Lab 运行 7 项检查。
4. `Assets/Scripts/MagnetRules.cs` 包含磁力与触发条件，`FacilityLevel.cs` 负责物理步进及视效。生成器位于 `Assets/Editor/FacilityBuilder.cs`。使用场景重建菜单前请另存自己的场景修改。
5. 可通过 Unity Build Settings 构建 Windows x64。程序会在纯本地运行。

## 实践范围

双球磁力演示实现接触锁定、凹槽锚定、排斥弹射、距离牵制、固定磁极、双球压盘与出场。配重转移演示增加动态质量，磁轨演示增加可翻转的环境磁极。泵涌演示使用弹性反弹墙积累速度并飞越断口，磁吸攀壁演示验证墙面与顶棚吸附、切极落台和双球协作。在线联机属于后续功能。

采用 unity-dod-architecture、ui-imgui、unity-ui-ux-checklist 的工作流，借鉴 unity-development 的本地验证顺序。相关原始文档见配套 Word。
