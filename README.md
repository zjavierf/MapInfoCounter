# MapInfoCounter

A lightweight and clean **Counters+** custom counter for Beat Saber that displays live map info, difficulty labels, star ratings (BeatLeader & ScoreSaber).

Built for 1.40.8, may work for other versions

## Preview

<table>
  <tr>
    <td align="center"><img width="415" alt="MapInfoCounter Preview" src="https://github.com/user-attachments/assets/2bd37369-f1ac-4ecb-9494-ce3881a0674c" /></td>
    <td align="center"><img width="358" height="193" alt="image" src="https://github.com/user-attachments/assets/efdabbff-cfd8-4b02-80b3-938a2180a5d6"/></td>
</td>
  </tr>
</table>

---

## Features

* **Live Map Details:** Automatically fetches and displays song names, custom difficulty labels, and abbreviation tags.
* **Rankings Integration:** Displays star ratings for both **BeatLeader (BL)** and **ScoreSaber (SS)** via SongDetailsCache.
* **Cover Art Display:** Pulls and renders the map's cover art with customizable styles (`Rounded Square`, `Square`, or `Circle`)[cite: 1].
* **Star Color Coding:** Optionally color-codes star ratings based on configurable difficulty thresholds (Low, Mid, High, and Expert)[cite: 1].
* **Fully Configurable:** Easily toggle elements, adjust font size, and customize settings directly through the Counters+ settings menu[cite: 1].

---

## Configurable Settings

You can customize what information is shown on the counter through the in-game configuration menu in Counters+:

* **Show Song Name:** Toggles the visibility of the song title and difficulty text block.
* **Show Cover Art:** Toggles whether the map's cover art thumbnail is displayed on the left side of the counter.
* **Cover Art Style:** Changes the cropping shape of the cover art (`Rounded Square`, `Square`, or `Circle`)[cite: 1].
* **Show Stars:** Master toggle for displaying map star ratings.
* **Show BeatLeader Stars:** Toggles the display of BeatLeader star ratings (`★ BL`).
* **Show ScoreSaber Stars:** Toggles the display of ScoreSaber star ratings (`★ SS`).
* **Color Star Ratings:** Toggles dynamic color-coding for star ratings based on tier thresholds[cite: 1].
* **Font Size:** Adjusts the text scale of the counter display[cite: 1].

---

## Requirements

* [Counters+](https://github.com/NuggoDEV/CountersPlus)
* [SongDetailsCache](https://github.com/kinsi55/BeatSaber_SongDetailsCache)
