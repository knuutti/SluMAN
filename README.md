# SluMAN

SluMAN is a speedrun tool for the Sly Cooper games on PS3 and RPCS3. It connects to the running game and gives speedrunners practice features the game doesn't have, such as loading levels and jobs, saving positions, toggles like invulnerability and fly mode, and a free camera.

Every game also has a Speedrun Mode with an input display (which can also be shown in OBS) and an autosplitter for LiveSplit. Features differ a little between games.

## Supported games
| Game | Versions |
|------|----------|
| Sly Cooper and the Thievius Raccoonus | NPUA80663 |
| Sly 2: Band of Thieves | NPUA80664, NPEA00342, NPHA80175 |
| Sly 3: Honor Among Thieves | NPEA00343 |
| Sly Cooper: Thieves in Time | BCES01284, NPEA00429 |
| Bentley's Hackpack | NPEA00427 |

## Setup
### Install SluMAN
Download `SluMAN.zip` from the [latest release](https://github.com/knuutti/SluMAN/releases/latest) and extract it anywhere. You only need to do this once: SluMAN checks for updates itself when a new release is published.

### PS3
1. Install the full version of `webMAN MOD` on a jailbroken PS3 (custom firmware or HEN). To get the full version, hold L1 while running the installer.
2. Connect your PC and PS3 to the same network.
3. Find the PS3's IP address in Settings > System Settings > System Information.
4. Enter the IP address in SluMAN, start the game and click **Attach**.

### RPCS3
Start the game in RPCS3, then run `SluMAN.exe` and click **RPCS3**.

Some features need RPCS3's PPU decoder set to Interpreter: the free camera in Sly 2, Sly 3, Sly 4 and Bentley's Hackpack, and loading levels and jobs in Sly 4 and Bentley's Hackpack. Set it in `Configuration > CPU > PPU Decoder > Interpreter (static)`, then **Apply & Save**. Otherwise keep the default, `Recompiler (LLVM)`.

## Contributing
Bug reports and feature requests are welcome and can be issued in the [SluMAN Development Discord](http://slyspeedruns.com/discord/sluman). Direct code changes are welcome as pull requests (target `master` branch).

### Building
SluMAN is a .NET Framework 4.7.2 WinForms app for Windows (x64 only).

1. Open `SluMAN.sln` in Visual Studio with the .NET desktop development workload installed.
2. Build with the **Release | x64** configuration.
3. The build goes to `SluMAN\bin\x64\Release\`. Run `SluMAN.exe` from there.

Local builds show as a dev build and don't check for updates.

## Acknowledgements
This project is a fork of [racman](https://github.com/MichaelRelaxen/racman). Huge thank you for the work of the racman and [Ratchetron](https://github.com/bordplate/Ratchetron) developers, without your contributions this project would not be possible!

Sly 1 Practice Mode uses addresses found by [SlyMultiTrainer](https://github.com/NiV-L-A/SlyMultiTrainer) by NiV-L-A, and Sly 4 support is built on [SLY4-DEBUG-MENU](https://github.com/gg3502/SLY4-DEBUG-MENU) by gg3502. Thank you!

Licenses for code and data from other projects are in [`THIRD-PARTY-NOTICES.txt`](THIRD-PARTY-NOTICES.txt), which is also included in the SluMAN download.
