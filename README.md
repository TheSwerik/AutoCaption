# AutoCaption

## Prerequisites

### Install Whisper

1. Install Python >=3.10 <3.14
2. Install Whisper

```bash
  pip install git+https://github.com/openai/whisper.git
  choco install ffmpeg
  pip install setuptools-rust
  pip uninstall torch torchaudio torchvision -y
  pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu121
```

## Settings

### Default Output Location

The Default Output Location for the caption files.  
Can be changed per file.

### Output Format

The Default Output-Format to generate.  
Can be changed per file.  
For YouTube, VTT is recommended.

### Default Language

The Default language to detect.  
Can be changed per file.

### Do File Splitting

Whether or not to split the input file into smaller temporary files, process them separately and then merge the resulting captions together.

In my experience, whisper starts breaking down at very long video lengths (50min+ for Medium).
At that video length, the words will be correct, but punctuation, timing and line length will be ignored and a wall of words gets written into the caption.

File splitting increases the processing time.

### Segment Duration

Only visible when File Splitting is enabled.  
The Duration of each segment when using file splitting.  
Increasing the segment duration results in less processing time but can lead to degraded quality of the result. Decreasing has the opposite effect.

### Model

The model to use.  
For the available Models, reference <a href="https://github.com/openai/whisper#available-models-and-languages">Whisper</a>.

### Use GPU

Whether or not to use the GPU (cuda) or the CPU.
CPU is way slower.

### Log Level

The Minimum Log-Level to display (for file and console)

### Log to file

Whether to log to `%appdata%/AutoCaption/logs.txt`

### YouTube Caption Name

This setting will be displayed on the caption itself:<br><img src="img/caption name.png">

- `English` is Displayed if you leave the Setting blank
- `English - AutoCaption` is Displayed if you set the Setting to `AutoCaption`

### Continue Generating from YouTube even if Quota limit is reached?

Importing every video from your channel to the session should normally be possible even considering the Quota.  
But Uploading one caption file to one video has the Quota cost of importing 8 Videos. You will reach this limit pretty quickly  
With this setting enabled, AutoCaption will continue to go through your session and process every video, even if it cannot upload the captions, so that you can upload them manually afterwards (since the caption files will be exported still).
This is possible since processing a file has no quota cost at all.

If you disable this option, once the quota limit is reached, processing will be paused and you will be notified.

If you enable this option, videos, for which the caption could not be uploaded, will be orange but the generated caption file will exist and processing will continue.