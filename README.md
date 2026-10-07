You can find the old SluMAN repository from [here](https://github.com/knuutti/SluMAN-EXT.com)

# SluMAN

SluMAN is speedrun tool for Sly Cooper games for PS3 and RPCS3. SluMAN is designed to help speedrunners practice the games by providing custom features that are not place in the vanilla game. 

SluMAN also provides a Speedrun Mode, that provides the runners with an input display, autosplitter and run file loading (exact features might differ between games)

## Supported games
- Sly Cooper and the Thievius Raccoonus (NPUA80663)
- Sly 2: Band of Thieves (NPHA80175)
- Sly 3: Honor Among Thieves (NPEA00343)

## Setup
To use SluMAN, follow these steps:
### Install SluMAN
Get the latest version of SluMAN from here: [Download](https://github.com/knuutti/SluMAN/releases/latest). Download the SluMAN.zip file and extract it to a destination you want. You only need to do the installation once: the program automatically checks for updates when a new release is published.
### PS3 setup
First, make sure you have `webMAN MOD` installed on a jailbroken PS3 using either a console with custom firmware or a HEN-enabled console.
It's important that you have the full version of webMAN MOD, which can be done by holding L1 while running the installer.

Make sure your PC and PS3 are connected to the same network.
Then, to connect SluMAN to your PS3, you need to IP address from your PS3. This can be done by going into Settings > System Settings > System Information on your console.

Input the IP address and insert it into the SluMAN.exe application on your PC.
Then all you need to do is run a game, and click Attach!

### RPCS3 setup
Just run `SluMAN.exe`, then while your game is open in RPCS3, click the `RPCS3` button. 

Some features may unfortunately not be supported on RPCS3.

## Input display in OBS
SluMAN serves the input display as a web page that OBS can show with a transparent background, so no chroma key is needed.

1. In SluMAN, open the Input Display, right-click it and choose **Copy OBS URL**. The default URL is `http://127.0.0.1:9674/pad`.
2. In OBS, add a **Browser Source** and paste the URL.
3. Set the width and height to the skin's size. SluMAN shows it when you copy the URL; DS3 skins are 800 × 558.

The page follows the skin chosen in SluMAN. To give a source its own skin, add `?skin=` and the folder name from `controllerskins`, for example `http://127.0.0.1:9674/pad?skin=Compact`. The pad turns grey while SluMAN isn't running and comes back when it starts again.

The page is only reachable from your own PC, so it needs no firewall rule. **Serve for OBS** and **Change OBS Port...** are in the same right-click menu.

## Acknowledgements
This project is a fork of [racman](https://github.com/MichaelRelaxen/racman). Huge thank you for the work of the racman and [Ratchetron](https://github.com/bordplate/Ratchetron) developers, without your contributions this project would not be possible!
