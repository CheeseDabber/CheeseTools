![thumbnail](thumbnail.png)

# CheeseTools by CheeseRunner1
CheeseTools is a mod for practicing Outer Wilds speedrunning.

I want to thank [SpeedTools](https://github.com/GrimLala/SpeedTools), the first speedrunning mod, for all the inspiration.
SpeedTools has been an absolute vital tool in the speedrunning scene but unfortunately the mod has been mostly abandoned and lately more and more things have started breaking. I felt a new speedrunning mod was overdue which is why I decided to make CheeseTools.

## Features
### Settings
**Infinite Fuel:** Toggle infinite fuel.  
**Infinite Oxygen:** Toggle infinite oxygen.  
**Player Invincibility:** Toggle player invincibility.  
**Ship Invincibility:** Toggle ship invincibility.  
**Create Launch Codes Save:** When enabled upon quitting to main menu it overwrites your current save with one where the loop has started (substitute for the autosplitter "Auto delete progression while keeping Launch Codes" setting).  
**Show Loop Time:** Shows the loop time.  
**Mark Stranger:** Marks the location of the stranger.  
**Show Sectors:** Shows current loaded sectors.  
**Enter Dreamworld Campfire:** Choose dreamworld enter campfire.  

### Keybinds
Keybinds in this mod work as text with a `+` separating each key. To edit keybinds you might find the following setting useful.  
**Log Names Of Pressed Keys:** When enabled it outputs the name of any key you press to the console. Useful for when you want to edit any keybinds but don't know the name of a key.  
| Keybind | Description |
| :-: | :-- |
`/ + R` | Toggle Spacesuit
`/ + T` | Fast Load New Expedition
`/ + Y` | Teleport Player To Ship
`/ + U` | Toggle Speedup
`/ + I` | Enter/Exit Dreamworld
`/ + O` | Log Player Location

### Practice States
Practice states are the main feature of this mod and provide a way to practice specific parts of a speedrun. There are 8 pre-made ones specifically for practicing any%. While practicing I also find it important to be able to quickly try again which is why I made it so starting a practice state automatically loads the scene. No need to quit back to the titlescreen and start a new expedition yourself. You can start any practice state from anywhere at anytime.
### ATP
**ATP Practice State:** `P + 1`  
**ATP Loop Time:** The time the practice state starts.  
_Note: This should not be equal to your ATP sleep time as in a run the loop starts before you reach the campfire. Do a test run with Show Loop Time enabled to see what your loop time should be._  
**ATP Interior Practice State Keybind:** `P + 2`  
**ATP Enter Timer**: Starts when waking up. Ends when entering ATP.  
**ATP Interior Timer**: Starts when entering ATP. Ends when leaving ATP.  
**ATP Exit Timer**: Starts when waking up. Ends when leaving ATP.  
### Bramble
**Bramble Practice State:** `P + 3`  
**Ultimate Feldsparring Practice State:** `P + 4`  
**Ultimate Feldsparring Ship Speed:** Ship initial forward velocity.  
**Bramble Timer:** Starts when leaving ATP. Ends when entering the vessel node.  
**Ultimate Feldsparring Timer:** Starts when entering the anglernest dimension. Ends when entering the vessel node.   
### Vessel
**Vessel Practice State:** `P + 5`  
**Vessel Clip Practice State:** `P + 6`  
**Warp Timer:** Starts when entering the vessel node. Ends when warping.  
**Observer Timer:** Starts upon having warped to the eye. Ends when observing.  
### Clone
**Clone Practice State:** `P + 7`  
**Clone Trees Locator:** Marks standing spot between the three trees.  
**Clone Timer:** Starts when observing. Ends when you hit the clone.
### Instrument Hunt
**Instrument Hunt Practice State:** `P + 8`  
**Cloneboosting Setup:** Puts the scout down at the exact opposite direction of where the clone will spawn. The idea is that after flicking away the trees you look at the scout and then start scoutboosting until you hit the clone.  
**Instrument Hunt Timer:** Starts when touching the clone. Ends when dying to the big bang.   
**Predict Instrument Hunt Time:** After talking to all the travelers it gives an estimate to when you will die to the big bang. Scout boosting to big bang is considered but because of this the estimation is never perfect.

### Custom Practice States
You can create your own practice states using Custom Practice States. There are 3 Custom Practice States provided which you can edit in the mod config. You can use the Log Player Location keybind to get the location of the player.

**Custom Practice States Keybinds:** `/ + 1`, `/ + 2`, `/ + 3`  
**Custom Practice State Body:** The body the location is relative to.  
**Custom Practice State Position:** The relative position.  
**Custom Practice State Rotation:** The relative rotation.  
**Custom Practice State Loop Time:** The time the practice state starts.  
**Custom Practice State Spacesuit:** Whether to spawn with a spacesuit.  
**Custom Practice State Ship** Whether to spawn seated in your ship.  
