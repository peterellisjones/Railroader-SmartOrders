# Railroader Mod: SmartOrders

Make shunting easier by telling the AI where to move in relation to nearby switches.

This mod adds the following buttons to the Yard AI panel:

* **Approach Ahead**: AI will approach, but not pass the switch immediately ahead of the train
* **Clear Ahead**: AI will pass the switch immediately ahead of the train, so the full length of the train is on the other side of the switch
* **Clear 1, 2, 3**: AI will look for switches _under the train_ and move to clear 1, 2 or 3 of those. If there are not enough switches under the train, it will continue looking for switches ahead of the train.

When approaching or clearing a switch, if the train stops on the _exit side_ of the switch, the AI will leave enough room for the other track to be used.

## Installation

* Download `SmartOrders.UMM.zip` from the releases page
* Install with [Unity Mod Manager](https://www.nexusmods.com/site/mods/21)

![screenshot](./Capture.PNG)
