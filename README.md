# MediaSplitter
This was written because I was frustrated with how my Plex Server would read a single file if it had multiple episodes in it.

For example If I had a file named `S01E01-E02 New Squid on the Block + Down the Drain.m4v` there is 2 episodes in this one file. Plex using the [TheTvDb](http://thetvdb.com) to get metadata for the episodes would show that file as 2 episodes. However when you chose to play episode 2 it would start at the beginning of episode 1 and you would have to scrub to episode 2 most of the time.

# Arguments
#### /Media
This is always required, it doesn't matter if it is a folder or a single file, it will still split the files. If it is a folder it will scan non-recursively in the folder specified.
````ini
  // Folder
  /Media="C:\Users\Administrator\Videos"
  
  // Single File
  /Media="C:\Users\Administrator\Videos\S01E01-E02 New Squid on the Block + Down the Drain.m4v"
````

#### /Extensions
If the `/Media` argument is specified, it will look for these extensions. If `/Media` is a single file it will ignore this argument. Every extension should be seperated by a command for it to be properly recognized.
````ini
  /Extensions=.m4v,.avi
````

#### /Debug
Pauses after every file is split. Used for attaching debugger as well as slowing down the processes and being able to see the results easier per file.
````ini
  /Debug
````
#### /BlackDuration
The minimum detected black duration (in seconds). [FFmpeg Setting](https://ffmpeg.org/ffmpeg-filters.html#blackdetect).
````ini
  /BlackDuration=(Default)0.08
````
  
#### /BlackThreshold
Threshold for considering a picture as "Black" (in percent). [FFmpeg Setting](https://ffmpeg.org/ffmpeg-filters.html#blackdetect).
````ini
  /BlackThreshold=(Default)0.08
````
  
#### /BlackPixelLuminance
Threshold for considering a picture as "Black" (in percent). [FFmpeg Setting](https://ffmpeg.org/ffmpeg-filters.html#blackdetect).
````ini
  /BlackPixelLuminance=(Default)0.12
````
  
#### /CutTime
If specified it will ignore any Black Screen Detection settings and just cut the file/files at the exact time specified. Parses string to a TimeSpan.
````ini
  // Will cut at exactly 10 seconds.
  /CutTime=00:00:10

  // Will cut at exactly 11 minutes.
  /CutTime=00:11:00
````

#### /StartRange
If specified and is splitting using Black Screen Detection will only get black screens that are greater than the StartRange. Parses string to a TimeSpan. (Both StartRange and EndRange needs to be specified)
````ini
  // Will only get black screens above 11 minutes.
  /StartRange=00:11:00
````
  
#### /EndRange
If specified and is splitting using Black Screen Detection will only get black screens that are less than the EndRange. Parses string to a TimeSpan. (Both StartRange and EndRange needs to be specified)
````ini
  // Will only get black screens below 12 minutes.
  /StartRange=00:12:00
````
For the StartRange and EndRange examples it will get all black screens between the time frame of 11 and 12 minutes.
