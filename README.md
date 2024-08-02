# Railroader Mod: SmartOrders

Make shunting easier by telling the AI where to move in relation to nearby switches.

This mod adds the following buttons to the Yard AI panel:

* **Approach Ahead** / **Clear Ahead** / **Clear Under**: Use this to choose where to move relative to switches:
  * **Approach Ahead**: Go to the nth switch in front of the train, but do not pass the switch
  * **Clear Ahead**: Go to the nth switch in front of the train, and pass the switch so the whole train is on the other side.
  * **Clear Under**: Look for switches _under_ the train and move so that the full length of the train is passed the nth switch.
* **1, 2, 3 etc**: Use these buttons to choose how many switches to move after choosing the switch mode above.

When approaching or clearing a switch, if the train stops on the _exit side_ of the switch, the AI will leave enough room for the other track to be used.

The AI will look for switches as far as 10,000 feet away, but is less precise the further away the target switch is. If the track ends before it finds all the switches you asked for it will just move to the track end, using the existing Yard AI logic to do so safely. If it can't can't find all the switches you asked for in the 10,000 feet in front of the train, it will just move 10,000 feet and stop.

Known incompatability: Unfortunately this mod is **not** compatible with wexp's [RR-YardAiExtended](https://github.com/wexp/RR-YardAiExtended) mod, however SmartOrders adds a Yard AI car lengths button to go "infinity car langths", which you can use if you just need to couple to something really far away. See below for other quality of life features this mod adds.

Example usage:

You have a train stopped in Parson's Tannery P3 and need to get back to the mainline. There are seven switches between Parson's Tannery P3 and the mainline, so you press the **7** button and the AI magically brings the entire train back to the mainline stopping just beyond the last switch.

If you find this mod useful you might like **[SwitchToDestination](https://github.com/peterellisjones/Railroader-SwitchToDestination)** and **[FlyShuntUI](https://github.com/peterellisjones/Railroader-FlyShuntUI)** as well

### Additional Quality of Life (QoL) Features:

* This mods adds an "infinity car lengths" button to the Yard AI "move X car lengths" buttons, for when you just want the AI to couple to something a very long way away.
* In the settings you can allow the Yard AI to automatically release handbrakes before moving
* In the settings you can allow the Yard AI to automatically connect air before moving
* There is setting to automatically apply the handbrakes to stationary cars after decoupling
* In Road AI mode, there is a checkbox to allow the road AI to couple to cars in front, rather than stop before cars.


## Installation

* Download `SmartOrders-VERSION.Railloader.zip` from the releases page
* Install with [Railloader]([https://www.nexusmods.com/site/mods/21](https://railroader.stelltis.ch/))

![screenshot](./Capture.PNG)

## Project Setup

In order to get going with this, follow the following steps:

1. Clone the repo
2. Copy the `Paths.user.example` to `Paths.user`, open the new `Paths.user` and set the `<GameDir>` to your game's directory.
3. Open the Solution
4. You're ready!

### During Development
Make sure you're using the _Debug_ configuration. Every time you build your project, the files will be copied to your Mods folder and you can immediately start the game to test it.

### Publishing
Make sure you're using the _Release_ configuration. The build pipeline will then automatically do a few things:

1. Makes sure it's a proper release build without debug symbols
1. Replaces `$(AssemblyVersion)` in the `Definition.json` with the actual assembly version.
1. Copies all build outputs into a zip file inside `bin` with a ready-to-extract structure inside, named like the project they belonged to and the version of it.
