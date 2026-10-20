using System;
using System.Collections.Generic;

namespace SluMAN
{
    /// <summary>
    /// Goal (checkpoint) names for each Sly 4 job, in goal order, for jobs that aren't running:
    /// the game only has a job's goals in memory while it runs. Keyed by the job's name key without
    /// "$" and "TitleName", as the job list shows it.
    ///
    /// Generated from the goal lists in SLY4-DEBUG-MENU by gg3502
    /// (https://github.com/gg3502/SLY4-DEBUG-MENU, MIT License). They were written by hand, so the
    /// running job's names read from the game take precedence.
    /// </summary>
    public static class Sly4Goals
    {
        private static readonly Dictionary<string, string[]> goals = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "AllRolledUp", new string[]
                {
                    "Chalk Talk: Operation All Rolled Up",
                    "Enter Lamp Shop",
                    "IGC: Belly Outfit",
                    "IGC: Bellydance Intro",
                    "Bellydance MiniGame!",
                    "Mouse Car:Follow the Guard to the Control Room",
                    "IGC: Hey Guards!",
                    "MouseCar: Lead the Guards to the Air Duct",
                    "IGC: Confused Guards",
                    "RCCar: Survive the Air Duct and disable the Laser Door",
                    "IGC: Laser Wall Deactivated",
                    "Hack the Control Room Terminal",
                    "Minigame: Reach the Goal before time runs out",
                    "win",
                    "IGC: Red Alert",
                    "Throw Enemies Into the Laser Turrets",
                    "IGC: Confront Paradox",
                    "Decibel Fight Start",
                    "IGC: Decibel Blast Off 00",
                    "Decibel Glass Fight",
                    "Decibel Platform 1",
                    "Decibel Music Fight",
                    "Decibel Platform 2",
                    "Get to the next platform",
                    "Decibel Glass Fight2",
                    "Decibel Platform 3",
                    "Decibel Music Fight2",
                    "IGC: Decibel Blast Off 04",
                    "Final Fight Decibel",
                    "IGC: Back To The Future",
                }
            },
            { "AlterEgo01", new string[]
                {
                    "Reach the CPU Core",
                    "Blow up the Core!",
                    "WinAlterEgo",
                }
            },
            { "AlterEgo02", new string[]
                {
                    "Reach the CPU Core",
                    "Blow up the Core!",
                    "WinAlterEgo",
                }
            },
            { "AlterEgo03", new string[]
                {
                    "Reach the CPU Core",
                    "Blow up the Core!",
                    "WinAlterEgo",
                }
            },
            { "AlterEgo04", new string[]
                {
                    "Reach the CPU Core",
                    "Blow up the Core!",
                    "WinAlterEgo",
                }
            },
            { "AlterEgo05", new string[]
                {
                    "Reach the CPU Core",
                    "Blow up the Core!",
                    "WinAlterEgo",
                }
            },
            { "AlterEgo06", new string[]
                {
                    "Reach the Portal 06A",
                    "Reach the Portal 06A_Main",
                    "Enter the Portal! 06X",
                    "win",
                    "Enter the Portal! 06A",
                    "win",
                    "Reach the Portal 06B",
                    "Enter the Portal! 06B",
                    "win",
                    "Reach the Portal 06C",
                    "Enter the Portal! 06C",
                    "win",
                    "Reach the CPU Core",
                    "Blow up the Core!",
                    "WinAlterEgo",
                }
            },
            { "AlterEgo06Multi", new string[]
                {
                    "Reach the Portal 06A (2 player)",
                    "Reach the Portal 06A (2 player)_Main",
                    "Enter the Portal! 06X",
                    "win",
                    "Enter the Portal! 06A",
                    "win",
                    "Reach the Portal 06B",
                    "Enter the Portal! 06B",
                    "win",
                    "Reach the Portal 06C",
                    "Enter the Portal! 06C",
                    "win",
                    "Reach the CPU Core",
                    "Blow up the Core!",
                    "WinAlterEgo",
                }
            },
            { "AlterEgo06xMulti", new string[]
                {
                    "Reach the Portal",
                    "Enter the Portal!",
                    "WinAlterEgo",
                }
            },
            { "AlterEgo07", new string[]
                {
                    "Reach the Portal 07A",
                    "Reach the Portal 07A Main",
                    "Enter the Portal! 07A",
                    "win",
                    "Reach the Portal 07B",
                    "Enter the Portal! 07X",
                    "Enter the Portal! 07B",
                    "Reach the Portal 07C",
                    "Enter the Portal! 07C",
                    "Reach the CPU Core 07D",
                    "Blow up the Core! 07D",
                    "WinAlterEgo",
                }
            },
            { "AlterEgo07Multi", new string[]
                {
                    "Reach the Portal 07A (2 player)",
                    "Enter the Portal! 07A",
                    "win",
                    "Reach the Portal 07B",
                    "Enter the Portal! 07X",
                    "Enter the Portal! 07B",
                    "Reach the Portal 07C",
                    "Enter the Portal! 07C",
                    "Reach the CPU Core 07D",
                    "Blow up the Core! 07D",
                    "WinAlterEgo",
                }
            },
            { "AlterEgo07xMulti", new string[]
                {
                    "Reach the Portal",
                    "Enter the Portal!",
                }
            },
            { "AlterEgo08", new string[]
                {
                    "Alter Ego: Reach the CPU Core",
                    "Alter Ego: Reach the CPU Core Main",
                    "Alter Ego: Destroy the CPU Core",
                    "WinAlterEgo",
                    "Spark Runner: Reach the Goal before the timer runs out",
                    "win",
                    "System Cracker: Reach the Docking Gate",
                    "win",
                }
            },
            { "BlindDate", new string[]
                {
                    "Blind Date Start",
                    "Follow Toothpick to the Canyon",
                    "Follow that Stagecoach",
                    "Lower the first bridge",
                    "Get to the Jackalope TNT area",
                    "Clear the path for the Stagecoach",
                    "Protect the stagecoach from the Jackalopes",
                    "Get to the Furnaces",
                    "Protect the Stagecoach and knock down the bridge",
                    "Get to the Train Graveyard",
                    "Fight off the Train Graveyard Jackalopes",
                    "Protect the Stagecoach in the Train Graveyard",
                    "Catch up to the Stagecoach again",
                    "Protect the Stagecoach from traps",
                    "Get to the end",
                    "Blind Date End",
                    "IGC: Carmelita Rescue",
                }
            },
            { "Breakout", new string[]
                {
                    "Collect the Armor Pieces",
                    "Enter the Prison",
                    "Meet Rioichi",
                    "Sneak under table",
                    "Use the Samurai Armor as a disguise",
                    "Use the Samurai Armor to Block Fire",
                    "Use the Samurai Armor to Block Fire Part Deux",
                    "Navigate through the Prison Traps",
                    "Steal the Key",
                    "Open the Gate",
                    "Cross the Bridge",
                    "Free Rioichi!",
                }
            },
            { "CaneSwipe", new string[]
                {
                    "Job Start IGC / Binocucom",
                    "Take a photo of the Black Knight",
                    "Take a picture of the cane case",
                    "Take out the Black Knight's guards",
                    "Hack the cane case",
                    "Minigame: Reach the CPU Core",
                    "Minigame: Destroy the CPU Core",
                    "WinAlterEgo",
                    "Collect Sir Galleth's cane",
                }
            },
            { "CavemanCoopersRescue", new string[]
                {
                    "Job Start IGC",
                    "Sneak into the Arena",
                    "Enter the crawl space",
                    "Approach the Bridge",
                    "IGC: Rescue Eavesdrop",
                    "Sneak above the Arena Gate",
                    "IGC: Grizzle B Intro",
                    "Head to the Arena Viewing Chamber",
                    "IGC: Grizzle Painting",
                    "Steal Bismarque's Lair Key",
                    "IGC: GrabKey",
                    "Open the Gate",
                    "IGC: Open Ice Gate",
                    "Acquire the Sabertooth Costume",
                    "IGC: Costume Collect",
                    "Walk to Pounce the Post",
                    "Pounce the Post Tutorial",
                    "Pounce the Post",
                    "Sneak Past Fighters",
                    "Pounce the Guard",
                    "IGC: Cane Swipe",
                    "IGC: Rescue_BIN_Pounce",
                    "Free Caveman Cooper",
                    "IGC: Caveman Outro",
                }
            },
            { "ClosedForRemodeling", new string[]
                {
                    "Job Start Binocucom",
                    "Enter the Blacksmith",
                    "Pre-Entrance VO",
                    "IGC Blacksmith Alarms",
                    "Destroy the control consoles! (room 1)",
                    "Go to room 2!",
                    "Destroy the control consoles! (room 2)",
                    "Go to room 3!",
                    "Destroy the control consoles! (room 3)",
                    "Job Complete",
                }
            },
            { "CrazyClimber", new string[]
                {
                    "Carpet Shop IGC",
                    "Go to the Carpet Shop",
                    "SuperClimb Tutorial",
                    "Bino: Backroom Reveal",
                    "Cobra Climb",
                    "Break Rope",
                    "Reach Base of Tower",
                    "Enter Tower",
                    "Hatch CLose IGC",
                    "Use Mechanism",
                    "Snake Plat",
                    "Tower Checkpoint01",
                    "Tower Checkpoint01A",
                    "Tower Checkpoint02",
                    "Tower Checkpoint02A",
                    "Tower Checkpoint03",
                    "Tower Checkpoint03A",
                    "Tower Checkpoint04",
                    "Tower Checkpoint05",
                    "Enter Penthouse",
                    "Minigame: Hack the Garage Terminal",
                    "Minigame: Reach the Goal before time runs out",
                    "win",
                    "Rescue the Thief CutScene",
                }
            },
            { "DutyCalls", new string[]
                {
                    "IGC: Duty Calls Start",
                    "Follow Grizz",
                    "Follow Griz (Start call 1)",
                    "Follow Griz (After call 1)",
                    "Follow Griz (Start call 2)",
                    "Follow Griz (After call 2)",
                    "Follow Griz (Start call 3)",
                    "Follow Griz (After call 3)",
                    "Follow Griz (Start call 4)",
                    "Follow Griz (After call 4)",
                    "Enter Arena Follow Grizzle B",
                }
            },
            { "Epilogue", new string[]
                {
                    "Chalk Talk: Epilogue",
                    "Sly Enters Blimp IGC",
                    "Sly: Blimp Start",
                    "Sly Carmelita Captured IGC",
                    "Rioichi: Get Cane Start",
                    "Rio Cane Recover IGC",
                    "Big Brawl 1",
                    "Hallway 1 IGC",
                    "Caveman Cane Path IGC",
                    "Rioichi: Recover Caveman Cane",
                    "Rio Leaves IGC",
                    "Caveman: Recover Salim Cane Part 1",
                    "Caveman: Recover Salim Cane Part 2",
                    "Caveman Leaves IGC",
                    "Big Brawl 2",
                    "Hallway 2 IGC",
                    "Galleth Cane Path IGC",
                    "Salim: Recover Galleth Cane",
                    "Salim Leaves IGC",
                    "Kid Cane Path IGC",
                    "Sir Galleth: Destroy Fuse Set 1",
                    "Sir Galleth: Destroy Fuse Set 2",
                    "Sir Galleth: Recover Kid Cooper Cane",
                    "Galleth Leaves IGC",
                    "Big Brawl 3",
                    "Hallway 3 IGC",
                    "Kid Boss Path IGC",
                    "Kid Cooper: Start",
                    "Kid Cooper: Spin Platforms",
                    "Kid Cooper: Boss Door IGC",
                    "Kid Cooper: Boss Door Puzzle",
                    "Boss Intro IGC",
                    "Boss Fight Round 1",
                    "Boss Fight Round 2",
                    "Blimp Going Down IGC",
                    "Boss Fight Round 3",
                    "Boss Fight Round 3 IGC",
                    "Boss Fight Fencing 3",
                    "Paradox Defeated IGC",
                }
            },
            { "Fishy", new string[]
                {
                    "Play Cutscene Caves Exterior",
                    "Minigame: Reach the CPU Core",
                    "Minigame: Destroy the CPU Core",
                    "WinAlterEgo",
                    "Enter The Cave",
                    "Bentley goes to the gate",
                    "Check out the first cave",
                    "Binocucom:Bentley tells Murray to hit the plant",
                    "Follow Markers to Fishing Spot",
                    "Open Door 04",
                    "Cross over the Lily Pads",
                    "Get to Rickety Bridge!",
                    "Get to Fishing Spot!",
                    "Catch Fish!",
                    "Get the fish back to the cart while it is still fresh!",
                    "Get the fish back to the final cart",
                }
            },
            { "GettingStronger", new string[]
                {
                    "Talk to Murray",
                    "IGC: Intro",
                    "Train with Murray",
                    "You think you bad?!",
                }
            },
            { "GoingUp", new string[]
                {
                    "IGC: Intro",
                    "Defeat the Dodo Birds",
                    "IGC: MadMurray",
                    "Climb the Mountain",
                    "Wall Smash",
                    "Wall Leap",
                    "Wall Slide",
                    "Enter the Pipe",
                    "IGC: CavemanOpening",
                    "IGC: BINO Rockcliff intro",
                    "Find the Temporal Sprocket",
                    "Reach the Ice Wall",
                    "Avoid the Eggs!",
                    "Continue up the mountain",
                    "Grab Van Piece",
                    "IGC: PteryFakey",
                    "IGC: HatcheryIntro",
                    "Climb but don't wake the babies",
                    "Climb but don't wake the babies 2",
                    "Climb but don't wake the babies 3",
                    "Climb but don't wake the babies 4",
                    "IGC: Outro",
                }
            },
            { "GrandKeyLarceny", new string[]
                {
                    "Chalk Talk: Grand Key Larceny!",
                    "Find the Secret Boxing Arena!",
                    "IGC: Boxing Start",
                    "Beat Up 20 Coyotes!",
                    "IGC: Brawl Champ",
                    "Defeat LeMoo!",
                    "IGC: Boxing Finish",
                    "Start the Ball Race IGC",
                    "Get to the Finish!",
                    "Race Finish IGC",
                    "Start the Shooting Match IGC",
                    "Start Shooting!",
                    "Win the Shooting Match IGC",
                    "IGC: Grand Key Larceny Completed!",
                }
            },
            { "HardTarget", new string[]
                {
                    "IGC Hard Target Setup",
                    "Gather three Fire Bulbs",
                    "Go to the archery game",
                    "IGC Carnival Game",
                    "Shoot the Targets",
                }
            },
            { "HungryHungryHippo", new string[]
                {
                    "IGC: Binocucom Hippo",
                    "Squeeze the Penguins",
                    "Play the final VO",
                }
            },
            { "IceIceBentley", new string[]
                {
                    "IGC: Intro",
                    "Find the Grizz",
                    "Locate Bismarque in the art room",
                    "Sculpture Room Intro",
                    "Solve the Sculpture Room puzzle",
                    "Sculpture Room Outro",
                    "Sculpture Room Outro Close Door",
                    "Navigate the tunnel and avoid the egg",
                    "Art Gallery Intro",
                    "Collect the gems from the guards",
                    "Use the gems to open the door",
                    "Art Gallery Outro",
                    "Locate Grizz through the tunnel",
                    "Burial Room Intro",
                    "Tag Grizz",
                    "Burial Room Outro",
                }
            },
            { "JailBreak", new string[]
                {
                    "IGC: Raft Start Down the River",
                    "IGC: JailBreakStart",
                    "Get to the first dock!",
                    "Clear Out the First Dock!",
                    "IGC: exit to dock A",
                    "Get to the first dock Lock panel!",
                    "IGC: JailBreak_IGC_DockAOpen",
                    "Get Back to the raft after first lock!",
                    "Get to the second dock!",
                    "Protect the Raft From Jackelopes!",
                    "IGC: exit to dock B",
                    "Get to the second dock Lock panel!",
                    "IGC: JailBreak_IGC_DockBOpen",
                    "Get Back to the raft after second lock!",
                    "Get to the Prison dock!",
                    "IGC: exit to dock C",
                    "Get to the Granary!",
                    "IGC: Rescue Gang Enter",
                    "Rescue the team!",
                    "IGC: Rescue Gang Exit",
                }
            },
            { "JailhouseBlues", new string[]
                {
                    "Get to location for picture of the Presidio Tower",
                    "Bentley takes picture of Presidio Tower",
                    "Get to location for picture of the Presidio Arsenal",
                    "Bentley takes picture of Presidio Arsenal",
                    "Get to location for picture of the Presidio Wall",
                    "Bentley takes picture of Presidio Wall",
                    "IGC: Sly Meets Tennessee Kid Cooper",
                    "Destroy Prison Wall",
                    "Get Out of the Tower!",
                    "IGC: Show the Tower Bottom",
                    "Help Tennessee escape!",
                    "What is Tennessee looking at?",
                    "First Block Puzzle",
                    "Get to Tnt",
                    "IGC: Kid Cooper picks up the TNT!",
                    "Get Across the water",
                    "IGC: Granary Start",
                    "Granary Block Puzzle",
                    "IGC: Granary Puzzle finished",
                    "Get To Fire Hatch Alley",
                    "IGC: Fire Hatch Alley",
                    "Get To Fourth Block Puzzle End",
                    "Catch up to Tennessee",
                    "What is that strange Glow?",
                    "IGC: Roll Ball Hall Start",
                    "Use the Convict Suit Ball Roll",
                    "IGC: Ball Roll End",
                    "Get to shuffle puzzle",
                    "IGC: Intro the Yard",
                    "Solve the Shuffle Puzzle",
                    "Get to the Presidio Back Wall!",
                    "IGC: Presidio End",
                    "Disable the Laser Fields!",
                    "IGC: Disable the Laser Fields",
                    "Push the final block!",
                    "Plant TNT at crack",
                    "IGC: Sly and Tennessee ending jump cutscene",
                }
            },
            { "JugglingAct", new string[]
                {
                    "Job Start Binocucom",
                    "Climb the Tower",
                    "Switch to the Archer Costume",
                    "Grab an arrow",
                    "Shoot the target",
                    "Enter the circus tent",
                    "CircusTent Intro (Binoccucom)",
                    "Find Sir Galleth (tent start)",
                    "Find Sir Galleth (triple rings)",
                    "Find Sir Galleth (trampoline)",
                    "Find Sir Galleth (frog set 1)",
                    "Find Sir Galleth (trapeze set 1)",
                    "Find Sir Galleth (cannon set 1)",
                    "Find Sir Galleth (trapeze set 2)",
                    "Find Sir Galleth (cannon set 2)",
                    "Find Sir Galleth (frog set 2)",
                    "Find Sir Galleth (dragon)",
                    "Find Sir Galleth (rescue galleth)",
                }
            },
            { "LeapingLizard", new string[]
                {
                    "IGC: Intro Binocucom",
                    "Get inside the distribution area",
                    "IGC: Lighthouse",
                    "Get to the top of the spotlight",
                    "Cut the ice into a penguin shape",
                    "Melt the dingle arm free",
                    "Steal the Dingle Arm",
                    "Outro IGC",
                }
            },
            { "LostAndFound", new string[]
                {
                    "Start Lost and Found",
                    "Enter Lamp Shop",
                    "Bentley talking about Lamps",
                    "Find Clues in Lamp Shop",
                    "Photo Strange Door",
                    "Exit to Village",
                    "Find Clues to Salim",
                    "Go talk to Salim al Kupar",
                    "IGC Sly Meets Salim",
                }
            },
            { "MadameGeisha", new string[]
                {
                    "RC car IGC Intro",
                    "Follow El Jefe to prison",
                    "El Jefe around corner to fishing cave",
                    "Follow El Jefe to geisha house",
                }
            },
            { "MechanicalMenace", new string[]
                {
                    "Job Start Binocucom",
                    "Find the Moat Monster's Lair",
                    "Find the Moat Monster (Cave Entrance)",
                    "Find the Moat Monster (Wall 1)",
                    "Find the Moat Monster (Wall 2)",
                    "Find the Moat Monster (Wall 3)",
                    "Find the Moat Monster (Wall 4)",
                    "Unplug the Moat Monster",
                    "Defeat the Moat Monster (Phase 1)",
                    "Defeat the Moat Monster (Phase 2)",
                    "IGC Final",
                }
            },
            { "OpAltSick", new string[]
                {
                    "Operation Chalk Talk",
                    "Pickpocket the key to the rollers",
                    "Unlock the rollers",
                    "Enter Dragon Gate",
                    "Play BridgeView IGC",
                    "Go to the bridge",
                    "IGC ElJefeConfrontation",
                    "Bridge Rat Fight",
                    "Cross under the bridge",
                    "Follow the stone path",
                    "Open the sword gate",
                    "Pursue El Jefe",
                    "Cross the broken bridge",
                    "Find El Jefe",
                    "Play Cutscene Face To Face",
                    "Get to Tower1",
                    "Defeat El Jefe (Tower1)",
                    "Get to Tower2",
                    "Defeat El Jefe (Tower2)",
                    "Get to Tower3",
                    "Defeat El Jefe",
                    "Play Cutscene El Jefe Defeat",
                }
            },
            { "OpenSesame", new string[]
                {
                    "Open Sesame Start Point",
                    "Binocucom:Decimal and Paradox Walking",
                    "Collect the Map Pieces",
                }
            },
            { "OperationGoldDigger", new string[]
                {
                    "chalk talk",
                    "IGC: Gang boards train",
                    "Free The Van!",
                    "IGC: Murray frees train",
                    "Escort Bentley to the Hacking Terminal",
                    "IGC: Approach the Hacking Terminal",
                    "Gain Access to the Hack Terminal!",
                    "Hack the Terminal",
                    "Reach the CPU Core",
                    "Destroy the CPU Core",
                    "WinAlterEgo",
                    "IGC: Carmelita and Bentley Escape",
                    "Get into the first gold car!",
                    "Release the first gold stash!",
                    "IGC: First Gold Release",
                    "Get to the next train car A",
                    "Release the second gold stash!",
                    "IGC: Second Gold Release",
                    "Get to the next train car B",
                    "IGC: Kid Cooper Cane Stolen",
                    "Defeat Toothpick Tutorial",
                    "Defeat Toothpick Round 1",
                    "Defeat Toothpick Round 2",
                    "Defeat Toothpick Round 3",
                    "IGC: Toothpick Defeated",
                }
            },
            { "OperationJurassicThievery", new string[]
                {
                    "IGC: Intro Chalk Talk",
                    "Enter the Distribution Area",
                    "Jump on the Conveyor Belt",
                    "IGC: Coveyor Belt Intro",
                    "Navigate to the First Egg Vat (pre)",
                    "IGC: Chamber Intro",
                    "Navigate to the First Egg Vat",
                    "Geysers",
                    "Near Bridge",
                    "Destroy the First Egg Vat",
                    "IGC: EggVat1Destroyed",
                    "Navigate to the next Egg Vat",
                    "Hack the Hammers",
                    "Minigame: Reach the Goal before time runs out",
                    "win",
                    "IGC: HammerHack",
                    "Navigate to the next egg vat2",
                    "Destroy the Second Egg Vat",
                    "IGC: EggVat2Destroyed",
                    "Loosen the valves1",
                    "Loosen the valves2",
                    "Loosen the valves3",
                    "IGC: Sly Intro",
                    "Activate the Egg Transporters",
                    "Glider",
                    "Take out the foreman",
                    "IGC: SlyOutro",
                    "switch to murray",
                    "IGC: Grizzle BossFight Intro",
                    "Defeat Grizz Round 1",
                    "Defeat Grizz Phase 2",
                    "Defeat Grizz Phase 3",
                    "IGC: Grizzle BossFight Outro",
                }
            },
            { "OperationMousetrap", new string[]
                {
                    "Chalk Talk: Operation Mousetrap",
                    "IGC Drawbridge Open",
                    "IGC Lose Cane",
                    "BOSS: Mega Knight Phase1",
                    "BOSS: Mega Knight Phase2",
                    "IGC Mega Knight Defeated",
                    "BOSS: Penelope",
                    "Job Complete",
                }
            },
            { "PhotoOp", new string[]
                {
                    "Take a picture of boarguard",
                    "Take a picture of Prison",
                    "Take a picture of Dragon Gate",
                    "Take a picture of Sushi Shop",
                    "Take a picture of Geisha house",
                    "Photograph El Jefe",
                }
            },
            { "PrettyPink", new string[]
                {
                    "Play Cutscene Geisha Exterior",
                    "Enter the hatch on Geisha roof",
                    "Play IGC RioichiEnterGeisha",
                    "Enter the back area of the Geisha house",
                    "Pass through the garden ponds",
                    "Cross the Cat Crushers",
                    "Cross the Broken bridge",
                    "Hack the terminal",
                    "Reach the Goal before time runs out",
                    "win",
                    "Disable Geisha Lock",
                    "Exit to Mainroom",
                    "Meet Murray On top of the Geisha Suit Case!",
                    "Play Dance MiniGame!",
                }
            },
            { "Prologue", new string[]
                {
                    "Intro Movies",
                    "Intro Cinematic",
                    "autosave warning",
                    "Sly Tutorial: Heli IGC",
                    "Sly Tutorial: Movement",
                    "Sly Tutorial: Jump",
                    "Sly Tutorial: Double Jump",
                    "Sly Tutorial: Jump Attach",
                    "Sly Tutorial: IGC Bus",
                    "Sly Tutorial: Before Sidle",
                    "Sly Tutorial: Sidle",
                    "Sly Tutorial: Helicopter 2 IGC",
                    "Sly Zipline To Flagpole",
                    "Sly Pole Flashback",
                    "Sly Tutorial: Balcony",
                    "Sly Tutorial: Le Paradox Window",
                    "Sly Tutorial: Heli Setup 4a",
                    "Sly Tutorial: Heli Setup 4b",
                    "Sly Tutorial: Heli Setup 4c",
                    "Sly: Search for Skylight",
                    "Sly: Sneak up on rat",
                    "Sly: pick pocket",
                    "Sly: Stealth Slam",
                    "Sly: Open Skylight",
                    "Bentley: Bomb",
                    "Bentley: Trains",
                    "Bentley: Hover Jump",
                    "Bentley: Bomb Throw",
                    "Bentley: Train Part 2",
                    "Bentley Bomb Floor",
                    "Bentley Flashback",
                    "Bentley: Control Room",
                    "Bentley: Hacking",
                    "Minigame: Reach the First Docking Gate",
                    "Minigame: Destroy the Firewall",
                    "Minigame: Use the Key to Disable the Lasers",
                    "Minigame: Activate the Panzer Code",
                    "Minigame: Destroy the Barriers",
                    "Minigame: Reach the final Docking Gate",
                    "win",
                    "Bentley Outro Murray Intro",
                    "Murray Tutorial: Basic Combat",
                    "Murray Tutorial: Front of warehouse fight",
                    "Murray Before FlashBack",
                    "Murray Tutorial: Destroy Junction Boxes",
                }
            },
            { "RedFlag", new string[]
                {
                    "Red Flag Start",
                    "Steal Flags",
                    "PlayerTrans IGC",
                    "Sneak into Harem",
                    "Find Silk inside Furniture",
                }
            },
            { "RiceRocket", new string[]
                {
                    "Start Job Pick-Up Sushi",
                    "Drop off First Sushi",
                    "Pick Up Sushi For 2nd Time",
                    "Drop off Second Sushi",
                    "End Rice RocketJob",
                }
            },
            { "RobbinHoods", new string[]
                {
                    "Job Start Binocucom",
                    "Go to the first tower",
                    "Break the first balloon",
                    "Go to the second tower",
                    "Break the second balloon",
                    "Go to the third tower",
                    "Break the third balloon",
                }
            },
            { "RockAndRoll", new string[]
                {
                    "ExtSweep IGC",
                    "Enter Library",
                    "IntSweep IGC",
                    "Salim Plants Radios",
                    "wait for vo",
                    "Bentley Trans IGC",
                    "Bentley Shoots Radios",
                    "wait for Bentley vo",
                    "rock on IGC",
                    "Plant Bug",
                }
            },
            { "SaloonBug", new string[]
                {
                    "Binocucom: Check Out Saloon",
                    "Enter the Saloon From Town",
                    "IGC: Sly and Bentley Enter the Saloon From Town",
                    "IGC: Sly and Bentley Enter the Saloon",
                    "Minigame: Get the Steers a Drink!",
                    "Find A Way Into the Casino!",
                    "Sly Enters the Casino",
                    "IGC: Casino Start",
                    "Knock the hooch vat into place",
                    "Get To the Money Drop Without Being Seen!",
                    "IGC: Sly Places the RC Truck",
                    "Plant the Bug At Toothpick's Office!",
                    "IGC: RC Car At Toothpick's Office",
                }
            },
            { "ShellShocked", new string[]
                {
                    "Job Start Binocucom",
                    "Follow the Black Knight",
                    "IGC Black Knight Enters Blacksmith",
                    "Enter the Blacksmith",
                    "Binocucom Blacksmith Interior Sweep",
                    "Follow the Black Knight (1a)",
                    "Follow the Black Knight (1b)",
                    "IGC Black Knight Gate 1",
                    "Hack the gate! (room 1)",
                    "Reach the Docking Gate 04",
                    "Win System Cracker 04",
                    "Move to Room 2",
                    "Follow the Black Knight (2a)",
                    "Follow the Black Knight (2b)",
                    "IGC Black Knight Gate 2",
                    "Hack the gate! (room 2)",
                    "Reach the CPU Core",
                    "Destroy the CPU Core",
                    "WinAlterEgo",
                    "Move to Room 3",
                    "Follow the Black Knight (3a)",
                    "Follow the Black Knight (3b)",
                    "IGC Black Knight Reveal",
                }
            },
            { "ShhhhhQuiet", new string[]
                {
                    "Go to ShhhhhQuiet Start",
                    "IGC:Bentley Flies in the RCCopter",
                    "Distract Guards with Food",
                    "IGC:Sly Gets in Position",
                    "Free the Thief",
                    "IGC:Sly Frees the Thief",
                }
            },
            { "ShoppingSpree", new string[]
                {
                    "Job Start Binocucom",
                    "Eavesdrop on Guards (Tavern)",
                    "Enter the Tavern",
                    "Tavern Binocucom Reveal",
                    "Find Metal Material",
                    "Exit the Tavern",
                    "Eavesdrop on Guards (Bakery)",
                    "Enter the Bakery",
                    "Reach the First Docking Gate",
                    "Reach the Second Docking Gate",
                    "Reach the Final Docking Gate",
                    "win",
                    "Find Wood Material (part 1)",
                    "Find Wood Material (part 2)",
                    "Exit the Bakery",
                    "Eavesdrop on Guards (Shoemaker)",
                    "Enter the Shoemaker",
                    "Find Leather Material (part 1)",
                    "Find Leather Material (part 2)",
                }
            },
            { "ShortSupply", new string[]
                {
                    "Chalk Talk: Short Supply",
                    "IGC Split Up",
                    "Enter the Tavern",
                    "Destroy the machines",
                    "Enter the Shoemaker",
                    "Locate the shutdown switch (part 1)",
                    "Locate the shutdown switch (part 2)",
                    "Locate the shutdown switch (part 3)",
                    "Minigame: Reach the Goal before time runs out",
                    "win",
                    "IGC Shoemaker Shut Down",
                    "Enter the Bakery",
                    "Find a way to the upper floor",
                    "Bakery Interior Binocucom",
                    "Disable all three nodes",
                    "IGC Bakery Shut Down",
                }
            },
            { "SparkRunner01", new string[]
                {
                    "Reach the end before the timer runs out!",
                    "start",
                    "win",
                }
            },
            { "SparkRunner02", new string[]
                {
                    "Reach the end before the timer runs out!",
                    "Reach the goal before time runs out!",
                    "win",
                }
            },
            { "SparkRunner03", new string[]
                {
                    "Reach the end before the timer runs out!",
                    "Reach the end before the timer runs out!",
                    "win",
                }
            },
            { "SparkRunner04", new string[]
                {
                    "Reach the end before the timer runs out!",
                    "Reach the end before the timer runs out!",
                    "win",
                }
            },
            { "SparkRunner05", new string[]
                {
                    "Reach the end before the timer runs out!",
                    "Reach the end before the timer runs out!",
                    "win",
                }
            },
            { "SparkRunner06", new string[]
                {
                    "Reach the end before the timer runs out!",
                    "Reach the end before the timer runs out!  Main",
                    "win",
                }
            },
            { "SparkRunner07", new string[]
                {
                    "Reach the end before the timer runs out!",
                    "Reach the end before the timer runs out!",
                    "win",
                }
            },
            { "SpecialDelivery", new string[]
                {
                    "Bino Gems",
                    "Find and Collect Gems",
                    "Deliver The Gems",
                    "Reach the gate",
                    "Sneak under the gate using the costume",
                    "Use Sword to smash gate controls",
                    "Follow LeParadox and Miss Decibel A",
                    "Follow LeParadox and Miss Decibel B",
                    "Follow LeParadox and Miss Decibel C",
                    "Combo Slo Time Sword",
                    "Use Lever and Costume B",
                    "Sneak Under Gate B",
                    "Use SwordB",
                    "Meet Decibel and Paradox",
                }
            },
            { "SpikedSushi", new string[]
                {
                    "Enter Cave",
                    "Play Caves Binocucom Intro",
                    "Pick Poison Plants",
                }
            },
            { "StoneAgeReconnaissance", new string[]
                {
                    "IGC: Intro Chalk Talk",
                    "Take photos in the village",
                    "Wait for Photo VO",
                    "Investigate the Arena",
                    "IGC Biz Intro",
                    "Photograph Grizzle B",
                    "IGC Job Complete",
                }
            },
            { "SushiStartup", new string[]
                {
                    "Play Cutscene Sushi Exterior",
                    "Sneak into the Sushi Shop",
                    "Play Cutscene Rioichi Enter",
                    "Play Cutscene Binocucom Int Sweep",
                    "Climb bamboo to get a better vantage point",
                    "Knife guard01 check point",
                    "Pickpocket the First Sushi Knife",
                    "Unlock the first sushi knife door",
                    "Pickpocket the Second Sushi Knife",
                    "Unlock the second sushi knife door",
                    "Get through second Laser Hall",
                    "Reach the top rafters",
                    "Find a way through the Banquet Room",
                    "Pickpocket the Third Sushi Knife",
                    "Unlock the third sushi knife door",
                    "Turn on the Power to the Sushi restaurant",
                }
            },
            { "SystemCracker01", new string[]
                {
                    "Reach the First Docking Gate",
                    "Destroy the Firewall",
                    "Use the Key to Disable the Lasers",
                    "Switch to the Tank Avatar",
                    "Destroy the Tank Barriers",
                    "Reach the Final Docking Gate",
                    "win",
                }
            },
            { "SystemCracker02", new string[]
                {
                    "Reach the First Docking Gate",
                    "Reach the Second Docking Gate",
                    "Reach the Final Docking Gate",
                    "win",
                }
            },
            { "SystemCracker03", new string[]
                {
                    "Reach the First Docking Gate",
                    "Reach the Second Docking Gate",
                    "Reach the Final Docking Gate",
                    "win",
                }
            },
            { "SystemCracker04", new string[]
                {
                    "Reach the Docking Gate",
                    "win",
                }
            },
            { "SystemCracker05", new string[]
                {
                    "Reach the Docking Gate",
                    "win",
                }
            },
            { "SystemCracker07", new string[]
                {
                    "Reach the Docking Gate",
                    "win",
                }
            },
            { "SystemCracker08", new string[]
                {
                    "Reach the Docking Gate",
                    "Reach the Docking Gate 08b",
                    "Reach the Docking Gate 08c",
                    "Reach the Docking Gate 08d",
                    "win",
                }
            },
            { "SystemCracker09", new string[]
                {
                    "Get to the Final Docking Gate!",
                    "win",
                }
            },
            { "SystemCracker09Multi", new string[]
                {
                    "Get to the Final Docking Gate! (2P)",
                    "win",
                }
            },
            { "TaxiDriver", new string[]
                {
                    "Go to Taxi Shop",
                    "IGC:Sly and Murray Plunge into Sewers",
                    "Lift the Sunken Ship",
                    "Murray blocked",
                    "Open Door",
                    "Sneak through Door",
                    "TrapA Checkpoint",
                    "Conquer TrapA",
                    "Navigate to TrapB",
                    "Halfway TrapB",
                    "TrapRoomB Door Checkpoint",
                    "Conquer TrapB",
                    "Use Sword",
                    "Cannon Fortress Battle",
                    "Use the Crank A",
                    "Lift Ship B",
                    "Advance to Grinders",
                    "Gold Room",
                    "Gold Room Part Deux",
                    "Gold Room Checkpoint",
                    "Gold Room Checkpoint Part Deux",
                    "Sword KeyB",
                    "Grinders Stop",
                    "Navigate to island",
                    "Use the Crank B",
                    "Cannon Fight B",
                    "Lift ShipC",
                    "Navigate to Ship",
                    "Ship is Sinking",
                    "Hack the Garage Terminal",
                    "Minigame: Reach the Docking Gate",
                    "win",
                    "Rescue cutscene",
                }
            },
            { "ToothpicksVault", new string[]
                {
                    "Chalk Talk: Toothpick's Vault",
                    "Follow Toothpick to the Vault!",
                    "IGC: Toothpick Enters the Vault",
                    "Protect Bentley From the Scorpions!",
                    "IGC: Bentley Hack Start",
                    "Minigame: Reach the First Docking Gate",
                    "Minigame: Reach the Second Docking Gate",
                    "Minigame: Reach the Final Docking Gate",
                    "win",
                    "IGC: Kid Cooper Enters the Vault",
                    "IGC: Kid Cooper Gets His Cane Back",
                    "Use Your Gun to Open the Vault Door!",
                    "Explore the Vault!",
                    "IGC: Show Laser Field!",
                    "Shoot the Switch to Turn Off the Grid!",
                    "Open Up the Big Vault Door!",
                    "IGC: Crackshot Vault Door",
                    "Use Crack Shot On Multiple Locks!",
                    "Ride the Rails to the Miner's Camp!",
                    "Use Crack Shot On the Big Vault Door!",
                    "Find a way out of the Mine",
                    "Escape Route! Unlock the door",
                    "Open the Exit Door!",
                    "Escape the mines!",
                    "IGC: Vault Exit",
                }
            },
            { "UnderArrest", new string[]
                {
                    "Binocucom: Deface Posters",
                    "Deface First Toothpick Poster",
                    "Deface Second Toothpick Poster",
                    "Deface Third Toothpick Poster",
                    "Deface Fourth Toothpick Poster",
                    "Deface Fifth Toothpick Poster",
                    "Deface Final Toothpick Poster",
                    "Binocucom: Steal Lollipop!",
                    "Steal Toothpick's Lollipop!",
                    "Get a look at the Toothpick's Banner",
                    "Binocucom shows the banner",
                    "Tear Down Toothpick's Banner!",
                    "Toothpick's Banner Fall Cutscene",
                }
            },
            { "UpInSmoke", new string[]
                {
                    "IGC Up In Smoke Start",
                    "Go To TurretA",
                    "IGC TurretA Reveal",
                    "RCCopter Smoke BombA",
                    "IGC On The Move A",
                    "Go To TurretB",
                    "IGC TurretB Reveal",
                    "RCCopter Smoke BombB",
                    "IGC On The Move B",
                    "Go To TurretC",
                    "IGC TurretC Reveal",
                    "RCCopter Smoke BombC",
                    "IGC On The Move C",
                    "Free the Thief",
                    "Minigame: Reach the CPU Core",
                    "Minigame: Destroy the CPU Core",
                    "WinAlterEgo",
                    "IGC Rescue",
                }
            },
        };

        /// <summary>The job's goal names, or null if they aren't known.</summary>
        public static string[] ForJob(string jobName)
        {
            string[] names;
            if (jobName != null && goals.TryGetValue(jobName, out names))
            {
                return names;
            }
            return null;
        }
    }
}
