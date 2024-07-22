# Railroader Mod: SmartOrders

Make shunting easier by telling the AI where to move in relation to nearby switches.

This mod adds the following buttons to the Yard AI panel:

* **Approach Ahead**: AI will approach, but not pass the switch immediately ahead of the train in the direction of travel
* **Clear Ahead**: AI will pass the switch immediately ahead of the train, so the full length of the train is on the other side of the switch
* **1, 2, 3**: Starting from the back of the train, the AI will look for switches under the train and move so it clears 1, 2 or 3 switches as desired. If there are not enough switches under the train, it will continue looking for switches ahead of the train

When approaching or clearing a switch, if the train stops on the _exit side_ of the switch, the AI will leave enough room for the other track to be used.

The AI will look for switches as far as 1000m away, but is less precise the further away the target switch is.

Known incompatability: Unfortunately this mod is **not** compatible with wexp's [RR-YardAiExtended](https://github.com/wexp/RR-YardAiExtended) mod

## Installation

* Download `SmartOrders-VERSION.Railloader.zip` from the releases page
* Install with [Railloader]([https://www.nexusmods.com/site/mods/21](https://railroader.stelltis.ch/))

![screenshot](./Capture.PNG)
