# 《极性失控》项目交接

更新日期为 2026-11-19。本文件记录截至本轮整理的工程状态，后续工作范围以用户在新对话中的要求为准。

## 项目目标与用户偏好

项目处于玩法原型验证阶段。先逐项制作可玩的机制，放进独立的「功能演示」。正式关卡暂定两关，启动页均为「待制作」。两关的主题、路线、谜题组合和难度节奏待确定。

用户要求机制被发现时有明显的顿悟和回报。泵涌曾只有约 17% 的速度增量，用户认为不足以支撑玩法。因此目前用单次冲坡失败、积累速度后飞越断口的结果体现差异。磁吸攀壁的目标是让玩家发现墙和顶棚也能成为路线。

当前五项演示已实现并通过自动试玩。用户本轮要求整理上传、交接给下个对话。下一个开发功能待用户确定。

## 最新工程与试玩

- 本机工程为 [polarity-facility](D:/Codex/Projects02/polarity-facility)。
- Unity 为 [6000.3.25f1](D:/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe)。
- 当前已验证的完整试玩为 [Windows-ClimbDemo/PolarityFacility.exe](D:/Codex/Projects02/polarity-facility/Build/Windows-ClimbDemo/PolarityFacility.exe)，包含选关页和五项演示。
- Unity 入口为 [LevelMenu.unity](D:/Codex/Projects02/polarity-facility/Assets/Scenes/LevelMenu.unity)。按 Play 后与独立试玩使用相同菜单。
- 「功能演示」按钮或 D 打开列表，数字键 1 到 5 选择演示。列表支持滚动，第五项可直接按 5 打开。Enter 开始，Esc 返回列表，R 重试。完成后 Enter 返回列表。
- 通过 Tools → Polarity Facility → Build Demo Menu for Windows，或批处理调用 `PolarityFacility.FacilityBuilder.BuildDemoMenuWindows`，会把当前源码重建到 `Build/Windows-DemoMenu`。

本机 `Windows-ClimbDemo` 是修正了 A 球通路遮挡后验证通过的版本。当时检测到用户正在操作早先试玩窗口，因此将修正版构建到独立目录。`Windows-DemoMenu` 的现有磁吸攀壁包保留了修正前布局。旧 `Windows-Campaign` 为更早的课堂版本。接续试玩可直接使用上面的 `Windows-ClimbDemo`。

## 五项演示

| 快捷键 | 演示与场景 | 当前行为 |
| --- | --- | --- |
| 1 | 双球磁力，`PolarityFacility.unity` | 吸引、接触锁定、锚定、排斥弹射、双球机关与出口。 |
| 2 | 配重转移，`MassTransfer.unity` | 双盘转移配重，改变弹射距离，触发升桥后双球出场。 |
| 3 | 可翻转磁轨，`RailRelay.unity` | A 踩盘改变环境磁轨，让 B 保持自身极性通过转角，再为 A 开路。 |
| 4 | 泵涌，`PumpSurge.unity` | 理想弹性墙折返，切极积累速度，冲坡飞越断口并落到接收台。 |
| 5 | 磁吸攀壁，`MagneticClimb.unity` | B 沿墙、圆角与顶棚移动，切极落台后升桥，A 从前侧通路过桥，两球抵达出口。 |

前三项共用 `FacilityLevel.cs`。泵涌和攀壁各有独立控制器，操作与原型范围如下。

### 泵涌

A 固定。Space 或右 Shift 切换 B 极性，R 立即重试。球靠近 A 前会碰到反弹墙，水平速度符号翻转、大小保持，沿滚动方向的转动也反向。磁力沿用 `MagnetRules.PairForce`，轨道阻尼为 0.08。单次弹射会在坡中滚回，通过切极让球折返并再次撞墙后，可获得足够速度冲坡和落台。

最新对照日志中，最佳单次弹射以 2.12 m/s 过线，在 x=4.52 处滚回，坡顶为 x=5.22。另一条完整泵涌路线以 1.72 m/s 首次过线，积累一轮后达到 2.69 m/s，约提高 56%，在 x=6.08 落台。首次过线速度因切极时机而有差异。

该演示在同一测速线记录首次与最快过线速度，完成条件是落到对岸。场内给目标、按键和读数，切极时机由玩家探索。

### 磁吸攀壁

A / D 移动 A。方向键移动 B，右 Shift 或 Space 切 B 极性。蓝色 B 为 S，墙和顶棚为 N。B 向左靠墙会吸附，随后上或右沿磁面前进，下或左后退。顶棚边缘附近切成红色后，B 沿法线脱离并受重力和斥力影响，继续向右可落到接收台。

接收台启动下方桥梁，等待桥升起后 A 可向右通过。上层接收台与下坡位于后侧，A 的地面路线位于前侧，已修正早期 A 被上层平台挡住的问题。两球同时站在右侧出口平台完成演示。

当前吸附通过预设墙面、圆角和顶棚路径的弹簧约束、沿面驱动与重力补偿实现。切极脱离含 2.5 m/s 的法线速度增量，再施加近面排斥。该原型的攀壁逻辑作用于 B，A 负责地面路线，球间连线用于视觉反馈。通用磁面组件和双球均可攀壁属于后续可扩展范围。

## 源码接续位置

