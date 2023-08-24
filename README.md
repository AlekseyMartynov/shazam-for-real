![Screenshot](Screenshot.png?raw=true)

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
  00:05:12 https://www.shazam.com/track/668835426/okhopa
  . . .
  00:06:00 https://www.shazam.com/track/668835426/okhopa
  00:06:12 https://www.shazam.com/track/667516878/in-surdose
  00:06:24 https://www.shazam.com/track/667516878/in-surdose
  . . .
  ```
