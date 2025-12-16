# Action Space Expansion Plan

## Current State: Simple Mode (4 actions)

After first VICTORY, expand action space to support full Tzar gameplay.

### Current Actions (Simple Mode)
| Index | Action | Purpose |
|-------|--------|---------|
| 0 | None | Do nothing |
| 1 | MouseMove | Move mouse cursor |
| 2 | LeftClick | Select/interact |
| 3 | RightClick | **Move units / Attack** |

All 30 network output neurons are mapped cyclically to these 4 actions (~25% each).

---

## Phase 2: Full Action Space

### Tzar Keyboard Controls (from game settings)

#### Camera Movement
| Key | Action |
|-----|--------|
| W | Move Up |
| S | Move Down |
| A | Move Left |
| D | Move Right |
| Arrow Keys | Alternative camera movement |

#### Unit Selection (F-keys)
| Key | Action |
|-----|--------|
| F1 | Select All Idle Peasants |
| F2 | Select All Military Units |
| F3 | Select All Priests |
| F5 | Select All Wizards |
| F6 | Select All Spies |
| F7 | Select All Heroes |
| F10 | Select All Mounted Units |
| F11 | Select All Archers |
| F12 | Select All Sword Units |

#### Unit Actions
| Key | Action |
|-----|--------|
| A | Attack enemy |
| B | Bless |
| C | Confusion |
| E | Explore |
| I | Bribe enemy unit |

#### UI/View
| Key | Action |
|-----|--------|
| X | Expand Minimap |
| O | Reset Zoom |
| Z | Standard Kingdom Colors |
| Q | Game Information |
| SPACE | Focus View into Selected Unit |
| TAB | Diplomacy |
| ESCAPE | Menu |
| ENTER | Chat Message |
| DELETE | Delete Building/Unit |
| PAUSE | Pause |

### Proposed Full Action Map (~60 actions)

```
Index | Action
------|--------
0     | None
1     | MouseMove
2     | LeftClick
3     | RightClick
4     | DoubleClick
5     | DragStart
6     | DragEnd
7     | ScrollUp
8     | ScrollDown
9-18  | Hotkey 0-9 (Ctrl+number to assign group)
19-28 | HotkeyCtrl 0-9 (Select group)
29    | Key_W (camera up)
30    | Key_S (camera down)
31    | Key_A (camera left / attack)
32    | Key_D (camera right)
33    | Key_E (explore)
34    | Key_Q (game info)
35    | Key_X (minimap)
36    | Key_O (reset zoom)
37    | Key_Z (colors)
38    | Key_B (bless)
39    | Key_C (confusion)
40    | Key_I (bribe)
41-52 | F1-F12 (unit selection)
53    | Space (focus unit)
54    | Tab (diplomacy)
55    | Escape
56    | Enter
57    | Delete
58    | ArrowUp
59    | ArrowDown
60    | ArrowLeft
61    | ArrowRight
```

### Implementation Steps

1. **Update ActionType enum** in `TzarBot.Common/Models/ActionType.cs`
   - Add new action types for keys

2. **Update ActionDecoder** in `TzarBot.NeuralNetwork/Inference/ActionDecoder.cs`
   - Uncomment and modify `BuildActionTypeMapFull()`
   - Add mapping logic for new actions

3. **Update PlaywrightGameInterface** in `TzarBot.BrowserInterface/`
   - Add keyboard input methods for new keys
   - Implement F1-F12, letter keys, arrow keys

4. **Update network architecture** (optional)
   - Increase output neurons from 30 to 62
   - Or keep 30 and use sampling strategy

5. **Retrain from Generation 0**
   - New networks with expanded action space
   - Start fresh with simple maps

### Priority Actions for RTS

**High Priority (essential for gameplay):**
- RightClick (move/attack)
- F1 (select idle peasants)
- F2 (select military)
- LeftClick (select)
- A (attack command)

**Medium Priority (useful):**
- Camera movement (W/A/S/D or arrows)
- Hotkey groups (1-9)
- Space (focus)

**Low Priority (situational):**
- Special abilities (B, C, I, E)
- UI toggles (X, O, Z, Q)
- F3-F12 (specific unit types)

---

## Migration Notes

When switching from Simple to Full mode:
1. Networks trained in Simple mode won't transfer well
2. Start new Generation 0 with full action space
3. Consider curriculum: simple maps -> complex maps
