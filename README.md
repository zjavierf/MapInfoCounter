# MapInfoCounter

A lightweight and clean **Counters+** custom counter for Beat Saber that displays live map info, difficulty labels, and direct live star ratings (BeatLeader & ScoreSaber).

Built for Beat Saber 1.40.8.

## Preview

| Square Style | Circle Style | Rounded Square Style |
| :---: | :---: | :---: |
| <img width="400" alt="Square Style Preview" src="https://github.com/user-attachments/assets/c45ae43c-4ba7-4102-b754-95eeaaf7c423"/> | <img width="400" alt="Circle Style Preview" src="https://github.com/user-attachments/assets/04e891bd-f9f1-4f63-b3ad-00666bd3e31a"/> | <img width="400" alt="Rounded Square Style Preview" src="https://github.com/user-attachments/assets/fa8dd0f1-7ea9-4306-8909-40d095738328"/> |

---

## Features

- **Live Map Details:** Automatically fetches and displays song names, custom difficulty labels, and abbreviation tags.

- **Direct Live API Integration:** Fetches up-to-date star ratings directly from **BeatLeader's** APIs ensuring that star rating data is accurate.

- **Modifier Star Support:** Automatically displays official BeatLeader modifier star ratings (`[SS]`, `[FS]`, `[SF]`) and dynamically calculates estimated modified star ratings for ScoreSaber when speed modifiers are active.

- Automatically hides star ratings for maps that lack star data on either platform.

- **Cover Art Display:** Pulls and renders the map's cover art with customizable styles (`Rounded Square`, `Square`, or `Circle`).

- **Star Color Coding:** Optionally color-codes star ratings based on configurable difficulty thresholds (Low, Mid, High, and Extreme).

- **Smooth Entrance Transitions:** Clean animated fade, slide, or smooth scaling effects when entering a map.

- **Fully Configurable:** Easily toggle elements, apply global bold/italic styling, adjust font size, and customize settings directly through the Counters+ settings menu.


---

## Configurable Settings

You can customize what information is shown on the counter through the in-game configuration menu in Counters+:

- **Show Song Name:** Toggles the visibility of the song title and difficulty text block.

- **Bold Text:** Applies bold formatting to the entire counter display.

- **Italic Text:** Applies italic formatting to the entire counter display.

- **Compact Mode:** Switches to a streamlined, single-line display format.

- **Show Cover Art:** Toggles whether the map's cover art thumbnail is displayed on the left side of the counter.

- **Cover Art Style:** Changes the cropping shape of the cover art (`Rounded Square`, `Square`, or `Circle`).

- **Show Stars:** Master toggle for displaying map star ratings.

- **Show Modifier Indicator:** Toggles indicator tags (`[SS]`, `[FS]`, `[SF]`) when playing with speed modifiers.

- **Enable Transitions / Transition Style:** Toggles and configures entrance animation effects (`Fade In`, `Slide In`, `Smooth Linear`).

- **Show BeatLeader Stars:** Toggles the display of BeatLeader star ratings (`★ BL`).

- **Show ScoreSaber Stars:** Toggles the display of ScoreSaber star ratings (`★ SS`).

- **Color Star Ratings:** Configure dynamic color coding settings for each star range.

- **Font Size:** Adjusts the text scale of the counter display.


---

## Requirements

- [SongDetailsCache](https://github.com/kinsi55/BeatSaber_SongDetailsCache) 

- [Counters+](https://github.com/NuggoDEV/CountersPlus)

- [CustomJSONData](https://github.com/Aeroluna/CustomJSONData)
