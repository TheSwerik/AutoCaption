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

### YouTube Caption Name

This setting will be displayed on the caption itself:<br><img src="img/caption name.png">

- `English` is Displayed if you leave the Setting blank
- `English - AutoCaption` is Displayed if you set the Setting to `AutoCaption`