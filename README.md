[![Publish](https://github.com/AlekseyMartynov/shazam-for-real/actions/workflows/publish.yml/badge.svg?branch=master)](https://github.com/AlekseyMartynov/shazam-for-real/actions/workflows/publish.yml)

![Screenshot](Screenshot.png?raw=true)

## Install

- Download binary for your platform from [Releases](https://github.com/AlekseyMartynov/shazam-for-real/releases)

- (Linux/Mac) Set executable bit `chmod +x Shazam`

- (Linux/Mac) Install [SoX](https://en.wikipedia.org/wiki/SoX) via `sudo apt-get install sox` or `brew install sox`

## Usage

- Interactive: run without arguments
  
- <a name="tag-files">Tag `WAV` and `MP3` files</a>
  ```
  Shazam PATH [TIME] [till-end]
  ```
  **Example 1: Tag at a specific time**
  ```
  > Shazam mix.mp3 02:00

  00:02:00 https://www.shazam.com/track/668835426/okhopa
  ```
  **Example 2: Tag till the end of file**
  ```
  > Shazam mix.mp3 05:00 till-end

  00:05:00 https://www.shazam.com/track/668835426/okhopa
  00:05:30 https://www.shazam.com/track/668835426/okhopa
  00:06:00 https://www.shazam.com/track/667516878/in-surdose
  00:06:30 https://www.shazam.com/track/667516878/in-surdose
  00:07:00 https://www.shazam.com/track/667516878/in-surdose
  . . .
  ```