| 文件 | 用途 |
| --- | --- |
| `Assets/Scripts/LevelNavigation.cs` | 五项 Demos 登记、菜单、滚动列表、中文字体回退、演示返回与自动巡回。提供 FindChineseFont、CaptureFrame、Fill 给新演示使用。 |
| `Assets/Scripts/MagnetRules.cs` | 基础磁力、锁定规则，新增 TetherForce 供原控制器和泵涌共用。 |
| `Assets/Scripts/FacilityLevel.cs` | 前三项控制与自动试玩，远距牵制已调用共用 TetherForce。 |
| `Assets/Scripts/PumpSurgeDemo.cs` | 泵涌物理、反弹墙、UI、落台完成与自动验证。坡道和落台尺寸常量在文件顶部。 |
| `Assets/Editor/FacilityPumpBuilder.cs` | 泵涌场景生成，菜单 Rebuild Pump Surge Scene。 |
| `Assets/Scripts/MagneticClimbDemo.cs` | 攀壁吸附、切极脱离、接收台、桥梁、出口与自动验证。路径尺寸常量在文件顶部。 |
| `Assets/Editor/FacilityClimbBuilder.cs` | 攀壁场景与圆角磁面生成，菜单 Rebuild Magnetic Climb Scene。 |
| `Assets/Editor/FacilityBuilder.cs` | 共用生成工具与完整 Windows 构建，场景数组包含菜单加五项演示。 |
| `ProjectSettings/EditorBuildSettings.asset` | 编辑器构建场景登记，已包含五项演示。 |
| `README.md` | 试玩、构建和自动试玩说明。 |

新增演示涉及 Demos、构建场景数组和编辑器构建设置三个登记位置。控制器同时需要自己的自动验证路线。原三个场景的验证在 FacilityLevel，新两个场景各自执行 Prove。正式关卡落地时，需要根据场景类型设计返回和完成后的目的地。

场景生成器会重新生成布局。Unity 保存时曾附带改写材质 `m_AllowLocking`、场景模板设置和 MultiplayerManager，这些编辑器附带变化已清理，本次提交包含玩法和文档改动。

## 验证记录

- 初次攀壁构建日志 [climb-build.log](D:/Codex/Projects02/polarity-facility/Logs/climb-build.log) 记录 Build Success 与 7 项物理规则检查通过。
- 最终修正版构建日志 [climb-final-build.log](D:/Codex/Projects02/polarity-facility/Logs/climb-final-build.log) 记录 Build Success。该版通过 Unity 命令行的 `-buildWindows64Player` 构建到独立目录。
- 最终自动试玩日志 [ClimbFinal-150127/player.log](D:/Codex/Projects02/polarity-facility/Logs/ClimbFinal-150127/player.log) 完整跑通五项演示与各自返回列表，进程退出码 0。攀壁覆盖同极排斥、异极吸附、墙角过渡、顶棚停留、切极落台、桥梁、双球出场及重试复位。
- 实际窗口截图 [climb-window.jpeg](D:/Codex/Projects02/polarity-facility/Logs/ClimbFinal-150127/climb-window.jpeg) 展示最终布局。后台隐藏窗口运行时，ReadPixels 生成的部分 proof PNG 会是空白，画面参考这张实际窗口截图。
- 本轮没有重新运行旧交接中提到的 55 文件工程扫描。现有验证包括 Unity 编译构建、物理规则检查、自动试玩和实际窗口查看。

从工程根目录运行现有完整试玩验证可使用以下 PowerShell。路径前缀的父目录需已存在。

```powershell
$proofDir = Join-Path $env:TEMP ('polarity-proof-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
[void](New-Item -ItemType Directory -Path $proofDir)
$player = (Resolve-Path '.\Build\Windows-ClimbDemo\PolarityFacility.exe').Path
$playerArgs = @('-facilityCampaign', '-facilityProof', (Join-Path $proofDir 'proof'),
    '-logFile', (Join-Path $proofDir 'player.log'))
$playerProcess = Start-Process -FilePath $player -ArgumentList $playerArgs -PassThru -WindowStyle Hidden
if (-not $playerProcess.WaitForExit(170000)) {
    Stop-Process -InputObject $playerProcess
    throw 'Automatic playthrough timed out'
}
$playerProcess.Refresh()
$playerProcess.ExitCode
Get-Content -LiteralPath (Join-Path $proofDir 'player.log') -Tail 25
```

## Git 与交接文件

远端为 [EirudyX/polarity-facility](https://github.com/EirudyX/polarity-facility)，工作分支为 `feature/mass-transfer-demo`。

- 菜单整理提交为 `8298026`。
- 本轮五项演示源码提交为 [996223960ce9ccd8dd7c1ec20f1bf473d19ac2da](https://github.com/EirudyX/polarity-facility/commit/996223960ce9ccd8dd7c1ec20f1bf473d19ac2da)。
- 本交接随后的文档提交同步到同一分支，最新提交可用 `git log -2 --oneline` 查看。
- `Build/`、`Logs/`、`Library/` 等遵循现有 .gitignore，试玩包和验证日志保存在本机。Git 仓库包含源码、场景、Unity meta 与本交接，可在相应 Unity 版本中重新构建。

仓库内交接为 [HANDOFF.md](D:/Codex/Projects02/polarity-facility/HANDOFF.md)，仓库外提供同内容副本 [极性失控_HANDOFF.md](D:/Codex/Projects02/极性失控_HANDOFF.md)，便于直接交给下一对话。接续时可先核对 `git status --short --branch`、`git log -2 --oneline` 和上面的试玩入口。

早期设计资料为 [极性失控_前期策划案.docx](D:/Codex/Projects02/策划案确定/极性失控_前期策划案.docx)，课堂材料为 [极性失控_提交](D:/Codex/Projects02/游戏开发智能体SKILL_调研与演示/极性失控_提交)。当前实现和用户后续确认将继续更新设计。
