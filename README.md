# MapInfoCounter

A lightweight and clean **Counters+** custom counter for Beat Saber that displays live map info, difficulty labels, star ratings (BeatLeader & ScoreSaber).

Built for 1.40.8, may work for other versions

## Preview

<table>
  <tr>
    <td align="center"><b>Square Style</b></td>
    <td align="center"><b>Circle Style</b></td>
    <td align="center"><b>Rounded Square Style</b></td>
  </tr>
  <tr>
    <td align="center"><img width="300" alt="Square Style Preview" src="https://github.com/user-attachments/assets/c45ae43c-4ba7-4102-b754-95eeaaf7c423"/></td>
    <td align="center"><img width="300" alt="Circle Style Preview" src="https://github.com/user-attachments/assets/04e891bd-f9f1-4f63-b3ad-00666bd3e31a"/></td>
    <td align="center"><img width="300" alt="Rounded Square Style Preview" src="https://github.com/user-attachments/assets/fa8dd0f1-7ea9-4306-8909-40d095738328"/></td>
  </tr>
</table>

---

## Features

* **Live Map Details:** Automatically fetches and displays song names, custom difficulty labels, and abbreviation tags.
* **Rankings Integration:** Displays star ratings for both **BeatLeader (BL)** and **ScoreSaber (SS)** via SongDetailsCache.
* **Cover Art Display:** Pulls and renders the map's cover art with customizable styles (`Rounded Square`, `Square`, or `Circle`).
* **Star Color Coding:** Optionally color-codes star ratings based on configurable difficulty thresholds (Low, Mid, High, and Expert).
* **Fully Configurable:** Easily toggle elements, adjust font size, and customize settings directly through the Counters+ settings menu.

---

## Configurable Settings

You can customize what information is shown on the counter through the in-game configuration menu in Counters+:

* **Show Song Name:** Toggles the visibility of the song title and difficulty text block.
* **Show Cover Art:** Toggles whether the map's cover art thumbnail is displayed on the left side of the counter.
* **Cover Art Style:** Changes the cropping shape of the cover art (`Rounded Square`, `Square`, or `Circle`).
* **Show Stars:** Master toggle for displaying map star ratings.
* **Show BeatLeader Stars:** Toggles the display of BeatLeader star ratings (`★ BL`).
* **Show ScoreSaber Stars:** Toggles the display of ScoreSaber star ratings (`★ SS`).
* **Color Star Ratings:** Configure dynamic color coding settings for each star ranges.
* **Font Size:** Adjusts the text scale of the counter display.

---

## Requirements

* [Counters+](https://github.com/NuggoDEV/CountersPlus)
* [SongDetailsCache](https://github.com/kinsi55/BeatSaber_SongDetailsCache)
